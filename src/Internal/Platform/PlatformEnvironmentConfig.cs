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

        // Only the "name" is needed by phase 3 (to compute the federated credential subject); the rest of githubEnvironment is parsed in phase 4
        internal string GitHubEnvironmentName { get; set; }
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
