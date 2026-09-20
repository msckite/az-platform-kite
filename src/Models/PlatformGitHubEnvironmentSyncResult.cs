using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Models
{
    public class PlatformGitHubEnvironmentSyncResult
    {
        public List<PlatformGitHubEnvironmentActionResult> Environments { get; } = new List<PlatformGitHubEnvironmentActionResult>();
    }
}
