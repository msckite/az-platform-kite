using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MSCKite.Azure.Platform.Internal.Azure
{
    // Current state of a federated identity credential, as returned by Get-AzFederatedIdentityCredential
    internal class AzureFederatedCredentialInfo
    {
        internal string Issuer { get; set; }

        internal string Subject { get; set; }

        internal List<string> Audiences { get; } = new List<string>();
    }

    // Creates, updates, and reads federated identity credentials on a user-assigned managed identity via the Az.ManagedServiceIdentity module
    internal static class AzureFederatedCredentialHelper
    {
        private const string GetScript = @"
param($ResourceGroupName, $IdentityName, $Name)
$credential = Get-AzFederatedIdentityCredential -ResourceGroupName $ResourceGroupName -IdentityName $IdentityName -Name $Name -ErrorAction SilentlyContinue
if ($credential) {
    [PSCustomObject]@{
        Issuer   = $credential.Issuer
        Subject  = $credential.Subject
        Audience = @($credential.Audience)
    }
}
";

        private const string CreateScript = @"
param($ResourceGroupName, $IdentityName, $Name, $Issuer, $Subject, $Audience)
New-AzFederatedIdentityCredential -ResourceGroupName $ResourceGroupName -IdentityName $IdentityName -Name $Name -Issuer $Issuer -Subject $Subject -Audience $Audience -ErrorAction Stop | Out-Null
";

        private const string UpdateScript = @"
param($ResourceGroupName, $IdentityName, $Name, $Issuer, $Subject, $Audience)
Update-AzFederatedIdentityCredential -ResourceGroupName $ResourceGroupName -IdentityName $IdentityName -Name $Name -Issuer $Issuer -Subject $Subject -Audience $Audience -ErrorAction Stop | Out-Null
";

        internal static AzureFederatedCredentialInfo Get(PSCmdlet cmdlet, string resourceGroupName, string identityName, string name)
        {
            var result = cmdlet.InvokeCommand.InvokeScript(GetScript, resourceGroupName, identityName, name).FirstOrDefault();
            if (result == null)
            {
                return null;
            }

            var info = new AzureFederatedCredentialInfo
            {
                Issuer = result.Properties["Issuer"]?.Value as string,
                Subject = result.Properties["Subject"]?.Value as string
            };

            if (result.Properties["Audience"]?.Value is IEnumerable<object> audiences)
            {
                info.Audiences.AddRange(audiences.Select(a => a?.ToString()).Where(a => a != null));
            }

            return info;
        }

        internal static void Create(PSCmdlet cmdlet, string resourceGroupName, string identityName, string name, string issuer, string subject, string[] audiences)
        {
            cmdlet.InvokeCommand.InvokeScript(CreateScript, resourceGroupName, identityName, name, issuer, subject, audiences);
        }

        internal static void Update(PSCmdlet cmdlet, string resourceGroupName, string identityName, string name, string issuer, string subject, string[] audiences)
        {
            cmdlet.InvokeCommand.InvokeScript(UpdateScript, resourceGroupName, identityName, name, issuer, subject, audiences);
        }
    }
}
