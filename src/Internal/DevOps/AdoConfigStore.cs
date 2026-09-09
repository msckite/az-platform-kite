using System.IO;
using System.Text.RegularExpressions;
using MSCKite.Azure.Platform.Internal.Common;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Internal.DevOps
{
    // Persists default Azure DevOps settings (organization/project) to a per-user devops-config.jsonc, cached in memory for the session
    internal static class AdoConfigStore
    {
        private static readonly string ConfigDirectory = ModulePaths.ConfigDirectory;

        private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, ModulePaths.DevOpsConfigFileName);

        private static AdoDefaults _cached;

        // Writes the defaults to devops-config.jsonc and refreshes the in-memory cache; passing null clears a value
        internal static AdoDefaults Save(string organization, string project)
        {
            var defaults = new AdoDefaults
            {
                Organization = string.IsNullOrEmpty(organization) ? null : organization,
                Project = string.IsNullOrEmpty(project) ? null : project,
                CollectionUri = string.IsNullOrEmpty(organization) ? null : $"https://dev.azure.com/{organization}"
            };

            Directory.CreateDirectory(ConfigDirectory);
            File.WriteAllText(ConfigFilePath, ToJsonc(defaults));

            _cached = defaults;
            return defaults;
        }

        // Returns the cached defaults, loading them from devops-config.jsonc on first use; never throws
        internal static AdoDefaults Load()
        {
            if (_cached != null)
            {
                return _cached;
            }

            if (!File.Exists(ConfigFilePath))
            {
                return _cached = new AdoDefaults();
            }

            try
            {
                _cached = FromJsonc(File.ReadAllText(ConfigFilePath));
            }
            catch (IOException)
            {
                _cached = new AdoDefaults();
            }

            return _cached;
        }

        private static string ToJsonc(AdoDefaults defaults)
        {
            return "// Default Azure DevOps settings used by Azure Platform Kite; managed via Set-AdoDefault\n" +
                   "{\n" +
                   $"  \"organization\": {Quote(defaults.Organization)},\n" +
                   $"  \"project\": {Quote(defaults.Project)},\n" +
                   $"  \"collectionUri\": {Quote(defaults.CollectionUri)}\n" +
                   "}\n";
        }

        private static string Quote(string value)
        {
            return value == null ? "null" : $"\"{value}\"";
        }

        private static AdoDefaults FromJsonc(string json)
        {
            // Strip only full-line comments; a naive "//.*" also matches the "//" inside https:// values
            var withoutComments = Regex.Replace(json, @"^\s*//.*$", string.Empty, RegexOptions.Multiline);

            return new AdoDefaults
            {
                Organization = ExtractValue(withoutComments, "organization"),
                Project = ExtractValue(withoutComments, "project"),
                CollectionUri = ExtractValue(withoutComments, "collectionUri")
            };
        }

        private static string ExtractValue(string json, string key)
        {
            var match = Regex.Match(json, $"\"{key}\"\\s*:\\s*\"([^\"]+)\"");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
