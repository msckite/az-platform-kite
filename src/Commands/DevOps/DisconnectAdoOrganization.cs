using System.Management.Automation;
using MSCKite.Azure.Platform.Internal.DevOps;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Commands.DevOps
{
    [Cmdlet(VerbsCommunications.Disconnect, "AdoOrganization")]
    [OutputType(typeof(DevOpsContext))]
    public class DisconnectAdoOrganization : PSCmdlet
    {
        // Returns the context that was disconnected, or null if the session was already disconnected
        protected override void ProcessRecord()
        {
            var context = AdoSessionState.Current;
            AdoSessionState.Clear();

            if (context != null)
            {
                context.IsSignedIn = false;
            }

            WriteObject(context, false);
        }
    }
}
