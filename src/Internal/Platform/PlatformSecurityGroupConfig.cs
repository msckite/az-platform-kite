using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Internal.Platform
{
    // Raw (unresolved) representation of one entry in platform-config.jsonc's "securityGroups" array
    internal class PlatformSecurityGroupConfig
    {
        internal string DisplayName { get; set; }

        internal string MailNickname { get; set; }

        internal string Description { get; set; }

        internal List<PlatformRoleAssignmentConfig> RoleAssignments { get; } = new List<PlatformRoleAssignmentConfig>();
    }

    // Raw (unresolved) representation of one entry in a "roleAssignments" array
    internal class PlatformRoleAssignmentConfig
    {
        internal string Role { get; set; }

        // Exactly one of ResourceGroupId or Scope is set; Scope is "subscription" for a subscription-wide role assignment
        internal string ResourceGroupId { get; set; }

        internal string Scope { get; set; }
    }
}
