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
    [Cmdlet(VerbsCommon.Get, "PlatformContext")]
    [OutputType(typeof(string))]
    public class GetPlatformContext : PSCmdlet
    {
        private static readonly PowerShellModule[] RequiredModules = {
            new PowerShellModule("Az.Accounts", "5.5")
        };

        [Parameter]
        public SwitchParameter All { get; set; }

        // Ensures all required modules are loaded in the current session before delegating to them
        protected override void BeginProcessing()
        {
            PowerShellModuleLoader.Import(this, (PowerShellModule[])(object)RequiredModules);
        }

        // Combines Get-AzContext with the Azure DevOps REST API auth status and the local GitHub CLI auth status into a single object
        protected override void ProcessRecord()
        {
            var azureContext = AzureContextHelper.GetContext(this, out var azureMessage);
            azureContext = KeepOrDrop(azureContext, azureContext.IsSignedIn, azureMessage);

            var devOpsContext = AdoApiHelper.GetAuthStatus(this, out var devOpsMessage);
            devOpsContext = KeepOrDrop(devOpsContext, devOpsContext.IsSignedIn, devOpsMessage);

            var gitHubContext = GitHubCliHelper.GetAuthStatus(out var gitHubMessage);
            gitHubContext = KeepOrDrop(gitHubContext, gitHubContext.IsSignedIn, gitHubMessage);

            // Only signed-in (or, with -All, always-present) contexts end up as keys, so dropped ones are omitted rather than serialized as null
            var platformContext = new OrderedDictionary();
            if (azureContext != null) platformContext["Azure"] = azureContext;
            if (devOpsContext != null) platformContext["DevOps"] = devOpsContext;
            if (gitHubContext != null) platformContext["GitHub"] = gitHubContext;

            if (platformContext.Count == 0)
            {
                return;
            }

            // Emit ready-to-use JSON so callers don't have to pipe through ConvertTo-Json themselves
            var json = InvokeCommand.InvokeScript(
                SessionState,
                ScriptBlock.Create("param($Context) $Context | ConvertTo-Json -Depth 5"),
                platformContext).FirstOrDefault();

            WriteObject(json?.ToString());
        }

        // Without -All, a not-signed-in context is dropped silently; with -All it's kept and the reason is surfaced as a warning
        private T KeepOrDrop<T>(T context, bool isSignedIn, string message) where T : class
        {
            if (All.IsPresent)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    WriteWarning(message);
                }

                return context;
            }

            return isSignedIn ? context : null;
        }
    }
}
