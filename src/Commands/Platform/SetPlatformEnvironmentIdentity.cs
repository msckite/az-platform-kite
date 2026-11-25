using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Text.Json;
using MSCKite.Azure.Platform.Internal.Azure;
using MSCKite.Azure.Platform.Internal.Common;
using MSCKite.Azure.Platform.Internal.GitHub;
using MSCKite.Azure.Platform.Internal.Platform;
using MSCKite.Azure.Platform.Internal.Platform.Phases;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Commands.Platform
{
    // Phase 3: creates the federated user-assigned managed identity declared per environment in environments.jsonc, and assigns its RBAC roles against the resource groups from phase 1
    [Cmdlet(VerbsCommon.Set, "PlatformEnvironmentIdentity", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
    [OutputType(typeof(PlatformEnvironmentIdentityActionResult))]
    [OutputType(typeof(Hashtable))]
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
        public string ResourceGroupsConfigPath { get; set; } = Path.Combine("config", "platform", "resource-groups.jsonc");

        [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string EnvironmentsConfigPath { get; set; } = Path.Combine("config", "platform", "environments.jsonc");

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
            var resourceGroupsConfigPath = GetUnresolvedProviderPathFromPSPath(ResourceGroupsConfigPath);
            var environmentsConfigPath = GetUnresolvedProviderPathFromPSPath(EnvironmentsConfigPath);

            WriteVerbose($"Loading global config from '{globalConfigPath}'.");
            WriteVerbose($"Loading resource groups config from '{resourceGroupsConfigPath}'.");
            WriteVerbose($"Loading environments config from '{environmentsConfigPath}'.");

            GlobalConfig globalConfig;
            List<PlatformResourceGroupConfig> resourceGroupConfigs;
            List<PlatformEnvironmentConfig> environmentConfigs;
            try
            {
                globalConfig = PlatformConfigLoader.LoadGlobalConfig(globalConfigPath);
                resourceGroupConfigs = PlatformConfigLoader.LoadResourceGroups(resourceGroupsConfigPath);
                environmentConfigs = PlatformConfigLoader.LoadEnvironments(environmentsConfigPath);
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidOperationException || ex is JsonException)
            {
                ThrowTerminatingError(new ErrorRecord(ex, "PlatformEnvironmentIdentityInvalidConfig", ErrorCategory.InvalidData, EnvironmentsConfigPath));
                return;
            }

            WriteVerbose($"Found {environmentConfigs.Count} environment(s) in the config.");

            if (!AzureContextHelper.TryGetConfiguredContext(this, globalConfig, out var azureContext, out var azureMessage))
            {
                ThrowTerminatingError(new ErrorRecord(
                    new InvalidOperationException(azureMessage),
                    azureContext.IsSignedIn ? "PlatformEnvironmentIdentityContextMismatch" : "PlatformEnvironmentIdentityNotSignedIn",
                    azureContext.IsSignedIn ? ErrorCategory.SecurityError : ErrorCategory.AuthenticationError,
                    null));
                return;
            }

            var owner = globalConfig.SourceControl?.Owner;
            var repository = globalConfig.SourceControl?.Repository;
            WriteVerbose($"Fetching live repository '{owner}/{repository}' from GitHub to compute the immutable OIDC subject.");
            var githubRepository = GitHubRepositoryHelper.GetRepository(owner, repository, out var repositoryError);
            if (githubRepository == null)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new InvalidOperationException($"Failed to read repository '{owner}/{repository}' from GitHub: {repositoryError}"),
                    "PlatformEnvironmentIdentityRepositoryNotFound", ErrorCategory.ObjectNotFound, EnvironmentsConfigPath));
                return;
            }

            WriteVerbose($"Resolved repository to owner id '{githubRepository.OwnerId}' and repository id '{githubRepository.Id}'.");

            // Resolve every resourceGroupId referenced anywhere in this config up front, so a missing phase-1 resource group fails fast with a clear message
            var resourceGroupsById = PlatformResourceGroupResolver.Resolve(
                this, resourceGroupConfigs, globalConfig.ToPlaceholderMap(), "PlatformEnvironmentIdentityResourceGroupMissing");

            var items = EnvironmentIdentityPhase.Run(this, globalConfig, environmentConfigs, resourceGroupsById, githubRepository);

            if (AsHashtable.IsPresent)
            {
                WriteObject(new Hashtable
                {
                    ["IsWhatIf"] = MyInvocation.BoundParameters.ContainsKey("WhatIf"),
                    ["Count"] = items.Count,
                    ["Environments"] = items
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
    }
}
