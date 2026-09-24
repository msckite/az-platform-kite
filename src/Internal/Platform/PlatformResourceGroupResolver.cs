using System;
using System.Collections.Generic;
using System.Management.Automation;
using MSCKite.Azure.Platform.Internal.Azure;

namespace MSCKite.Azure.Platform.Internal.Platform
{
    // Resolves platform-config.jsonc resourceGroupId references against Azure; shared by every phase that assigns roles or provisions resources into a resource group
    internal static class PlatformResourceGroupResolver
    {
        // Value of a roleAssignment's "scope" property that targets the subscription instead of a single resource group
        internal const string SubscriptionScope = "subscription";

        // Resolves a role assignment to its ARM scope: the subscription, or the ARM id of one of the resource groups resolved by Resolve() above
        internal static bool TryResolveRoleAssignmentScope(
            PlatformRoleAssignmentConfig roleAssignment,
            Dictionary<string, AzureResourceGroupInfo> resourceGroupsById,
            string subscriptionId,
            out string scope)
        {
            if (string.Equals(roleAssignment.Scope, SubscriptionScope, StringComparison.Ordinal))
            {
                scope = $"/subscriptions/{subscriptionId}";
                return true;
            }

            if (resourceGroupsById.TryGetValue(roleAssignment.ResourceGroupId, out var resourceGroup))
            {
                scope = resourceGroup.ResourceId;
                return true;
            }

            scope = null;
            return false;
        }

        // Verifies every referenced resource group already exists (phase 1 must have run), reporting a clear per-item error rather than failing the whole command
        internal static Dictionary<string, AzureResourceGroupInfo> Resolve(
            PSCmdlet cmdlet,
            List<PlatformResourceGroupConfig> configs,
            Dictionary<string, string> placeholders,
            string errorId)
        {
            var resourceGroupsById = new Dictionary<string, AzureResourceGroupInfo>(StringComparer.Ordinal);

            foreach (var config in configs)
            {
                string name;
                try
                {
                    name = PlatformConfigLoader.ResolvePlaceholders(config.Name, placeholders);
                }
                catch (InvalidOperationException ex)
                {
                    cmdlet.WriteError(new ErrorRecord(ex, errorId, ErrorCategory.InvalidData, config.Id));
                    continue;
                }

                var resourceGroup = AzureResourceGroupHelper.Get(cmdlet, name);
                if (resourceGroup == null)
                {
                    cmdlet.WriteError(new ErrorRecord(
                        new InvalidOperationException($"Resource group '{name}' (id '{config.Id}') does not exist. Run Set-PlatformResourceGroup first."),
                        errorId, ErrorCategory.ObjectNotFound, config.Id));
                    continue;
                }

                cmdlet.WriteVerbose($"Resolved resource group id '{config.Id}' to '{name}' (scope '{resourceGroup.ResourceId}').");
                resourceGroupsById[config.Id] = resourceGroup;
            }

            return resourceGroupsById;
        }
    }
}
