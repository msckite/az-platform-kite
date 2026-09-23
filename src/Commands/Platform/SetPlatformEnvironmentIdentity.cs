using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text.Json;
using MSCKite.Azure.Platform.Internal.Azure;
using MSCKite.Azure.Platform.Internal.Common;
using MSCKite.Azure.Platform.Internal.GitHub;
using MSCKite.Azure.Platform.Internal.Platform;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Commands.Platform
{
    // Phase 3: creates the federated user-assigned managed identity declared per environment in platform-config.jsonc, and assigns its RBAC roles against the resource groups from phase 1
    [Cmdlet(VerbsCommon.Set, "PlatformEnvironmentIdentity", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
    [OutputType(typeof(PlatformEnvironmentIdentitySyncResult))]
    public class SetPlatformEnvironmentIdentity : PSCmdlet
    {
        private static readonly PowerShellModule[] RequiredModules = {
            new PowerShellModule("Az.Resources", "6.0"),
            new PowerShellModule("Az.ManagedServiceIdentity", "2.0")
        };

        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string GlobalConfigPath { get; set; } = Path.Combine("config", "global-config.jsonc");

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string PlatformConfigPath { get; set; } = Path.Combine("config", "platform-config.jsonc");

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
            List<PlatformEnvironmentConfig> environmentConfigs;
            try
            {
                globalConfig = PlatformConfigLoader.LoadGlobalConfig(globalConfigPath);
                resourceGroupConfigs = PlatformConfigLoader.LoadResourceGroups(platformConfigPath);
                environmentConfigs = PlatformConfigLoader.LoadEnvironments(platformConfigPath);
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidOperationException || ex is JsonException)
            {
                ThrowTerminatingError(new ErrorRecord(ex, "PlatformEnvironmentIdentityInvalidConfig", ErrorCategory.InvalidData, PlatformConfigPath));
                return;
            }

            WriteVerbose($"Found {environmentConfigs.Count} environment(s) in platform config.");

            var globalPlaceholders = globalConfig.ToPlaceholderMap();

            var owner = globalConfig.SourceControl?.Owner;
            var repository = globalConfig.SourceControl?.Repository;
            WriteVerbose($"Fetching live repository '{owner}/{repository}' from GitHub to compute the immutable OIDC subject.");
            var githubRepository = GitHubRepositoryHelper.GetRepository(owner, repository, out var repositoryError);
            if (githubRepository == null)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new InvalidOperationException($"Failed to read repository '{owner}/{repository}' from GitHub: {repositoryError}"),
                    "PlatformEnvironmentIdentityRepositoryNotFound", ErrorCategory.ObjectNotFound, PlatformConfigPath));
                return;
            }

            WriteVerbose($"Resolved repository to owner id '{githubRepository.OwnerId}' and repository id '{githubRepository.Id}'.");

            // Resolve every resourceGroupId referenced anywhere in this config up front, so a missing phase-1 resource group fails fast with a clear message
            var resourceGroupsById = PlatformResourceGroupResolver.Resolve(
                this, resourceGroupConfigs, globalPlaceholders, "PlatformEnvironmentIdentityResourceGroupMissing");

            var result = new PlatformEnvironmentIdentitySyncResult
            {
                IsWhatIf = MyInvocation.BoundParameters.ContainsKey("WhatIf")
            };

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

                var actionResult = SyncIdentityAndCredential(config, resourceGroup, placeholders, githubRepository);
                if (actionResult == null)
                {
                    continue;
                }

                SyncRoleAssignments(actionResult, config.UserAssignedIdentity, resourceGroupsById, placeholders);
                result.Environments.Add(actionResult);
            }

            WriteVerbose($"Done. Processed {result.Environments.Count} of {environmentConfigs.Count} environment(s).");
            WriteObject(result);
        }

        // Creates the identity and its federated credential if missing, updating the credential in place if it has drifted; returns null (after recording an error) on failure
        private PlatformEnvironmentIdentityActionResult SyncIdentityAndCredential(
            PlatformEnvironmentConfig config,
            AzureResourceGroupInfo resourceGroup,
            Dictionary<string, string> placeholders,
            GitHubRepositoryInfo githubRepository)
        {
            string identityName;
            try
            {
                identityName = PlatformConfigLoader.ResolvePlaceholders(config.UserAssignedIdentity.Name, placeholders);
            }
            catch (InvalidOperationException ex)
            {
                WriteError(new ErrorRecord(ex, "PlatformEnvironmentIdentityUnresolvedPlaceholder", ErrorCategory.InvalidData, config.EnvironmentCode));
                return null;
            }

            WriteVerbose($"Environment '{config.EnvironmentCode}': checking whether identity '{identityName}' already exists.");
            var existing = AzureUserAssignedIdentityHelper.Get(this, resourceGroup.Name, identityName);
            string identityAction;
            AzureUserAssignedIdentityInfo identity;

            if (existing == null)
            {
                WriteVerbose($"Identity '{identityName}' does not exist yet (or its principal isn't readable yet).");
                if (!ShouldProcess(identityName, "Create user-assigned managed identity"))
                {
                    return new PlatformEnvironmentIdentityActionResult
                    {
                        EnvironmentCode = config.EnvironmentCode,
                        IdentityName = identityName,
                        Action = "PlannedCreate",
                        FederatedCredentialAction = "PlannedCreate"
                    };
                }

                WriteVerbose($"Creating identity '{identityName}' in resource group '{resourceGroup.Name}'.");
                try
                {
                    identity = AzureUserAssignedIdentityHelper.Create(this, resourceGroup.Name, identityName, resourceGroup.Location);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(
                        new InvalidOperationException($"Failed to create identity '{identityName}': {ex.Message}", ex),
                        "PlatformEnvironmentIdentityCreateFailed", ErrorCategory.WriteError, identityName));
                    return null;
                }

                if (identity == null)
                {
                    WriteError(new ErrorRecord(
                        new InvalidOperationException($"Identity '{identityName}' was created but its principal is not yet readable. It may still be propagating; re-run this command to verify."),
                        "PlatformEnvironmentIdentityNotReadable", ErrorCategory.ReadError, identityName));
                    return null;
                }

                WriteVerbose($"Identity '{identityName}' created and confirmed readable (principal id '{identity.PrincipalId}').");
                identityAction = "Created";
            }
            else
            {
                WriteVerbose($"Identity '{identityName}' already exists (principal id '{existing.PrincipalId}').");
                identity = existing;
                identityAction = "Unchanged";
            }

            var actionResult = new PlatformEnvironmentIdentityActionResult
            {
                EnvironmentCode = config.EnvironmentCode,
                IdentityName = identity.Name,
                PrincipalId = identity.PrincipalId,
                ClientId = identity.ClientId,
                Action = identityAction
            };

            actionResult.FederatedCredentialAction = SyncFederatedCredential(config, identityName, resourceGroup, placeholders, githubRepository);
            return actionResult;
        }

        // Creates the federated credential if missing, or updates it if the issuer/subject/audiences have drifted; returns the action taken, or null if it couldn't be resolved/applied
        private string SyncFederatedCredential(
            PlatformEnvironmentConfig config,
            string identityName,
            AzureResourceGroupInfo resourceGroup,
            Dictionary<string, string> placeholders,
            GitHubRepositoryInfo githubRepository)
        {
            var federatedConfig = config.UserAssignedIdentity.FederatedCredential;

            string credentialName;
            string issuer;
            string githubEnvironmentName;
            string[] audiences;
            try
            {
                credentialName = PlatformConfigLoader.ResolvePlaceholders(federatedConfig.Name, placeholders);
                issuer = PlatformConfigLoader.ResolvePlaceholders(federatedConfig.Issuer, placeholders);
                githubEnvironmentName = PlatformConfigLoader.ResolvePlaceholders(config.GitHubEnvironment.Name, placeholders);
                audiences = federatedConfig.Audiences.Select(a => PlatformConfigLoader.ResolvePlaceholders(a, placeholders)).ToArray();
            }
            catch (InvalidOperationException ex)
            {
                WriteError(new ErrorRecord(ex, "PlatformEnvironmentIdentityUnresolvedPlaceholder", ErrorCategory.InvalidData, config.EnvironmentCode));
                return null;
            }

            string subject;
            try
            {
                subject = GitHubSubjectHelper.ResolveSubject(federatedConfig.SubjectType, githubRepository, githubEnvironmentName);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is NotSupportedException)
            {
                WriteError(new ErrorRecord(ex, "PlatformEnvironmentIdentitySubjectUnresolved", ErrorCategory.InvalidData, config.EnvironmentCode));
                return null;
            }

            WriteVerbose($"Federated credential '{credentialName}': checking whether it already exists.");
            var existing = AzureFederatedCredentialHelper.Get(this, resourceGroup.Name, identityName, credentialName);

            if (existing == null)
            {
                WriteVerbose($"Federated credential '{credentialName}' does not exist yet.");
                if (!ShouldProcess(credentialName, "Create federated credential"))
                {
                    return "PlannedCreate";
                }

                WriteVerbose($"Creating federated credential '{credentialName}' with subject '{subject}'.");
                try
                {
                    AzureFederatedCredentialHelper.Create(this, resourceGroup.Name, identityName, credentialName, issuer, subject, audiences);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(
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
                WriteVerbose($"Federated credential '{credentialName}' already matches the configured values; nothing to do.");
                return "Unchanged";
            }

            WriteVerbose($"Federated credential '{credentialName}' has drifted from the configured values.");
            if (!ShouldProcess(credentialName, "Update federated credential"))
            {
                return "PlannedUpdate";
            }

            WriteVerbose($"Updating federated credential '{credentialName}'.");
            try
            {
                AzureFederatedCredentialHelper.Update(this, resourceGroup.Name, identityName, credentialName, issuer, subject, audiences);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(
                    new InvalidOperationException($"Failed to update federated credential '{credentialName}': {ex.Message}", ex),
                    "PlatformEnvironmentIdentityCredentialUpdateFailed", ErrorCategory.WriteError, credentialName));
                return null;
            }

            return "Updated";
        }

        // Assigns every configured role that the identity doesn't already hold; existing role assignments outside this list are left untouched
        private void SyncRoleAssignments(
            PlatformEnvironmentIdentityActionResult actionResult,
            PlatformUserAssignedIdentityConfig identityConfig,
            Dictionary<string, AzureResourceGroupInfo> resourceGroupsById,
            Dictionary<string, string> placeholders)
        {
            foreach (var roleAssignment in identityConfig.RoleAssignments)
            {
                if (!resourceGroupsById.TryGetValue(roleAssignment.ResourceGroupId, out var resourceGroup))
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
                    WriteError(new ErrorRecord(ex, "PlatformEnvironmentIdentityUnresolvedPlaceholder", ErrorCategory.InvalidData, actionResult.EnvironmentCode));
                    continue;
                }

                var scope = resourceGroup.ResourceId;

                if (actionResult.Action == "PlannedCreate")
                {
                    actionResult.RoleAssignments.Add(new PlatformRoleAssignmentActionResult { Role = role, Scope = scope, Action = "PlannedAdd" });
                    continue;
                }

                if (AzureRoleAssignmentHelper.Exists(this, actionResult.PrincipalId, scope, role))
                {
                    WriteVerbose($"Identity '{actionResult.IdentityName}' already has role '{role}' at scope '{scope}'.");
                    actionResult.RoleAssignments.Add(new PlatformRoleAssignmentActionResult { Role = role, Scope = scope, Action = "Unchanged" });
                    continue;
                }

                if (!ShouldProcess(actionResult.IdentityName, $"Assign role '{role}' at scope '{scope}'"))
                {
                    actionResult.RoleAssignments.Add(new PlatformRoleAssignmentActionResult { Role = role, Scope = scope, Action = "PlannedAdd" });
                    continue;
                }

                WriteVerbose($"Assigning role '{role}' to identity '{actionResult.IdentityName}' at scope '{scope}'.");
                try
                {
                    AzureRoleAssignmentHelper.Create(this, actionResult.PrincipalId, scope, role);
                    actionResult.RoleAssignments.Add(new PlatformRoleAssignmentActionResult { Role = role, Scope = scope, Action = "Added" });
                }
                catch (InvalidOperationException ex)
                {
                    WriteError(new ErrorRecord(ex, "PlatformEnvironmentIdentityRoleAssignmentFailed", ErrorCategory.WriteError, actionResult.EnvironmentCode));
                }
            }
        }
    }
}
