using System.Linq;
using System.Management.Automation;

namespace MSCKite.Azure.Platform.Internal.Azure
{
    // Current state of a Microsoft Entra security group, as returned by Get-AzADGroup
    internal class AzureAdGroupInfo
    {
        internal string Id { get; set; }

        internal string DisplayName { get; set; }

        internal string MailNickname { get; set; }

        internal string Description { get; set; }
    }

    // Creates, updates, and reads Microsoft Entra security groups via the Az.Resources module
    internal static class AzureAdGroupHelper
    {
        // Get-AzADGroup has no -MailNickname parameter; lookup is done via an OData filter instead
        private const string GetScript = @"
param($MailNickname)
$escaped = $MailNickname -replace ""'"", ""''""
$group = Get-AzADGroup -Filter ""mailNickname eq '$escaped'"" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($group) {
    [PSCustomObject]@{
        Id           = $group.Id
        DisplayName  = $group.DisplayName
        MailNickname = $group.MailNickname
        Description  = $group.Description
    }
}
";

        // A mail-enabled security group is rejected by Graph, so MailEnabled must stay off; -ErrorAction Stop makes creation failures throw instead of silently no-op'ing
        private const string CreateScript = @"
param($DisplayName, $MailNickname, $Description)
New-AzADGroup -DisplayName $DisplayName -MailNickname $MailNickname -Description $Description -SecurityEnabled -MailEnabled:$false -ErrorAction Stop | Out-Null
";

        // There is no Set-AzADGroup cmdlet; Update-AzADGroup is the correct one
        private const string UpdateScript = @"
param($ObjectId, $DisplayName, $Description)
Update-AzADGroup -ObjectId $ObjectId -DisplayName $DisplayName -Description $Description -ErrorAction Stop | Out-Null
";

        // Returns null if the group doesn't exist (or isn't yet visible)
        internal static AzureAdGroupInfo Get(PSCmdlet cmdlet, string mailNickname)
        {
            var result = cmdlet.InvokeCommand.InvokeScript(GetScript, mailNickname).FirstOrDefault();
            if (result == null)
            {
                return null;
            }

            return new AzureAdGroupInfo
            {
                Id = result.Properties["Id"]?.Value as string,
                DisplayName = result.Properties["DisplayName"]?.Value as string,
                MailNickname = result.Properties["MailNickname"]?.Value as string,
                Description = result.Properties["Description"]?.Value as string
            };
        }

        // Creates the group, then waits for it to become readable before returning (Entra directory writes can lag reads); throws if creation itself failed
        internal static AzureAdGroupInfo Create(PSCmdlet cmdlet, string displayName, string mailNickname, string description)
        {
            cmdlet.InvokeCommand.InvokeScript(CreateScript, displayName, mailNickname, description);
            return AzurePropagationHelper.WaitUntilReadable(() => Get(cmdlet, mailNickname), cmdlet.WriteVerbose);
        }

        internal static void Update(PSCmdlet cmdlet, string objectId, string displayName, string description)
        {
            cmdlet.InvokeCommand.InvokeScript(UpdateScript, objectId, displayName, description);
        }
    }
}
