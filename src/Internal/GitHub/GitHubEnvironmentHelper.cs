using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace MSCKite.Azure.Platform.Internal.GitHub
{
    internal class GitHubReviewer
    {
        internal string Type { get; set; }

        internal long Id { get; set; }
    }

    // Creates, updates, and reads a repository's deployment environment (protection rules only; secrets/variables are separate APIs)
    internal static class GitHubEnvironmentHelper
    {
        // Returns whether the environment exists; error is only populated on failure, so a permission/auth problem isn't silently mistaken for a real 404
        internal static bool Exists(string owner, string repository, string name, out string error)
        {
            GitHubCliRunner.Run(new[] { "api", $"repos/{owner}/{repository}/environments/{Uri.EscapeDataString(name)}" }, out var exitCode, out var stdError);
            error = exitCode == 0 ? null : stdError;
            return exitCode == 0;
        }

        // Always reconciles the environment to the desired state (idempotent PUT); GitHub doesn't expose a cheap way to diff protection_rules beforehand
        internal static bool CreateOrUpdate(string owner, string repository, string name, int waitTimerMinutes, List<GitHubReviewer> reviewers, out string error)
        {
            // wait_timer/reviewers are premium-plan-only protection rules on private repos; sending them (even as 0/empty) triggers a 422 on plans that don't support them, so only include what's actually configured
            var body = new JsonObject();

            if (waitTimerMinutes > 0)
            {
                body["wait_timer"] = waitTimerMinutes;
            }

            if (reviewers.Count > 0)
            {
                var reviewersArray = new JsonArray();
                foreach (var reviewer in reviewers)
                {
                    reviewersArray.Add(new JsonObject { ["type"] = reviewer.Type, ["id"] = reviewer.Id });
                }

                body["reviewers"] = reviewersArray;
            }

            GitHubCliRunner.Run(
                new[] { "api", "-X", "PUT", $"repos/{owner}/{repository}/environments/{Uri.EscapeDataString(name)}", "--input", "-" },
                out var exitCode,
                out var stdError,
                body.ToJsonString());

            error = exitCode == 0 ? null : stdError;
            return exitCode == 0;
        }
    }
}
