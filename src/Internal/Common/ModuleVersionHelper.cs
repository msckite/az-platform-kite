using System.Collections;
using System.Management.Automation;

namespace MSCKite.Azure.Platform.Internal.Common
{
    internal static class ModuleVersionHelper
    {
        // Returns the loaded module's full version as used in release tags without the leading 'v' (e.g. "1.1.0" or "1.1.0-prev1"),
        // or null when the cmdlet was not loaded through the module manifest (e.g. the bare DLL in tests)
        internal static string GetKiteVersion(PSModuleInfo module)
        {
            // Only a module manifest carries the real release version; a bare DLL reports its assembly version instead,
            // and has no PrivateData. The unstamped '0.0.0' source manifests aren't a release either.
            if (!(module?.PrivateData is Hashtable) || module.Version == null || module.Version.Major == 0 && module.Version.Minor == 0 && module.Version.Build <= 0)
            {
                return null;
            }

            // Built from the parts, since Version.ToString(3) throws for a two-part version such as "1.1"
            var version = $"{module.Version.Major}.{module.Version.Minor}.{System.Math.Max(module.Version.Build, 0)}";
            var prerelease = ((Hashtable)module.PrivateData)["PSData"] is Hashtable psData ? psData["Prerelease"] as string : null;

            return string.IsNullOrWhiteSpace(prerelease) ? version : $"{version}-{prerelease}";
        }
    }
}
