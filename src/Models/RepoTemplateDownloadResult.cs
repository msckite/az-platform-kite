using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Models
{
    public class RepoTemplateDownloadResult
    {
        public string RepositoryUrl { get; set; }

        public string Branch { get; set; }

        public string OutputFolder { get; set; }

        public List<string> CopiedFolders { get; } = new List<string>();

        public List<string> CopiedFiles { get; } = new List<string>();

        public bool Success { get; set; }
    }
}
