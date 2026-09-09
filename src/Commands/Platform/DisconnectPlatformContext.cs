using System.Collections.Specialized;
using System.Linq;
using System.Management.Automation;
using MSCKite.Azure.Platform.Internal.Azure;
using MSCKite.Azure.Platform.Internal.Common;
using MSCKite.Azure.Platform.Internal.DevOps;
using MSCKite.Azure.Platform.Internal.GitHub;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Commands.Platform
{
    [Cmdlet(VerbsCommunications.Disconnect, "PlatformContext")]
    [OutputType(typeof(string))]
    public class DisconnectPlatformContext : PSCmdlet
    {
        private static readonly PowerShellModule[] RequiredModules = {
            new PowerShellModule("Az.Accounts", "5.5")
        };

        protected override void BeginProcessing()
        {
            PowerShellModuleLoader.Import(this, (PowerShellModule[])(object)RequiredModules);
        }

        // Signs out of every context (Azure, Azure DevOps, GitHub) that is currently signed in; already signed-out contexts are left untouched
        protected override void ProcessRecord()
        {
            var results = new OrderedDictionary();

            var azureContext = AzureContextHelper.GetContext(this, out _);
            var devOpsContext = AdoApiHelper.GetAuthStatus(this, out _);
            var gitHubContext = GitHubCliHelper.GetAuthStatus(out _);

            if (azureContext.IsSignedIn)
            {
                AzureContextHelper.SignOut(this, azureContext.Account);
                results["Azure"] = "Signed out";
            }

            if (devOpsContext.IsSignedIn)
            {
                AdoSessionState.Clear();
                results["DevOps"] = "Signed out";
            }

            if (gitHubContext.IsSignedIn)
            {
                GitHubCliHelper.SignOut(gitHubContext.Host, gitHubContext.Account);
                results["GitHub"] = "Signed out";
            }

            if (results.Count == 0)
            {
                WriteObject("Already signed out of Azure, Azure DevOps, and GitHub.");
                return;
            }

            var json = InvokeCommand.InvokeScript(
                SessionState,
                ScriptBlock.Create("param($Result) $Result | ConvertTo-Json -Depth 3"),
                results).FirstOrDefault();

            WriteObject(json?.ToString());
        }
    }
}
