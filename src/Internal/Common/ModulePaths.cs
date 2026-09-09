using System;
using System.IO;

namespace MSCKite.Azure.Platform.Internal.Common
{
    // Central place for per-user file paths used by the config/session stores, so they stay in sync
    internal static class ModulePaths
    {
        internal static readonly string ConfigDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MSCKite", "Azure.Platform");

        internal const string DevOpsConfigFileName = "devops-config.jsonc";
        internal const string DevOpsSessionFileName = "devops-session.jsonc";
        internal const string GitHubConfigFileName = "github-config.jsonc";
    }
}
