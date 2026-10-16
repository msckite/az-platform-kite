using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Internal.Platform
{
    // Raw (unresolved) representation of one entry in environments.jsonc's "environments" array
    internal class PlatformEnvironmentConfig
    {
        internal string DisplayName { get; set; }

        internal string EnvironmentCode { get; set; }

        internal string ResourceGroupId { get; set; }

        // Always at least one; each pairs an identity with the GitHub environment that receives its client id
        internal List<PlatformPipelineConfig> Pipelines { get; } = new List<PlatformPipelineConfig>();
    }

    // Raw (unresolved) representation of one entry in an environment's "pipelines" array: a federated identity and the GitHub environment a pipeline signs in with
    internal class PlatformPipelineConfig
    {
        // Short label such as "infra" or "workload", unique within the environment; reported in the cmdlet output
        internal string Purpose { get; set; }

        internal PlatformUserAssignedIdentityConfig UserAssignedIdentity { get; set; }

        internal PlatformGitHubEnvironmentConfig GitHubEnvironment { get; set; }
    }

    // Raw (unresolved) representation of an environment's "githubEnvironment"
    internal class PlatformGitHubEnvironmentConfig
    {
        internal string Name { get; set; }

        internal List<string> RequiredReviewers { get; } = new List<string>();

        internal int WaitTimerMinutes { get; set; }

        // Null when "deploymentBranchPolicy" is omitted, in which case the environment's existing branch policy is left untouched
        internal PlatformDeploymentBranchPolicyConfig DeploymentBranchPolicy { get; set; }

        internal List<PlatformKeyValueConfig> Secrets { get; } = new List<PlatformKeyValueConfig>();

        internal List<PlatformKeyValueConfig> Variables { get; } = new List<PlatformKeyValueConfig>();
    }

    // Branch and tag name patterns (fnmatch, e.g. "main", "release/*", "infra/*") allowed to deploy to a GitHub environment
    internal class PlatformDeploymentBranchPolicyConfig
    {
        internal List<string> Branches { get; } = new List<string>();

        internal List<string> Tags { get; } = new List<string>();
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
