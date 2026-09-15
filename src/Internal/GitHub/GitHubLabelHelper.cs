using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MSCKite.Azure.Platform.Internal.Common;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Internal.GitHub
{
    // Reads label definitions from a jsonc file and syncs them to a repository via `gh api repos/{owner}/{repo}/labels`
    internal static class GitHubLabelHelper
    {
        private static readonly Regex ColorPattern = new Regex("^#?[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

        // Parses a versioned jsonc label template with a labels array
        internal static List<GitHubLabel> LoadFromFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Labels file not found: {path}", path);
            }

            TemplateVersionHelper.ReadVersion(path, "templateVersion");

            var json = File.ReadAllText(path);
            var options = new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            using (var document = JsonDocument.Parse(json, options))
            {
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object ||
                    !root.TryGetProperty("labels", out var arrayElement) ||
                    arrayElement.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException("Labels file must be a JSON object with a \"templateVersion\" and a \"labels\" array.");
                }

                var labels = new List<GitHubLabel>();
                var seenNames = new HashSet<string>(StringComparer.Ordinal);

                foreach (var element in arrayElement.EnumerateArray())
                {
                    var label = ParseLabel(element);

                    if (!seenNames.Add(label.Name))
                    {
                        throw new InvalidOperationException($"Duplicate label name in labels file: '{label.Name}'.");
                    }

                    labels.Add(label);
                }

                return labels;
            }
        }

        private static GitHubLabel ParseLabel(JsonElement element)
        {
            var name = GetString(element, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Each label must have a non-empty \"name\".");
            }

            var color = GetString(element, "color");
            if (string.IsNullOrWhiteSpace(color) || !ColorPattern.IsMatch(color))
            {
                throw new InvalidOperationException($"Label '{name}' has an invalid color '{color}'. Expected a 6-digit hex value like #ffffff.");
            }

            return new GitHubLabel
            {
                Name = name,
                Color = color.TrimStart('#').ToLowerInvariant(),
                Description = GetString(element, "description") ?? string.Empty
            };
        }

        private static string GetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }

        // Retrieves all labels currently defined on the repository; returns null and sets error on failure
        internal static List<GitHubLabel> GetRemoteLabels(string owner, string repository, out string error)
        {
            var output = RunGh(
                new[] { "api", $"repos/{owner}/{repository}/labels", "--paginate" },
                out var exitCode,
                out var stdError);

            if (exitCode != 0)
            {
                error = string.IsNullOrWhiteSpace(stdError) ? output : stdError;
                return null;
            }

            try
            {
                var labels = new List<GitHubLabel>();
                var docOptions = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip };

                using (var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(output) ? "[]" : output, docOptions))
                {
                    foreach (var element in document.RootElement.EnumerateArray())
                    {
                        labels.Add(new GitHubLabel
                        {
                            Name = GetString(element, "name"),
                            Color = GetString(element, "color")?.ToLowerInvariant(),
                            Description = GetString(element, "description") ?? string.Empty
                        });
                    }
                }

                error = null;
                return labels;
            }
            catch (JsonException ex)
            {
                error = $"Failed to parse labels response from GitHub: {ex.Message}";
                return null;
            }
        }

        // Creates a label that doesn't yet exist on the repository
        internal static bool CreateLabel(string owner, string repository, GitHubLabel label, out string error)
        {
            RunGh(
                new[]
                {
                    "api", $"repos/{owner}/{repository}/labels",
                    "-X", "POST",
                    "-f", $"name={label.Name}",
                    "-f", $"color={label.Color}",
                    "-f", $"description={label.Description}"
                },
                out var exitCode,
                out var stdError);

            error = exitCode == 0 ? null : stdError;
            return exitCode == 0;
        }

        // Updates the color/description of an existing label, matched by its current name
        internal static bool UpdateLabel(string owner, string repository, GitHubLabel label, out string error)
        {
            RunGh(
                new[]
                {
                    "api", $"repos/{owner}/{repository}/labels/{Uri.EscapeDataString(label.Name)}",
                    "-X", "PATCH",
                    "-f", $"new_name={label.Name}",
                    "-f", $"color={label.Color}",
                    "-f", $"description={label.Description}"
                },
                out var exitCode,
                out var stdError);

            error = exitCode == 0 ? null : stdError;
            return exitCode == 0;
        }

        // Deletes a label by name
        internal static bool DeleteLabel(string owner, string repository, string name, out string error)
        {
            RunGh(
                new[]
                {
                    "api", $"repos/{owner}/{repository}/labels/{Uri.EscapeDataString(name)}",
                    "-X", "DELETE"
                },
                out var exitCode,
                out var stdError);

            error = exitCode == 0 ? null : stdError;
            return exitCode == 0;
        }

        // Runs `gh` with the given arguments, quoting each one; never throws (missing gh surfaces as a non-zero exit code)
        private static string RunGh(string[] arguments, out int exitCode, out string stdError)
        {
            try
            {
                using (var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "gh",
                        Arguments = string.Join(" ", QuoteAll(arguments)),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                })
                {
                    process.Start();
                    var output = process.StandardOutput.ReadToEnd();
                    stdError = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    exitCode = process.ExitCode;
                    return output;
                }
            }
            catch (Win32Exception)
            {
                exitCode = -1;
                stdError = "GitHub CLI ('gh') was not found on PATH. Install it, then run gh auth login.";
                return null;
            }
        }

        private static IEnumerable<string> QuoteAll(string[] arguments)
        {
            foreach (var argument in arguments)
            {
                yield return QuoteArgument(argument);
            }
        }

        private static string QuoteArgument(string argument)
        {
            if (argument.Length > 0 && argument.IndexOfAny(new[] { ' ', '"', '\t', '\n' }) < 0)
            {
                return argument;
            }

            var builder = new StringBuilder("\"");
            builder.Append(argument.Replace("\"", "\\\""));
            builder.Append('"');
            return builder.ToString();
        }
    }
}
