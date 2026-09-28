using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MSCKite.Azure.Platform.Internal.GitHub
{
    internal class GitHubDeploymentBranchPolicy
    {
        internal long Id { get; set; }

        internal string Name { get; set; }

        // "branch" or "tag"
        internal string Type { get; set; }
    }

    // Lists, creates, and deletes the custom branch/tag name patterns that may deploy to an environment
    internal static class GitHubDeploymentBranchPolicyHelper
    {
        internal static List<GitHubDeploymentBranchPolicy> List(string owner, string repository, string environment, out string error)
        {
            // 100 is the API's page maximum; an environment is never expected to carry more patterns than that
            var output = GitHubCliRunner.Run(
                new[] { "api", $"{PoliciesPath(owner, repository, environment)}?per_page=100" },
                out var exitCode,
                out var stdError);

            if (exitCode != 0)
            {
                error = string.IsNullOrWhiteSpace(stdError) ? output : stdError;
                return null;
            }

            var policies = new List<GitHubDeploymentBranchPolicy>();
            try
            {
                using (var document = JsonDocument.Parse(output))
                {
                    if (document.RootElement.TryGetProperty("branch_policies", out var policiesElement) && policiesElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var element in policiesElement.EnumerateArray())
                        {
                            policies.Add(new GitHubDeploymentBranchPolicy
                            {
                                Id = element.GetProperty("id").GetInt64(),
                                Name = element.GetProperty("name").GetString(),
                                // Policies created before tag support existed have no type, and are always branch policies
                                Type = element.TryGetProperty("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String ? typeElement.GetString() : "branch"
                            });
                        }
                    }
                }

                error = null;
                return policies;
            }
            catch (Exception ex) when (ex is JsonException || ex is KeyNotFoundException || ex is InvalidOperationException)
            {
                error = $"Failed to parse deployment branch policy response from GitHub: {ex.Message}";
                return null;
            }
        }

        internal static bool Create(string owner, string repository, string environment, string name, string type, out string error)
        {
            var body = new JsonObject { ["name"] = name, ["type"] = type };

            GitHubCliRunner.Run(
                new[] { "api", "-X", "POST", PoliciesPath(owner, repository, environment), "--input", "-" },
                out var exitCode,
                out var stdError,
                body.ToJsonString());

            error = exitCode == 0 ? null : stdError;
            return exitCode == 0;
        }

        internal static bool Delete(string owner, string repository, string environment, long id, out string error)
        {
            GitHubCliRunner.Run(
                new[] { "api", "-X", "DELETE", $"{PoliciesPath(owner, repository, environment)}/{id}" },
                out var exitCode,
                out var stdError);

            error = exitCode == 0 ? null : stdError;
            return exitCode == 0;
        }

        private static string PoliciesPath(string owner, string repository, string environment)
        {
            return $"repos/{owner}/{repository}/environments/{Uri.EscapeDataString(environment)}/deployment-branch-policies";
        }
    }
}
