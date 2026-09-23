using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Text.Json;
using MSCKite.Azure.Platform.Internal.Azure;
using MSCKite.Azure.Platform.Internal.Common;
using MSCKite.Azure.Platform.Internal.Platform;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Commands.Platform
{
    // Phase 2: creates or updates the Microsoft Entra security groups declared in platform-config.jsonc, and assigns their RBAC roles against the resource groups from phase 1
    [Cmdlet(VerbsCommon.Set, "PlatformSecurityGroup", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
    [OutputType(typeof(PlatformSecurityGroupActionResult))]
    [OutputType(typeof(Hashtable))]
    public class SetPlatformSecurityGroup : PSCmdlet
    {
        private static readonly PowerShellModule[] RequiredModules = {
            new PowerShellModule("Az.Resources", "6.0")
        };

        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string GlobalConfigPath { get; set; } = Path.Combine("config", "global-config.jsonc");

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string PlatformConfigPath { get; set; } = Path.Combine("config", "platform-config.jsonc");

        // Collects every result in memory and emits a single summary Hashtable at the end, instead of streaming each result as it's created
        [Parameter]
        public SwitchParameter AsHashtable { get; set; }

        protected override void BeginProcessing()
        {
            PowerShellModuleLoader.Import(this, (PowerShellModule[])(object)RequiredModules);
        }

        protected override void ProcessRecord()
        {
            var globalConfigPath = GetUnresolvedProviderPathFromPSPath(GlobalConfigPath);
            var platformConfigPath = GetUnresolvedProviderPathFromPSPath(PlatformConfigPath);

            WriteVerbose($"Loading global config from '{globalConfigPath}'.");
            WriteVerbose($"Loading platform config from '{platformConfigPath}'.");

            GlobalConfig globalConfig;
            List<PlatformResourceGroupConfig> resourceGroupConfigs;
            List<PlatformSecurityGroupConfig> securityGroupConfigs;
            try
            {
                globalConfig = PlatformConfigLoader.LoadGlobalConfig(globalConfigPath);
                resourceGroupConfigs = PlatformConfigLoader.LoadResourceGroups(platformConfigPath);
                securityGroupConfigs = PlatformConfigLoader.LoadSecurityGroups(platformConfigPath);
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidOperationException || ex is JsonException)
            {
                ThrowTerminatingError(new ErrorRecord(ex, "PlatformSecurityGroupInvalidConfig", ErrorCategory.InvalidData, PlatformConfigPath));
                return;
            }

            WriteVerbose($"Found {securityGroupConfigs.Count} security group(s) in platform config.");

            var azureContext = AzureContextHelper.GetContext(this, out var azureMessage);
            if (!azureContext.IsSignedIn)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new InvalidOperationException(azureMessage),
                    "PlatformSecurityGroupNotSignedIn", ErrorCategory.AuthenticationError, null));
                return;
            }

            var placeholders = globalConfig.ToPlaceholderMap();

            // Resolve every resourceGroupId referenced anywhere in this config up front, so a missing phase-1 resource group fails fast with a clear message
            var scopesById = ResolveResourceGroupScopes(resourceGroupConfigs, placeholders);

            var items = new List<PlatformSecurityGroupActionResult>();

            foreach (var config in securityGroupConfigs)
            {
                string displayName;
                string mailNickName;
                string description;
                try
                {
                    displayName = PlatformConfigLoader.ResolvePlaceholders(config.DisplayName, placeholders);
                    mailNickName = PlatformConfigLoader.ResolvePlaceholders(config.MailNickName, placeholders);
                    description = PlatformConfigLoader.ResolvePlaceholders(config.Description, placeholders);
                }
                catch (InvalidOperationException ex)
                {
                    WriteError(new ErrorRecord(ex, "PlatformSecurityGroupUnresolvedPlaceholder", ErrorCategory.InvalidData, config.DisplayName));
                    continue;
                }

                var actionResult = SyncSecurityGroup(displayName, mailNickName, description);
                if (actionResult == null)
                {
                    continue;
                }

                SyncRoleAssignments(actionResult, config, scopesById, placeholders);
                items.Add(actionResult);
            }

            WriteVerbose($"Done. Processed {items.Count} of {securityGroupConfigs.Count} security group(s).");
            if (AsHashtable.IsPresent)
            {
                WriteObject(new Hashtable
                {
                    ["IsWhatIf"] = MyInvocation.BoundParameters.ContainsKey("WhatIf"),
                    ["Count"] = items.Count,
                    ["SecurityGroups"] = items
                });
            }
            else
            {
                foreach (var item in items)
                {
                    WriteObject(item);
                }
            }
        }

        // Builds a map of resourceGroupId -> ARM resource ID, verifying every referenced resource group already exists (phase 1 must have run)
        private Dictionary<string, string> ResolveResourceGroupScopes(List<PlatformResourceGroupConfig> resourceGroupConfigs, Dictionary<string, string> placeholders)
        {
            var resourceGroupsById = PlatformResourceGroupResolver.Resolve(this, resourceGroupConfigs, placeholders, "PlatformSecurityGroupResourceGroupMissing");

            var scopesById = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in resourceGroupsById)
            {
                scopesById[entry.Key] = entry.Value.ResourceId;
            }

            return scopesById;
        }

        // Creates the group if missing, or updates its display name / description if either has drifted; returns null (after recording an error) if creation didn't succeed
        private PlatformSecurityGroupActionResult SyncSecurityGroup(string displayName, string mailNickName, string description)
        {
            WriteVerbose($"Security group '{mailNickName}': checking whether it already exists.");
            var existing = AzureAdGroupHelper.Get(this, mailNickName);

            if (existing == null)
            {
                WriteVerbose($"Security group '{mailNickName}' does not exist yet.");
                if (!ShouldProcess(mailNickName, "Create security group"))
                {
                    return new PlatformSecurityGroupActionResult
                    {
                        DisplayName = displayName,
                        MailNickName = mailNickName,
                        Action = "WouldCreate"
                    };
                }

                WriteVerbose($"Creating security group '{mailNickName}'.");
                AzureAdGroupInfo created;
                try
                {
                    created = AzureAdGroupHelper.Create(this, displayName, mailNickName, description);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(
                        new InvalidOperationException($"Failed to create security group '{mailNickName}': {ex.Message}", ex),
                        "PlatformSecurityGroupCreateFailed", ErrorCategory.WriteError, mailNickName));
                    return null;
                }

                if (created == null)
                {
                    WriteError(new ErrorRecord(
                        new InvalidOperationException($"Security group '{mailNickName}' was created but is not yet readable. It may still be propagating; re-run this command to verify."),
                        "PlatformSecurityGroupNotReadable", ErrorCategory.ReadError, mailNickName));
                    return null;
                }

                WriteVerbose($"Security group '{mailNickName}' created and confirmed readable (object id '{created.Id}').");
                return new PlatformSecurityGroupActionResult
                {
                    DisplayName = created.DisplayName,
                    MailNickName = created.MailNickname,
                    ObjectId = created.Id,
                    Action = "Created"
                };
            }

            WriteVerbose($"Security group '{mailNickName}' already exists (object id '{existing.Id}').");

            var isUnchanged = string.Equals(existing.DisplayName, displayName, StringComparison.Ordinal) &&
                               string.Equals(existing.Description ?? string.Empty, description ?? string.Empty, StringComparison.Ordinal);

            if (isUnchanged)
            {
                WriteVerbose($"Security group '{mailNickName}' display name and description already match; nothing to do.");
                return new PlatformSecurityGroupActionResult
                {
                    DisplayName = existing.DisplayName,
                    MailNickName = existing.MailNickname,
                    ObjectId = existing.Id,
                    Action = "Unchanged"
                };
            }

            WriteVerbose($"Security group '{mailNickName}' display name or description has drifted from the configured values.");
            if (!ShouldProcess(mailNickName, "Update security group"))
            {
                return new PlatformSecurityGroupActionResult
                {
                    DisplayName = displayName,
                    MailNickName = existing.MailNickname,
                    ObjectId = existing.Id,
                    Action = "WouldUpdate"
                };
            }

            WriteVerbose($"Updating security group '{mailNickName}'.");
            AzureAdGroupHelper.Update(this, existing.Id, displayName, description);

            return new PlatformSecurityGroupActionResult
            {
                DisplayName = displayName,
                MailNickName = existing.MailNickname,
                ObjectId = existing.Id,
                Action = "Updated"
            };
        }

        // Assigns every configured role that the group doesn't already hold; existing role assignments outside this list are left untouched
        private void SyncRoleAssignments(
            PlatformSecurityGroupActionResult actionResult,
            PlatformSecurityGroupConfig config,
            Dictionary<string, string> scopesById,
            Dictionary<string, string> placeholders)
        {
            foreach (var roleAssignment in config.RoleAssignments)
            {
                if (!scopesById.TryGetValue(roleAssignment.ResourceGroupId, out var scope))
                {
                    // Missing scope was already reported by ResolveResourceGroupScopes
                    continue;
                }

                string role;
                try
                {
                    role = PlatformConfigLoader.ResolvePlaceholders(roleAssignment.Role, placeholders);
                }
                catch (InvalidOperationException ex)
                {
                    WriteError(new ErrorRecord(ex, "PlatformSecurityGroupUnresolvedPlaceholder", ErrorCategory.InvalidData, config.DisplayName));
                    continue;
                }

                if (actionResult.Action == "WouldCreate")
                {
                    actionResult.RoleAssignments.Add(new PlatformRoleAssignmentActionResult { Role = role, Scope = scope, Action = "WouldAdd" });
                    continue;
                }

                if (AzureRoleAssignmentHelper.Exists(this, actionResult.ObjectId, scope, role))
                {
                    WriteVerbose($"Security group '{actionResult.MailNickName}' already has role '{role}' at scope '{scope}'.");
                    actionResult.RoleAssignments.Add(new PlatformRoleAssignmentActionResult { Role = role, Scope = scope, Action = "Unchanged" });
                    continue;
                }

                if (!ShouldProcess(actionResult.MailNickName, $"Assign role '{role}' at scope '{scope}'"))
                {
                    actionResult.RoleAssignments.Add(new PlatformRoleAssignmentActionResult { Role = role, Scope = scope, Action = "WouldAdd" });
                    continue;
                }

                WriteVerbose($"Assigning role '{role}' to security group '{actionResult.MailNickName}' at scope '{scope}'.");
                try
                {
                    AzureRoleAssignmentHelper.Create(this, actionResult.ObjectId, scope, role);
                    actionResult.RoleAssignments.Add(new PlatformRoleAssignmentActionResult { Role = role, Scope = scope, Action = "Added" });
                }
                catch (InvalidOperationException ex)
                {
                    WriteError(new ErrorRecord(ex, "PlatformSecurityGroupRoleAssignmentFailed", ErrorCategory.WriteError, config.DisplayName));
                }
            }
        }
    }
}
