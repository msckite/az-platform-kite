using System;
using System.Collections.Generic;
using System.Text.Json;

namespace MSCKite.Azure.Platform.Internal.GitHub
{
    // Sets environment secrets via `gh secret set`; GitHub never exposes secret values, so existing ones can only be listed by name, not diffed
    internal static class GitHubSecretHelper
    {
        internal static HashSet<string> ListNames(string owner, string repository, string environment, out string error)
        {
            var output = GitHubCliRunner.Run(
                new[] { "secret", "list", "--env", environment, "--repo", $"{owner}/{repository}", "--json", "name" },
                out var exitCode,
                out var stdError);

            if (exitCode != 0)
            {
                error = string.IsNullOrWhiteSpace(stdError) ? output : stdError;
                return null;
            }

            var names = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                using (var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(output) ? "[]" : output))
                {
                    foreach (var element in document.RootElement.EnumerateArray())
                    {
                        if (element.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
                        {
                            names.Add(nameElement.GetString());
                        }
                    }
                }

                error = null;
                return names;
            }
            catch (JsonException ex)
            {
                error = $"Failed to parse secret list response from GitHub: {ex.Message}";
                return null;
            }
        }

        internal static bool Set(string owner, string repository, string environment, string name, string value, out string error)
        {
            GitHubCliRunner.Run(
                new[] { "secret", "set", name, "--env", environment, "--repo", $"{owner}/{repository}", "--body", value },
                out var exitCode,
                out var stdError);

            error = exitCode == 0 ? null : stdError;
            return exitCode == 0;
        }
    }
}
