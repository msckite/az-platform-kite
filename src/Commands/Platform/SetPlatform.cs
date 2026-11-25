using System;
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
    // Runs the four platform phases in order and hands each phase's results to the next in memory, instead of reading them back from Azure
    [Cmdlet(VerbsCommon.Set, "Platform", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
    [OutputType(typeof(PlatformDeploymentResult))]
    public class SetPlatform : PSCmdlet
    {
        private const string ProgressActivity = "Set-Platform";

        private static readonly PowerShellModule[] RequiredModules = {
            new PowerShellModule("Az.Resources", "6.0"),
            new PowerShellModule("Az.ManagedServiceIdentity", "2.0")
        };

        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string GlobalConfigPath { get; set; } = Path.Combine("config", "global-config.jsonc");

        // Folder holding resource-groups.jsonc, security-groups.jsonc and environments.jsonc
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string PlatformConfigFolder { get; set; } = Path.Combine("config", "platform");

        protected override void BeginProcessing()
        {
            PowerShellModuleLoader.Import(this, (PowerShellModule[])(object)RequiredModules);
        }

        protected override void ProcessRecord()
        {
            var globalConfigPath = GetUnresolvedProviderPathFromPSPath(GlobalConfigPath);
            var platformConfigFolder = GetUnresolvedProviderPathFromPSPath(PlatformConfigFolder);
            var resourceGroupsConfigPath = Path.Combine(platformConfigFolder, "resource-groups.jsonc");
            var securityGroupsConfigPath = Path.Combine(platformConfigFolder, "security-groups.jsonc");
            var environmentsConfigPath = Path.Combine(platformConfigFolder, "environments.jsonc");

            GlobalConfig globalConfig;
            List<PlatformResourceGroupConfig> resourceGroupConfigs;
            List<PlatformSecurityGroupConfig> securityGroupConfigs;
            List<PlatformEnvironmentConfig> environmentConfigs;
            var currentPath = globalConfigPath;
            try
            {
                WriteVerbose($"Loading global config from '{globalConfigPath}'.");
                globalConfig = PlatformConfigLoader.LoadGlobalConfig(globalConfigPath);

                currentPath = resourceGroupsConfigPath;
                WriteVerbose($"Loading resource groups config from '{resourceGroupsConfigPath}'.");
                resourceGroupConfigs = PlatformConfigLoader.LoadResourceGroups(resourceGroupsConfigPath);

                currentPath = securityGroupsConfigPath;
                WriteVerbose($"Loading security groups config from '{securityGroupsConfigPath}'.");
                securityGroupConfigs = PlatformConfigLoader.LoadSecurityGroups(securityGroupsConfigPath);

                currentPath = environmentsConfigPath;
                WriteVerbose($"Loading environments config from '{environmentsConfigPath}'.");
                environmentConfigs = PlatformConfigLoader.LoadEnvironments(environmentsConfigPath);
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidOperationException || ex is JsonException)
            {
                ThrowTerminatingError(new ErrorRecord(ex, "PlatformInvalidConfig", ErrorCategory.InvalidData, currentPath));
                return;
            }

            var owner = globalConfig.SourceControl?.Owner;
            var repository = globalConfig.SourceControl?.Repository;
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repository))
            {
                ThrowTerminatingError(new ErrorRecord(
                    new InvalidOperationException("global-config.jsonc must have a \"sourceControl.owner\" and \"sourceControl.repository\"."),
                    "PlatformMissingRepository", ErrorCategory.InvalidData, globalConfigPath));
                return;
            }

            if (!AzureContextHelper.TryGetConfiguredContext(this, globalConfig, out var azureContext, out var azureMessage))
            {
                ThrowTerminatingError(new ErrorRecord(
                    new InvalidOperationException(azureMessage),
                    azureContext.IsSignedIn ? "PlatformContextMismatch" : "PlatformNotSignedIn",
                    azureContext.IsSignedIn ? ErrorCategory.SecurityError : ErrorCategory.AuthenticationError,
                    null));
                return;
            }

            // Checked before any phase runs, so a missing GitHub sign-in can't leave the platform half deployed
            WriteVerbose($"Fetching live repository '{owner}/{repository}' from GitHub to compute the immutable OIDC subject.");
            var githubRepository = GitHubRepositoryHelper.GetRepository(owner, repository, out var repositoryError);
            if (githubRepository == null)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new InvalidOperationException($"Failed to read repository '{owner}/{repository}' from GitHub: {repositoryError}"),
                    "PlatformRepositoryNotFound", ErrorCategory.ObjectNotFound, globalConfigPath));
                return;
            }

            WriteVerbose($"Resolved repository to owner id '{githubRepository.OwnerId}' and repository id '{githubRepository.Id}'.");

            var result = new PlatformDeploymentResult
            {
                IsWhatIf = MyInvocation.BoundParameters.ContainsKey("WhatIf")
            };

            StartPhase(1, "Resource groups");
            var errorCount = PhaseErrors.Count(this);
            result.ResourceGroups = ResourceGroupPhase.Run(this, globalConfig, resourceGroupConfigs);
            if (HasNewErrors(ref errorCount))
            {
                Stop(result, 1, "ResourceGroups", "resource groups");
                return;
            }

            var resourceGroupsById = new Dictionary<string, AzureResourceGroupInfo>(StringComparer.Ordinal);
            foreach (var resourceGroup in result.ResourceGroups)
            {
                resourceGroupsById[resourceGroup.Id] = new AzureResourceGroupInfo
                {
                    Name = resourceGroup.Name,
                    Location = resourceGroup.Location,
                    ResourceId = resourceGroup.ResourceId
                };
            }

            StartPhase(2, "Security groups");
            result.SecurityGroups = SecurityGroupPhase.Run(this, globalConfig, securityGroupConfigs, resourceGroupsById);
            if (HasNewErrors(ref errorCount))
            {
                // Phases 3 and 4 don't depend on the security groups, so their errors don't stop the run
                result.PhasesWithErrors.Add("SecurityGroups");
            }

            StartPhase(3, "Environment identities");
            result.EnvironmentIdentities = EnvironmentIdentityPhase.Run(this, globalConfig, environmentConfigs, resourceGroupsById, githubRepository);
            if (HasNewErrors(ref errorCount))
            {
                Stop(result, 3, "EnvironmentIdentities", "environment identities");
                return;
            }

            var clientIdsByIdentity = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var identity in result.EnvironmentIdentities)
            {
                var clientId = identity.ClientId;
                if (string.IsNullOrEmpty(clientId) && result.IsWhatIf)
                {
                    clientId = GitHubEnvironmentPhase.UnknownClientId;
                }

                if (!string.IsNullOrEmpty(clientId))
                {
                    clientIdsByIdentity[GitHubEnvironmentPhase.IdentityKey(identity.ResourceGroupName, identity.IdentityName)] = clientId;
                }
            }

            StartPhase(4, "GitHub environments");
            result.GitHubEnvironments = GitHubEnvironmentPhase.Run(this, globalConfig, environmentConfigs, resourceGroupsById, clientIdsByIdentity);
            if (HasNewErrors(ref errorCount))
            {
                result.PhasesWithErrors.Add("GitHubEnvironments");
            }

            CompleteProgress();
            WriteObject(result);
        }

        private void StartPhase(int phase, string label)
        {
            WriteVerbose($"Phase {phase} of 4: {label}.");
            WriteProgress(new ProgressRecord(1, ProgressActivity, $"Phase {phase} of 4: {label}")
            {
                PercentComplete = (phase - 1) * 25
            });
        }

        private bool HasNewErrors(ref int errorCount)
        {
            var current = PhaseErrors.Count(this);
            var hasNewErrors = current > errorCount;
            errorCount = current;
            return hasNewErrors;
        }

        private void Stop(PlatformDeploymentResult result, int phase, string phaseName, string label)
        {
            result.PhasesWithErrors.Add(phaseName);
            result.StoppedAtPhase = phaseName;
            WriteWarning($"Phase {phase} ({label}) reported errors, so the phases that depend on it did not run. Fix the errors above and run Set-Platform again.");
            CompleteProgress();
            WriteObject(result);
        }

        private void CompleteProgress()
        {
            WriteProgress(new ProgressRecord(1, ProgressActivity, "Done")
            {
                RecordType = ProgressRecordType.Completed
            });
        }
    }
}
