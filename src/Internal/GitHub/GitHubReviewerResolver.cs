using System.Text.Json;

namespace MSCKite.Azure.Platform.Internal.GitHub
{
    // Resolves a reviewer login (user) or slug (team) to the {type, id} pair the environments API requires
    internal static class GitHubReviewerResolver
    {
        internal static bool TryResolve(string owner, string reviewer, out string type, out long id, out string error)
        {
            var userOutput = GitHubCliRunner.Run(new[] { "api", $"users/{reviewer}" }, out var userExitCode, out _);
            if (userExitCode == 0 && TryGetId(userOutput, out id))
            {
                type = "User";
                error = null;
                return true;
            }

            var teamOutput = GitHubCliRunner.Run(new[] { "api", $"orgs/{owner}/teams/{reviewer}" }, out var teamExitCode, out var teamError);
            if (teamExitCode == 0 && TryGetId(teamOutput, out id))
            {
                type = "Team";
                error = null;
                return true;
            }

            type = null;
            id = 0;
            error = $"Could not resolve '{reviewer}' as a GitHub user or as a team in '{owner}': {teamError}";
            return false;
        }

        private static bool TryGetId(string json, out long id)
        {
            id = 0;
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                using (var document = JsonDocument.Parse(json))
                {
                    if (document.RootElement.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.Number)
                    {
                        id = idElement.GetInt64();
                        return true;
                    }
                }
            }
            catch (JsonException)
            {
                // Not a valid single-object response; treat as unresolved
            }

            return false;
        }
    }
}
