using System.Text.Json;

namespace MSCKite.Azure.Platform.Internal.GitHub
{
    // Live GitHub repository identity, used to build the immutable OIDC subject claim (owner/repo names alone can be recycled or renamed)
    internal class GitHubRepositoryInfo
    {
        internal string OwnerLogin { get; set; }

        internal string OwnerId { get; set; }

        internal string Name { get; set; }

        internal string Id { get; set; }
    }

    // Reads a repository's immutable identity via `gh api repos/{owner}/{repo}`
    internal static class GitHubRepositoryHelper
    {
        internal static GitHubRepositoryInfo GetRepository(string owner, string repository, out string error)
        {
            var output = GitHubCliRunner.Run(
                new[] { "api", $"repos/{owner}/{repository}" },
                out var exitCode,
                out var stdError);

            if (exitCode != 0)
            {
                error = string.IsNullOrWhiteSpace(stdError) ? output : stdError;
                return null;
            }

            try
            {
                using (var document = JsonDocument.Parse(output))
                {
                    var root = document.RootElement;
                    var info = new GitHubRepositoryInfo
                    {
                        Name = GetString(root, "name"),
                        Id = GetRawNumber(root, "id")
                    };

                    if (root.TryGetProperty("owner", out var ownerElement) && ownerElement.ValueKind == JsonValueKind.Object)
                    {
                        info.OwnerLogin = GetString(ownerElement, "login");
                        info.OwnerId = GetRawNumber(ownerElement, "id");
                    }

                    error = null;
                    return info;
                }
            }
            catch (JsonException ex)
            {
                error = $"Failed to parse repository response from GitHub: {ex.Message}";
                return null;
            }
        }

        private static string GetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }

        private static string GetRawNumber(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number
                ? value.GetRawText()
                : null;
        }
    }
}
