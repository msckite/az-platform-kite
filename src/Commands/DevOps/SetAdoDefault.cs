using System.Management.Automation;
using MSCKite.Azure.Platform.Internal.DevOps;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Commands.DevOps
{
    [Cmdlet(VerbsCommon.Set, "AdoDefault")]
    [OutputType(typeof(AdoDefaults))]
    public class SetAdoDefault : PSCmdlet
    {
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [AllowNull]
        public string Organization { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [AllowNull]
        public string Project { get; set; }

        // Persists Organization/Project as defaults for later Azure DevOps commands; pass $null to clear them
        protected override void ProcessRecord()
        {
            WriteObject(AdoConfigStore.Save(Organization, Project));
        }
    }
}
