using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Models
{
    public class PlatformGitHubEnvironmentActionResult
    {
        public string EnvironmentCode { get; set; }

        // "infra" or "workload"
        public string Purpose { get; set; }

        public string Name { get; set; }

        // "Created", "Updated", "WouldCreate", or "WouldUpdate"
        public string Action { get; set; }

        public List<PlatformKeyValueActionResult> Secrets { get; } = new List<PlatformKeyValueActionResult>();

        public List<PlatformKeyValueActionResult> Variables { get; } = new List<PlatformKeyValueActionResult>();
    }
}
