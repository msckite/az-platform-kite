using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Models
{
    public class PlatformSecurityGroupSyncResult
    {
        public List<PlatformSecurityGroupActionResult> SecurityGroups { get; } = new List<PlatformSecurityGroupActionResult>();
    }
}
