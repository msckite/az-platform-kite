using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Models
{
    public class PlatformConfigFolderResult
    {
        public string ConfigRootPath { get; set; }

        public List<string> CreatedFolders { get; } = new List<string>();

        public string GlobalConfigPath { get; set; }

        public bool Success { get; set; }
    }
}
