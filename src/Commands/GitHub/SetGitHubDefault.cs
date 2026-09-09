using System.Management.Automation;
using MSCKite.Azure.Platform.Internal.GitHub;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Commands.GitHub
{
    [Cmdlet(VerbsCommon.Set, "GitHubDefault")]
    [OutputType(typeof(GitHubDefaults))]
    public class SetGitHubDefault : PSCmdlet
    {
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [AllowNull]
        public string Owner { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [AllowNull]
        public string Repository { get; set; }

        // Persists Owner/Repository as defaults for later GitHub commands; pass $null to clear them
        protected override void ProcessRecord()
        {
            WriteObject(GitHubConfigStore.Save(Owner, Repository));
        }
    }
}
