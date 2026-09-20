using System;
using System.Collections.Generic;
using System.Text.Json;

namespace MSCKite.Azure.Platform.Internal.GitHub
{
    // Sets environment variables via `gh variable set`; unlike secrets, variable values are readable, so existing ones can be diffed
    internal static class GitHubVariableHelper
    {
        internal static Dictionary<string, string> List(string owner, string repository, string environment, out string error)
        {
            var output = GitHubCliRunner.Run(
                new[] { "variable", "list", "--env", environment, "--repo", $"{owner}/{repository}", "--json", "name,value" },
                out var exitCode,
                out var stdError);

            if (exitCode != 0)
            {
                error = string.IsNullOrWhiteSpace(stdError) ? output : stdError;
                return null;
            }

            var variables = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                using (var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(output) ? "[]" : output))
                {
                    foreach (var element in document.RootElement.EnumerateArray())
                    {
                        if (element.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
                        {
                            variables[nameElement.GetString()] = element.TryGetProperty("value", out var valueElement) && valueElement.ValueKind == JsonValueKind.String
                                ? valueElement.GetString()
                                : string.Empty;
                        }
                    }
                }

                error = null;
                return variables;
            }
            catch (JsonException ex)
            {
                error = $"Failed to parse variable list response from GitHub: {ex.Message}";
                return null;
            }
        }

        internal static bool Set(string owner, string repository, string environment, string name, string value, out string error)
        {
            GitHubCliRunner.Run(
                new[] { "variable", "set", name, "--env", environment, "--repo", $"{owner}/{repository}", "--body", value },
                out var exitCode,
                out var stdError);

            error = exitCode == 0 ? null : stdError;
            return exitCode == 0;
        }
    }
}
