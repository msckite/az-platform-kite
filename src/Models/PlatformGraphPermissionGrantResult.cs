using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Models
{
    public class PlatformGraphPermissionGrantResult
    {
        public string PrincipalId { get; set; }

        public List<PlatformGraphPermissionActionResult> Permissions { get; } = new List<PlatformGraphPermissionActionResult>();
    }
}
