using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Models
{
    public class PlatformDeploymentResult
    {
        public bool IsWhatIf { get; set; }

        // Name of the phase whose errors stopped the run ("ResourceGroups" or "EnvironmentIdentities"); null when every phase ran
        public string StoppedAtPhase { get; set; }

        // Every phase that reported at least one error, in run order
        public List<string> PhasesWithErrors { get; } = new List<string>();

        public List<PlatformResourceGroupActionResult> ResourceGroups { get; set; } = new List<PlatformResourceGroupActionResult>();

        public List<PlatformSecurityGroupActionResult> SecurityGroups { get; set; } = new List<PlatformSecurityGroupActionResult>();

        public List<PlatformEnvironmentIdentityActionResult> EnvironmentIdentities { get; set; } = new List<PlatformEnvironmentIdentityActionResult>();

        public List<PlatformGitHubEnvironmentActionResult> GitHubEnvironments { get; set; } = new List<PlatformGitHubEnvironmentActionResult>();
    }
}
