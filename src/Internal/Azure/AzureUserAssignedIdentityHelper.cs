using System;
using System.Linq;
using System.Management.Automation;

namespace MSCKite.Azure.Platform.Internal.Azure
{
    // Current state of a user-assigned managed identity, as returned by Get-AzUserAssignedIdentity
    internal class AzureUserAssignedIdentityInfo
    {
        internal string Id { get; set; }

        internal string Name { get; set; }

        internal string Location { get; set; }

        internal string PrincipalId { get; set; }

        internal string ClientId { get; set; }

        internal string TenantId { get; set; }
    }

    // Creates and reads user-assigned managed identities via the Az.ManagedServiceIdentity module
    internal static class AzureUserAssignedIdentityHelper
    {
        private const string GetScript = @"
param($ResourceGroupName, $Name)
$identity = Get-AzUserAssignedIdentity -ResourceGroupName $ResourceGroupName -Name $Name -ErrorAction SilentlyContinue
if ($identity) {
    [PSCustomObject]@{
        Id          = $identity.Id
        Name        = $identity.Name
        Location    = $identity.Location
        PrincipalId = $identity.PrincipalId
        ClientId    = $identity.ClientId
        TenantId    = $identity.TenantId
    }
}
";

        private const string CreateScript = @"
param($ResourceGroupName, $Name, $Location)
New-AzUserAssignedIdentity -ResourceGroupName $ResourceGroupName -Name $Name -Location $Location -ErrorAction Stop | Out-Null
";

        // Returns null if the identity doesn't exist, or exists but its PrincipalId hasn't propagated yet (role assignments would fail against an empty principal)
        internal static AzureUserAssignedIdentityInfo Get(PSCmdlet cmdlet, string resourceGroupName, string name)
        {
            var result = cmdlet.InvokeCommand.InvokeScript(GetScript, resourceGroupName, name).FirstOrDefault();
            if (result == null)
            {
                return null;
            }

            var info = new AzureUserAssignedIdentityInfo
            {
                Id = result.Properties["Id"]?.Value as string,
                Name = result.Properties["Name"]?.Value as string,
                Location = result.Properties["Location"]?.Value as string,
                PrincipalId = result.Properties["PrincipalId"]?.Value as string,
                ClientId = result.Properties["ClientId"]?.Value as string,
                TenantId = result.Properties["TenantId"]?.Value as string
            };

            return string.IsNullOrEmpty(info.PrincipalId) ? null : info;
        }

        // Creates the identity, then waits for its PrincipalId to become readable before returning; throws if creation itself failed
        internal static AzureUserAssignedIdentityInfo Create(PSCmdlet cmdlet, string resourceGroupName, string name, string location)
        {
            cmdlet.InvokeCommand.InvokeScript(CreateScript, resourceGroupName, name, location);
            return AzurePropagationHelper.WaitUntilReadable(() => Get(cmdlet, resourceGroupName, name), cmdlet.WriteVerbose);
        }
    }
}
