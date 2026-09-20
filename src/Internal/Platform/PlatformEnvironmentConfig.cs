using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Internal.Platform
{
    // Raw (unresolved) representation of one entry in platform-config.jsonc's "environments" array
    internal class PlatformEnvironmentConfig
    {
        internal string DisplayName { get; set; }

        internal string EnvironmentCode { get; set; }

        internal string ResourceGroupId { get; set; }

        internal PlatformUserAssignedIdentityConfig UserAssignedIdentity { get; set; }

        internal PlatformGitHubEnvironmentConfig GitHubEnvironment { get; set; }
    }

    // Raw (unresolved) representation of an environment's "githubEnvironment"
    internal class PlatformGitHubEnvironmentConfig
    {
        internal string Name { get; set; }

        internal List<string> RequiredReviewers { get; } = new List<string>();

        internal int WaitTimerMinutes { get; set; }

        internal List<PlatformKeyValueConfig> Secrets { get; } = new List<PlatformKeyValueConfig>();

        internal List<PlatformKeyValueConfig> Variables { get; } = new List<PlatformKeyValueConfig>();
    }

    // Raw (unresolved) name/value pair, used for both githubEnvironment secrets and variables
    internal class PlatformKeyValueConfig
    {
        internal string Name { get; set; }

        internal string Value { get; set; }
    }

    // Raw (unresolved) representation of an environment's "userAssignedIdentity"
    internal class PlatformUserAssignedIdentityConfig
    {
        internal string Name { get; set; }

        internal PlatformFederatedCredentialConfig FederatedCredential { get; set; }

        internal List<PlatformRoleAssignmentConfig> RoleAssignments { get; } = new List<PlatformRoleAssignmentConfig>();
    }

    // Raw (unresolved) representation of a "federatedCredential"
    internal class PlatformFederatedCredentialConfig
    {
        internal string Name { get; set; }

        internal string Issuer { get; set; }

        // "environment", "branch", or "pull_request"; the command resolves this to the immutable OIDC subject at runtime
        internal string SubjectType { get; set; }

        internal List<string> Audiences { get; } = new List<string>();
    }
}
