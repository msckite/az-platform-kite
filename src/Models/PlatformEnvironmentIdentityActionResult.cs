using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Models
{
    public class PlatformEnvironmentIdentityActionResult
    {
        public string EnvironmentCode { get; set; }

        public string IdentityName { get; set; }

        public string PrincipalId { get; set; }

        public string ClientId { get; set; }

        // "Created", "Unchanged", or "WouldCreate"
        public string Action { get; set; }

        // "Created", "Updated", "Unchanged", "WouldCreate", or "WouldUpdate"
        public string FederatedCredentialAction { get; set; }

        public List<PlatformRoleAssignmentActionResult> RoleAssignments { get; } = new List<PlatformRoleAssignmentActionResult>();
    }
}
