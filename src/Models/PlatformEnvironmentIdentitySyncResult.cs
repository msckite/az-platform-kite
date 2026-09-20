using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Models
{
    public class PlatformEnvironmentIdentitySyncResult
    {
        public List<PlatformEnvironmentIdentityActionResult> Environments { get; } = new List<PlatformEnvironmentIdentityActionResult>();
    }
}
