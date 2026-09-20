using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Models
{
    public class PlatformEnvironmentIdentityActionResult
    {
        public string EnvironmentCode { get; set; }

        public string IdentityName { get; set; }

        public string PrincipalId { get; set; }

        public string ClientId { get; set; }

        // "Created" or "Unchanged"
        public string Action { get; set; }

        // "Created", "Updated", or "Unchanged"
        public string FederatedCredentialAction { get; set; }

        public List<PlatformRoleAssignmentActionResult> RoleAssignments { get; } = new List<PlatformRoleAssignmentActionResult>();
    }
}
