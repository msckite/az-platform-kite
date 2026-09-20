using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Internal.Platform
{
    // Raw (unresolved) representation of one entry in platform-config.jsonc's "securityGroups" array
    internal class PlatformSecurityGroupConfig
    {
        internal string DisplayName { get; set; }

        internal string MailNickName { get; set; }

        internal string Description { get; set; }

        internal List<PlatformRoleAssignmentConfig> RoleAssignments { get; } = new List<PlatformRoleAssignmentConfig>();
    }

    // Raw (unresolved) representation of one entry in a "roleAssignments" array
    internal class PlatformRoleAssignmentConfig
    {
        internal string Role { get; set; }

        internal string ResourceGroupId { get; set; }
    }
}
