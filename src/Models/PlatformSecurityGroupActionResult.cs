using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Models
{
    public class PlatformSecurityGroupActionResult
    {
        public string DisplayName { get; set; }

        public string MailNickname { get; set; }

        public string ObjectId { get; set; }

        // "Created", "Updated", "Unchanged", "WouldCreate", or "WouldUpdate"
        public string Action { get; set; }

        public List<PlatformRoleAssignmentActionResult> RoleAssignments { get; } = new List<PlatformRoleAssignmentActionResult>();
    }
}
