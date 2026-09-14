using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Models
{
    public class GitHubLabelSyncResult
    {
        public string Owner { get; set; }

        public string Repository { get; set; }

        public List<string> Added { get; } = new List<string>();

        public List<string> Updated { get; } = new List<string>();

        public List<string> Removed { get; } = new List<string>();

        public List<string> Unchanged { get; } = new List<string>();
    }
}
