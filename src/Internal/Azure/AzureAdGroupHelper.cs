using System;
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
        // Get-AzADGroup has no -MailNickname parameter; lookup is done via an OData filter instead.
        // Lookup failures (e.g. the caller's identity lacks the Microsoft Graph Group.Read.All permission) are captured
        // and re-thrown rather than swallowed, so they aren't misreported as "the group doesn't exist".
        private const string GetScript = @"
param($MailNickname)
$escaped = $MailNickname -replace ""'"", ""''""
$lookupError = $null
$group = Get-AzADGroup -Filter ""mailNickname eq '$escaped'"" -ErrorAction SilentlyContinue -ErrorVariable lookupError | Select-Object -First 1
if ($lookupError) {
    throw $lookupError[0]
}
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

        // Returns null if the group doesn't exist (or isn't yet visible); throws if the lookup itself failed (e.g. missing Graph permissions),
        // so a permission problem in CI surfaces as an error instead of being misreported as "the group doesn't exist yet"
        internal static AzureAdGroupInfo Get(PSCmdlet cmdlet, string mailNickname)
        {
            PSObject result;
            try
            {
                result = cmdlet.InvokeCommand.InvokeScript(GetScript, mailNickname).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to look up security group '{mailNickname}' in Microsoft Entra ID. This is usually caused by the identity running this command missing the Microsoft Graph 'Group.Read.All' (or 'Directory.Read.All') permission: {ex.Message}",
                    ex);
            }

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
