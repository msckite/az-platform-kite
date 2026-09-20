using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Models
{
    public class PlatformResourceGroupSyncResult
    {
        public List<PlatformResourceGroupActionResult> ResourceGroups { get; } = new List<PlatformResourceGroupActionResult>();
    }
}
