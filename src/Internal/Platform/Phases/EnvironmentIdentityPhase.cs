using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using MSCKite.Azure.Platform.Internal.Azure;
using MSCKite.Azure.Platform.Internal.GitHub;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Internal.Platform.Phases
{
    // Phase 3: creates the federated user-assigned managed identity declared per environment in environments.jsonc, and assigns its RBAC roles against the resource groups from phase 1
    internal static class EnvironmentIdentityPhase
    {
        internal static List<PlatformEnvironmentIdentityActionResult> Run(
            PSCmdlet cmdlet,
            GlobalConfig globalConfig,
            List<PlatformEnvironmentConfig> environmentConfigs,
            Dictionary<string, AzureResourceGroupInfo> resourceGroupsById,
            GitHubRepositoryInfo githubRepository)
        {
            var globalPlaceholders = globalConfig.ToPlaceholderMap();
            var items = new List<PlatformEnvironmentIdentityActionResult>();

            foreach (var config in environmentConfigs)
            {
                // Merge global placeholders with this environment's own values (${environmentCode}, ${tool})
                var placeholders = new Dictionary<string, string>(globalPlaceholders, StringComparer.Ordinal)
                {
                    ["environmentCode"] = config.EnvironmentCode
                };

                if (!resourceGroupsById.TryGetValue(config.ResourceGroupId, out var resourceGroup))
                {
                    // Missing resource group was already reported by PlatformResourceGroupResolver
                    continue;
                }

                foreach (var pipeline in config.Pipelines)
                {
                    var actionResult = SyncIdentityAndCredential(cmdlet, config, pipeline.Purpose, pipeline.UserAssignedIdentity, pipeline.GitHubEnvironment, resourceGroup, placeholders, githubRepository);
                    if (actionResult == null)
                    {
                        continue;
                    }

                    SyncRoleAssignments(cmdlet, actionResult, pipeline.UserAssignedIdentity, resourceGroupsById, globalConfig.SubscriptionId, placeholders);
                    items.Add(actionResult);
                }
            }

            cmdlet.WriteVerbose($"Done. Processed {items.Count} identity/identities across {environmentConfigs.Count} environment(s).");
            return items;
        }

        // Creates the identity and its federated credential if missing, updating the credential in place if it has drifted; returns null (after recording an error) on failure
        private static PlatformEnvironmentIdentityActionResult SyncIdentityAndCredential(
            PSCmdlet cmdlet,
            PlatformEnvironmentConfig config,
            string purpose,
            PlatformUserAssignedIdentityConfig identityConfig,
            PlatformGitHubEnvironmentConfig githubEnvironmentConfig,
            AzureResourceGroupInfo resourceGroup,
            Dictionary<string, string> placeholders,
            GitHubRepositoryInfo githubRepository)
        {
            string identityName;
            try
            {
                identityName = PlatformConfigLoader.ResolvePlaceholders(identityConfig.Name, placeholders);
            }
            catch (InvalidOperationException ex)
            {
                cmdlet.WriteError(new ErrorRecord(ex, "PlatformEnvironmentIdentityUnresolvedPlaceholder", ErrorCategory.InvalidData, config.EnvironmentCode));
                return null;
            }

            cmdlet.WriteVerbose($"Environment '{config.EnvironmentCode}' ({purpose}): checking whether identity '{identityName}' already exists.");
            var existing = AzureUserAssignedIdentityHelper.Get(cmdlet, resourceGroup.Name, identityName);
            string identityAction;
            AzureUserAssignedIdentityInfo identity;

            if (existing == null)
            {
                cmdlet.WriteVerbose($"Identity '{identityName}' does not exist yet (or its principal isn't readable yet).");
                if (!cmdlet.ShouldProcess(identityName, "Create user-assigned managed identity"))
                {
                    return new PlatformEnvironmentIdentityActionResult
                    {
                        EnvironmentCode = config.EnvironmentCode,
                        Purpose = purpose,
                        IdentityName = identityName,
                        Action = "WouldCreate",
                        FederatedCredentialAction = "WouldCreate"
                    };
                }

                cmdlet.WriteVerbose($"Creating identity '{identityName}' in resource group '{resourceGroup.Name}'.");
                try
                {
                    identity = AzureUserAssignedIdentityHelper.Create(cmdlet, resourceGroup.Name, identityName, resourceGroup.Location);
                }
                catch (Exception ex)
                {
                    cmdlet.WriteError(new ErrorRecord(
                        new InvalidOperationException($"Failed to create identity '{identityName}': {ex.Message}", ex),
                        "PlatformEnvironmentIdentityCreateFailed", ErrorCategory.WriteError, identityName));
                    return null;
                }

                if (identity == null)
                {
                    cmdlet.WriteError(new ErrorRecord(
                        new InvalidOperationException($"Identity '{identityName}' was created but its principal is not yet readable. It may still be propagating; re-run this command to verify."),
                        "PlatformEnvironmentIdentityNotReadable", ErrorCategory.ReadError, identityName));
                    return null;
                }

                cmdlet.WriteVerbose($"Identity '{identityName}' created and confirmed readable (principal id '{identity.PrincipalId}').");
                identityAction = "Created";
            }
            else
            {
                cmdlet.WriteVerbose($"Identity '{identityName}' already exists (principal id '{existing.PrincipalId}').");
                identity = existing;
                identityAction = "Unchanged";
            }

            var actionResult = new PlatformEnvironmentIdentityActionResult
            {
                EnvironmentCode = config.EnvironmentCode,
                Purpose = purpose,
                IdentityName = identity.Name,
                PrincipalId = identity.PrincipalId,
                ClientId = identity.ClientId,
                Action = identityAction
            };

            actionResult.FederatedCredentialAction = SyncFederatedCredential(cmdlet, config, identityConfig, githubEnvironmentConfig, identityName, resourceGroup, placeholders, githubRepository);
            return actionResult;
        }

        // Creates the federated credential if missing, or updates it if the issuer/subject/audiences have drifted; returns the action taken, or null if it couldn't be resolved/applied
        private static string SyncFederatedCredential(
            PSCmdlet cmdlet,
            PlatformEnvironmentConfig config,
            PlatformUserAssignedIdentityConfig identityConfig,
            PlatformGitHubEnvironmentConfig githubEnvironmentConfig,
            string identityName,
            AzureResourceGroupInfo resourceGroup,
            Dictionary<string, string> placeholders,
            GitHubRepositoryInfo githubRepository)
        {
            var federatedConfig = identityConfig.FederatedCredential;

            string credentialName;
            string issuer;
            string githubEnvironmentName;
            string[] audiences;
            try
            {
                credentialName = PlatformConfigLoader.ResolvePlaceholders(federatedConfig.Name, placeholders);
                issuer = PlatformConfigLoader.ResolvePlaceholders(federatedConfig.Issuer, placeholders);
                githubEnvironmentName = PlatformConfigLoader.ResolvePlaceholders(githubEnvironmentConfig.Name, placeholders);
                audiences = federatedConfig.Audiences.Select(a => PlatformConfigLoader.ResolvePlaceholders(a, placeholders)).ToArray();
            }
            catch (InvalidOperationException ex)
            {
                cmdlet.WriteError(new ErrorRecord(ex, "PlatformEnvironmentIdentityUnresolvedPlaceholder", ErrorCategory.InvalidData, config.EnvironmentCode));
                return null;
            }

            string subject;
            try
            {
                subject = GitHubSubjectHelper.ResolveSubject(federatedConfig.SubjectType, githubRepository, githubEnvironmentName);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is NotSupportedException)
            {
                cmdlet.WriteError(new ErrorRecord(ex, "PlatformEnvironmentIdentitySubjectUnresolved", ErrorCategory.InvalidData, config.EnvironmentCode));
                return null;
            }

            cmdlet.WriteVerbose($"Federated credential '{credentialName}': checking whether it already exists.");
            var existing = AzureFederatedCredentialHelper.Get(cmdlet, resourceGroup.Name, identityName, credentialName);

            if (existing == null)
            {
                cmdlet.WriteVerbose($"Federated credential '{credentialName}' does not exist yet.");
                if (!cmdlet.ShouldProcess(credentialName, "Create federated credential"))
                {
                    return "WouldCreate";
                }

                cmdlet.WriteVerbose($"Creating federated credential '{credentialName}' with subject '{subject}'.");
                try
                {
                    AzureFederatedCredentialHelper.Create(cmdlet, resourceGroup.Name, identityName, credentialName, issuer, subject, audiences);
                }
                catch (Exception ex)
                {
                    cmdlet.WriteError(new ErrorRecord(
                        new InvalidOperationException($"Failed to create federated credential '{credentialName}': {ex.Message}", ex),
                        "PlatformEnvironmentIdentityCredentialCreateFailed", ErrorCategory.WriteError, credentialName));
                    return null;
                }

                return "Created";
            }

            var isUnchanged = string.Equals(existing.Issuer, issuer, StringComparison.Ordinal) &&
                               string.Equals(existing.Subject, subject, StringComparison.Ordinal) &&
                               existing.Audiences.Count == audiences.Length &&
                               !existing.Audiences.Except(audiences, StringComparer.Ordinal).Any();

            if (isUnchanged)
            {
                cmdlet.WriteVerbose($"Federated credential '{credentialName}' already matches the configured values; nothing to do.");
                return "Unchanged";
            }

            cmdlet.WriteVerbose($"Federated credential '{credentialName}' has drifted from the configured values.");
            if (!cmdlet.ShouldProcess(credentialName, "Update federated credential"))
            {
                return "WouldUpdate";
            }

            cmdlet.WriteVerbose($"Updating federated credential '{credentialName}'.");
            try
            {
                AzureFederatedCredentialHelper.Update(cmdlet, resourceGroup.Name, identityName, credentialName, issuer, subject, audiences);
            }
            catch (Exception ex)
            {
                cmdlet.WriteError(new ErrorRecord(
                    new InvalidOperationException($"Failed to update federated credential '{credentialName}': {ex.Message}", ex),
                    "PlatformEnvironmentIdentityCredentialUpdateFailed", ErrorCategory.WriteError, credentialName));
                return null;
            }

            return "Updated";
        }

        // Assigns every configured role that the identity doesn't already hold; existing role assignments outside this list are left untouched
        private static void SyncRoleAssignments(
            PSCmdlet cmdlet,
            PlatformEnvironmentIdentityActionResult actionResult,
            PlatformUserAssignedIdentityConfig identityConfig,
            Dictionary<string, AzureResourceGroupInfo> resourceGroupsById,
            string subscriptionId,
            Dictionary<string, string> placeholders)
        {
            foreach (var roleAssignment in identityConfig.RoleAssignments)
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
                    cmdlet.WriteError(new ErrorRecord(ex, "PlatformEnvironmentIdentityUnresolvedPlaceholder", ErrorCategory.InvalidData, actionResult.EnvironmentCode));
                    continue;
                }

                if (actionResult.Action == "WouldCreate")
                {
                    actionResult.RoleAssignments.Add(new PlatformRoleAssignmentActionResult { Role = role, Scope = scope, Action = "WouldAdd" });
                    continue;
                }

                if (AzureRoleAssignmentHelper.Exists(cmdlet, actionResult.PrincipalId, scope, role))
                {
                    cmdlet.WriteVerbose($"Identity '{actionResult.IdentityName}' already has role '{role}' at scope '{scope}'.");
                    actionResult.RoleAssignments.Add(new PlatformRoleAssignmentActionResult { Role = role, Scope = scope, Action = "Unchanged" });
                    continue;
                }

                if (!cmdlet.ShouldProcess(actionResult.IdentityName, $"Assign role '{role}' at scope '{scope}'"))
                {
                    actionResult.RoleAssignments.Add(new PlatformRoleAssignmentActionResult { Role = role, Scope = scope, Action = "WouldAdd" });
                    continue;
                }

                cmdlet.WriteVerbose($"Assigning role '{role}' to identity '{actionResult.IdentityName}' at scope '{scope}'.");
                try
                {
                    AzureRoleAssignmentHelper.Create(cmdlet, actionResult.PrincipalId, scope, role);
                    actionResult.RoleAssignments.Add(new PlatformRoleAssignmentActionResult { Role = role, Scope = scope, Action = "Added" });
                }
                catch (InvalidOperationException ex)
                {
                    cmdlet.WriteError(new ErrorRecord(ex, "PlatformEnvironmentIdentityRoleAssignmentFailed", ErrorCategory.WriteError, actionResult.EnvironmentCode));
                }
            }
        }
    }
}
