using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Models
{
    public class PlatformSecurityGroupSyncResult
    {
        public bool IsWhatIf { get; set; }

        public List<PlatformSecurityGroupActionResult> SecurityGroups { get; } = new List<PlatformSecurityGroupActionResult>();
    }
}
