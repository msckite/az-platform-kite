using System;
using System.Net.Http;
using System.Management.Automation;
using MSCKite.Azure.Platform.Internal.Common;
using MSCKite.Azure.Platform.Internal.DevOps;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Commands.DevOps
{
    [Cmdlet(VerbsCommunications.Connect, "AdoOrganization")]
    [OutputType(typeof(DevOpsContext))]
    public class ConnectAdoOrganization : PSCmdlet
    {
        private static readonly PowerShellModule[] RequiredModules = {
            new PowerShellModule("Az.Accounts", "5.5")
        };

        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [AllowNull]
        [Alias("O")]
        public string Organization { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [AllowNull]
        [Alias("P")]
        public string Project { get; set; }

        protected override void BeginProcessing()
        {
            PowerShellModuleLoader.Import(this, (PowerShellModule[])(object)RequiredModules);
        }

        // Validates auth against Azure DevOps once and marks the session as connected; throws a terminating error on failure
        protected override void ProcessRecord()
        {
            try
            {
                WriteObject(AdoApiHelper.Connect(this, Organization, Project));
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is HttpRequestException || ex is UriFormatException)
            {
                ThrowTerminatingError(new ErrorRecord(ex, "DevOpsConnectFailed", ErrorCategory.AuthenticationError, null));
            }
        }
    }
}
