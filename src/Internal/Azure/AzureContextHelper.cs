using System.Linq;
using System.Management.Automation;
using MSCKite.Azure.Platform.Internal.Platform;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Internal.Azure
{
    internal static class AzureContextHelper
    {
        // Projects only the fields we need so we never have to walk nested PSObject graphs in C#
        private const string GetContextScript = @"
$azContext = Get-AzContext
if ($azContext -and $azContext.Account) {
    [PSCustomObject]@{
        Account        = $azContext.Account.Id
        Tenant         = $azContext.Tenant.Id
        SubscriptionId   = $azContext.Subscription.Id
        SubscriptionName = $azContext.Subscription.Name
        Environment  = $azContext.Environment.Name
    }
}
";

        // Runs Get-AzContext and maps the result into an AzureContext; never throws
        internal static AzureContext GetContext(PSCmdlet cmdlet, out string message)
        {
            message = null;
            var result = cmdlet.InvokeCommand.InvokeScript(GetContextScript).FirstOrDefault();

            if (result == null)
            {
                message = "Not signed in to Azure. Run Connect-AzAccount.";
                return new AzureContext { IsSignedIn = false };
            }

            return new AzureContext
            {
                Account = result.Properties["Account"]?.Value as string,
                Tenant = result.Properties["Tenant"]?.Value as string,
                SubscriptionId = result.Properties["SubscriptionId"]?.Value as string,
                SubscriptionName = result.Properties["SubscriptionName"]?.Value as string,
                Environment = result.Properties["Environment"]?.Value as string,
                IsSignedIn = true
            };
        }

        // Verifies that the active Az context is the tenant and subscription declared in global-config.jsonc
        internal static bool TryGetConfiguredContext(PSCmdlet cmdlet, GlobalConfig globalConfig, out AzureContext azureContext, out string message)
        {
            azureContext = GetContext(cmdlet, out message);
            if (!azureContext.IsSignedIn)
            {
                return false;
            }

            if (!string.Equals(azureContext.Tenant, globalConfig.TenantId, System.StringComparison.OrdinalIgnoreCase))
            {
                message = $"The active Azure tenant '{azureContext.Tenant}' does not match global-config.jsonc tenantId '{globalConfig.TenantId}'.";
                return false;
            }

            if (!string.Equals(azureContext.SubscriptionId, globalConfig.SubscriptionId, System.StringComparison.OrdinalIgnoreCase))
            {
                message = $"The active Azure subscription '{azureContext.SubscriptionId}' does not match global-config.jsonc subscriptionId '{globalConfig.SubscriptionId}'.";
                return false;
            }

            return true;
        }

        // Runs Disconnect-AzAccount for the given username to drop that specific cached Az context
        internal static void SignOut(PSCmdlet cmdlet, string username)
        {
            cmdlet.InvokeCommand.InvokeScript(
                "param($Username) Disconnect-AzAccount -Username $Username | Out-Null",
                username);
        }
    }
}
