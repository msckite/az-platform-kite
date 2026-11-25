using System;
using System.Collections.Generic;
using System.Management.Automation;
using MSCKite.Azure.Platform.Internal.Azure;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Internal.Platform.Phases
{
    // Phase 2: creates or updates the Microsoft Entra security groups declared in the platform config files, and assigns their RBAC roles against the resource groups from phase 1
    internal static class SecurityGroupPhase
    {
        internal static List<PlatformSecurityGroupActionResult> Run(
            PSCmdlet cmdlet,
            GlobalConfig globalConfig,
            List<PlatformSecurityGroupConfig> securityGroupConfigs,
            Dictionary<string, AzureResourceGroupInfo> resourceGroupsById)
        {
            var placeholders = globalConfig.ToPlaceholderMap();
            var items = new List<PlatformSecurityGroupActionResult>();

            foreach (var config in securityGroupConfigs)
            {
                string displayName;
                string mailNickname;
                string description;
                try
                {
                    displayName = PlatformConfigLoader.ResolvePlaceholders(config.DisplayName, placeholders);
                    mailNickname = PlatformConfigLoader.ResolvePlaceholders(config.MailNickname, placeholders);
                    description = PlatformConfigLoader.ResolvePlaceholders(config.Description, placeholders);
                }
                catch (InvalidOperationException ex)
                {
                    cmdlet.WriteError(new ErrorRecord(ex, "PlatformSecurityGroupUnresolvedPlaceholder", ErrorCategory.InvalidData, config.DisplayName));
                    continue;
                }

                var actionResult = SyncSecurityGroup(cmdlet, displayName, mailNickname, description);
                if (actionResult == null)
                {
                    continue;
                }

                SyncRoleAssignments(cmdlet, actionResult, config, resourceGroupsById, globalConfig.SubscriptionId, placeholders);
                items.Add(actionResult);
            }

            cmdlet.WriteVerbose($"Done. Processed {items.Count} of {securityGroupConfigs.Count} security group(s).");
            return items;
        }

        // Creates the group if missing, or updates its display name / description if either has drifted; returns null (after recording an error) if creation didn't succeed
        private static PlatformSecurityGroupActionResult SyncSecurityGroup(PSCmdlet cmdlet, string displayName, string mailNickname, string description)
        {
            cmdlet.WriteVerbose($"Security group '{mailNickname}': checking whether it already exists.");
            var existing = AzureAdGroupHelper.Get(cmdlet, mailNickname);

            if (existing == null)
            {
                cmdlet.WriteVerbose($"Security group '{mailNickname}' does not exist yet.");
                if (!cmdlet.ShouldProcess(mailNickname, "Create security group"))
                {
                    return new PlatformSecurityGroupActionResult
                    {
                        DisplayName = displayName,
                        MailNickname = mailNickname,
                        Action = "WouldCreate"
                    };
                }

                cmdlet.WriteVerbose($"Creating security group '{mailNickname}'.");
                AzureAdGroupInfo created;
                try
                {
                    created = AzureAdGroupHelper.Create(cmdlet, displayName, mailNickname, description);
                }
                catch (Exception ex)
                {
                    cmdlet.WriteError(new ErrorRecord(
                        new InvalidOperationException($"Failed to create security group '{mailNickname}': {ex.Message}", ex),
                        "PlatformSecurityGroupCreateFailed", ErrorCategory.WriteError, mailNickname));
                    return null;
                }

                if (created == null)
                {
                    cmdlet.WriteError(new ErrorRecord(
                        new InvalidOperationException($"Security group '{mailNickname}' was created but is not yet readable. It may still be propagating; re-run this command to verify."),
                        "PlatformSecurityGroupNotReadable", ErrorCategory.ReadError, mailNickname));
                    return null;
                }

                cmdlet.WriteVerbose($"Security group '{mailNickname}' created and confirmed readable (object id '{created.Id}').");
                return new PlatformSecurityGroupActionResult
                {
                    DisplayName = created.DisplayName,
                    MailNickname = created.MailNickname,
                    ObjectId = created.Id,
                    Action = "Created"
                };
            }

            cmdlet.WriteVerbose($"Security group '{mailNickname}' already exists (object id '{existing.Id}').");

            var isUnchanged = string.Equals(existing.DisplayName, displayName, StringComparison.Ordinal) &&
                               string.Equals(existing.Description ?? string.Empty, description ?? string.Empty, StringComparison.Ordinal);

            if (isUnchanged)
            {
                cmdlet.WriteVerbose($"Security group '{mailNickname}' display name and description already match; nothing to do.");
                return new PlatformSecurityGroupActionResult
                {
                    DisplayName = existing.DisplayName,
                    MailNickname = existing.MailNickname,
                    ObjectId = existing.Id,
                    Action = "Unchanged"
                };
            }

            cmdlet.WriteVerbose($"Security group '{mailNickname}' display name or description has drifted from the configured values.");
            if (!cmdlet.ShouldProcess(mailNickname, "Update security group"))
            {
                return new PlatformSecurityGroupActionResult
                {
                    DisplayName = displayName,
                    MailNickname = existing.MailNickname,
                    ObjectId = existing.Id,
                    Action = "WouldUpdate"
                };
            }

            cmdlet.WriteVerbose($"Updating security group '{mailNickname}'.");
            AzureAdGroupHelper.Update(cmdlet, existing.Id, displayName, description);

            return new PlatformSecurityGroupActionResult
            {
                DisplayName = displayName,
                MailNickname = existing.MailNickname,
                ObjectId = existing.Id,
                Action = "Updated"
            };
        }

        // Assigns every configured role that the group doesn't already hold; existing role assignments outside this list are left untouched
        private static void SyncRoleAssignments(
            PSCmdlet cmdlet,
            PlatformSecurityGroupActionResult actionResult,
            PlatformSecurityGroupConfig config,
            Dictionary<string, AzureResourceGroupInfo> resourceGroupsById,
            string subscriptionId,
            Dictionary<string, string> placeholders)
        {
            foreach (var roleAssignment in config.RoleAssignments)
            {
                if (!PlatformResourceGroupResolver.TryResolveRoleAssignmentScope(roleAssignment, resourceGroupsById, subscriptionId, out var scope))
                {
                    // Missing scope was already reported by PlatformResourceGroupResolver
                    continue;
                }

                string role;
                try
                {
                    role = PlatformConfigLoader.ResolvePlaceholders(roleAssignment.Role, placeholders);
                }
                catch (InvalidOperationException ex)
                {
                    cmdlet.WriteError(new ErrorRecord(ex, "PlatformSecurityGroupUnresolvedPlaceholder", ErrorCategory.InvalidData, config.DisplayName));
                    continue;
                }

                if (actionResult.Action == "WouldCreate")
                {
                    actionResult.RoleAssignments.Add(new PlatformRoleAssignmentActionResult { Role = role, Scope = scope, Action = "WouldAdd" });
                    continue;
                }

                if (AzureRoleAssignmentHelper.Exists(cmdlet, actionResult.ObjectId, scope, role))
                {
                    cmdlet.WriteVerbose($"Security group '{actionResult.MailNickname}' already has role '{role}' at scope '{scope}'.");
                    actionResult.RoleAssignments.Add(new PlatformRoleAssignmentActionResult { Role = role, Scope = scope, Action = "Unchanged" });
                    continue;
                }

                if (!cmdlet.ShouldProcess(actionResult.MailNickname, $"Assign role '{role}' at scope '{scope}'"))
                {
                    actionResult.RoleAssignments.Add(new PlatformRoleAssignmentActionResult { Role = role, Scope = scope, Action = "WouldAdd" });
                    continue;
                }

                cmdlet.WriteVerbose($"Assigning role '{role}' to security group '{actionResult.MailNickname}' at scope '{scope}'.");
                try
                {
                    AzureRoleAssignmentHelper.Create(cmdlet, actionResult.ObjectId, scope, role);
                    actionResult.RoleAssignments.Add(new PlatformRoleAssignmentActionResult { Role = role, Scope = scope, Action = "Added" });
                }
                catch (InvalidOperationException ex)
                {
                    cmdlet.WriteError(new ErrorRecord(ex, "PlatformSecurityGroupRoleAssignmentFailed", ErrorCategory.WriteError, config.DisplayName));
                }
            }
        }
    }
}
