using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Models
{
    public class PlatformWorkflowResult
    {
        public string BranchStrategy { get; set; }

        public string ManifestPath { get; set; }

        public string OutputFolder { get; set; }

        public List<string> Environments { get; } = new List<string>();

        public List<string> CopiedFiles { get; } = new List<string>();

        // Existing files left untouched because -Force wasn't specified
        public List<string> SkippedFiles { get; } = new List<string>();

        public bool Success { get; set; }
    }
}
