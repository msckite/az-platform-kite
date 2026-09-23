using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Models
{
    public class PlatformEnvironmentIdentitySyncResult
    {
        public bool IsWhatIf { get; set; }

        public List<PlatformEnvironmentIdentityActionResult> Environments { get; } = new List<PlatformEnvironmentIdentityActionResult>();
    }
}
