using System.IO;
using System.Text.RegularExpressions;
using MSCKite.Azure.Platform.Internal.Common;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Internal.GitHub
{
    // Persists default GitHub settings (owner/repository) to a per-user github-config.jsonc, cached in memory for the session
    internal static class GitHubConfigStore
    {
        private static readonly string ConfigDirectory = ModulePaths.ConfigDirectory;

        private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, ModulePaths.GitHubConfigFileName);

        private static GitHubDefaults _cached;

        // Writes the defaults to github-config.jsonc and refreshes the in-memory cache; passing null clears a value
        internal static GitHubDefaults Save(string owner, string repository)
        {
            var defaults = new GitHubDefaults
            {
                Owner = string.IsNullOrEmpty(owner) ? null : owner,
                Repository = string.IsNullOrEmpty(repository) ? null : repository
            };

            defaults.RepositoryUri = defaults.Owner == null || defaults.Repository == null
                ? null
                : $"https://github.com/{defaults.Owner}/{defaults.Repository}";

            Directory.CreateDirectory(ConfigDirectory);
            File.WriteAllText(ConfigFilePath, ToJsonc(defaults));

            _cached = defaults;
            return defaults;
        }

        // Returns the cached defaults, loading them from github-config.jsonc on first use; never throws
        internal static GitHubDefaults Load()
        {
            if (_cached != null)
            {
                return _cached;
            }

            if (!File.Exists(ConfigFilePath))
            {
                return _cached = new GitHubDefaults();
            }

            try
            {
                _cached = FromJsonc(File.ReadAllText(ConfigFilePath));
            }
            catch (IOException)
            {
                _cached = new GitHubDefaults();
            }

            return _cached;
        }

        private static string ToJsonc(GitHubDefaults defaults)
        {
            return "// Default GitHub settings used by Azure Platform Kite; managed via Set-GitHubDefault\n" +
                   "{\n" +
                   $"  \"owner\": {Quote(defaults.Owner)},\n" +
                   $"  \"repository\": {Quote(defaults.Repository)},\n" +
                   $"  \"repositoryUri\": {Quote(defaults.RepositoryUri)}\n" +
                   "}\n";
        }

        private static string Quote(string value)
        {
            return value == null ? "null" : $"\"{value}\"";
        }

        private static GitHubDefaults FromJsonc(string json)
        {
            var withoutComments = Regex.Replace(json, @"^\s*//.*$", string.Empty, RegexOptions.Multiline);

            return new GitHubDefaults
            {
                Owner = ExtractValue(withoutComments, "owner"),
                Repository = ExtractValue(withoutComments, "repository"),
                RepositoryUri = ExtractValue(withoutComments, "repositoryUri")
            };
        }

        private static string ExtractValue(string json, string key)
        {
            var match = Regex.Match(json, $"\"{key}\"\\s*:\\s*\"([^\"]+)\"");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
