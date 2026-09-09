using System.Management.Automation;
using MSCKite.Azure.Platform.Internal.DevOps;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Commands.DevOps
{
    [Cmdlet(VerbsCommon.Get, "AdoDefault")]
    [OutputType(typeof(AdoDefaults))]
    public class GetAdoDefault : PSCmdlet
    {
        // Returns the Organization/CollectionUri/Project currently stored via Set-AdoDefault
        protected override void ProcessRecord()
        {
            WriteObject(AdoConfigStore.Load());
        }
    }
}
