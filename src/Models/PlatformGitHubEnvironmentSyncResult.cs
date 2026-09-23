using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Models
{
    public class PlatformGitHubEnvironmentSyncResult
    {
        public bool IsWhatIf { get; set; }

        public List<PlatformGitHubEnvironmentActionResult> Environments { get; } = new List<PlatformGitHubEnvironmentActionResult>();
    }
}
