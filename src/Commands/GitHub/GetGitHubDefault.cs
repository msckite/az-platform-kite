using System.Management.Automation;
using MSCKite.Azure.Platform.Internal.GitHub;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Commands.GitHub
{
    [Cmdlet(VerbsCommon.Get, "GitHubDefault")]
    [OutputType(typeof(GitHubDefaults))]
    public class GetGitHubDefault : PSCmdlet
    {
        // Returns the Owner/Repository currently stored via Set-GitHubDefault
        protected override void ProcessRecord()
        {
            WriteObject(GitHubConfigStore.Load());
        }
    }
}
