using System.IO;
using System.Text.RegularExpressions;
using MSCKite.Azure.Platform.Internal.Common;

namespace MSCKite.Azure.Platform.Internal.DevOps
{
    internal class SavedAdoSession
    {
        public string Account { get; set; }
        public string Organization { get; set; }
        public string CollectionUri { get; set; }
        public string Project { get; set; }
        public bool UsesAzToken { get; set; }
    }

    // Persists the "connected" marker set by Connect-AdoOrganization to disk, so it survives across
    // sessions like Connect-AzAccount and gh auth login do; no token is stored here, only who/where
    internal static class AdoSessionStore
    {
        private static readonly string ConfigDirectory = ModulePaths.ConfigDirectory;

        private static readonly string SessionFilePath = Path.Combine(ConfigDirectory, ModulePaths.DevOpsSessionFileName);

        internal static void Save(string account, string organization, string collectionUri, string project, bool usesAzToken)
        {
            Directory.CreateDirectory(ConfigDirectory);

            var content =
                "// Marks Connect-AdoOrganization as connected across sessions; removed by Disconnect-AdoOrganization\n" +
                "{\n" +
                $"  \"account\": {Quote(account)},\n" +
                $"  \"organization\": {Quote(organization)},\n" +
                $"  \"collectionUri\": {Quote(collectionUri)},\n" +
                $"  \"project\": {Quote(project)},\n" +
                $"  \"usesAzToken\": {(usesAzToken ? "true" : "false")}\n" +
                "}\n";

            File.WriteAllText(SessionFilePath, content);
        }

        // Returns the saved session, or null if none was saved (or it couldn't be read); never throws
        internal static SavedAdoSession Load()
        {
            if (!File.Exists(SessionFilePath))
            {
                return null;
            }

            try
            {
                var withoutComments = Regex.Replace(
                    File.ReadAllText(SessionFilePath), @"^\s*//.*$", string.Empty, RegexOptions.Multiline);

                var organization = ExtractString(withoutComments, "organization");
                if (string.IsNullOrEmpty(organization))
                {
                    return null;
                }

                var collectionUri = ExtractString(withoutComments, "collectionUri");

                // Self-heal sessions saved before collectionUri existed, where organization held the full URL
                if (string.IsNullOrEmpty(collectionUri) && organization.Contains("://"))
                {
                    collectionUri = organization;
                    organization = AdoApiHelper.ExtractOrganizationFromUri(collectionUri);
                }

                return new SavedAdoSession
                {
                    Account = ExtractString(withoutComments, "account"),
                    Organization = organization,
                    CollectionUri = collectionUri,
                    Project = ExtractString(withoutComments, "project"),
                    UsesAzToken = Regex.IsMatch(withoutComments, "\"usesAzToken\"\\s*:\\s*true")
                };
            }
            catch (IOException)
            {
                return null;
            }
        }

        // Best-effort delete; a leftover file would just be overwritten by the next successful Connect
        internal static void Clear()
        {
            try
            {
                if (File.Exists(SessionFilePath))
                {
                    File.Delete(SessionFilePath);
                }
            }
            catch (IOException)
            {
            }
        }

        private static string Quote(string value)
        {
            return value == null ? "null" : $"\"{value}\"";
        }

        private static string ExtractString(string json, string key)
        {
            var match = Regex.Match(json, $"\"{key}\"\\s*:\\s*\"([^\"]+)\"");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
