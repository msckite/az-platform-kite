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
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Commands.Platform
{
    // Phase 4: creates or updates each environment's GitHub deployment environment declared in platform-config.jsonc (protection rules, secrets, and variables)
    [Cmdlet(VerbsCommon.Set, "PlatformGitHubEnvironment", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
    [OutputType(typeof(PlatformGitHubEnvironmentActionResult))]
    [OutputType(typeof(Hashtable))]
    public class SetPlatformGitHubEnvironment : PSCmdlet
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
            List<PlatformEnvironmentConfig> environmentConfigs;
            try
            {
                globalConfig = PlatformConfigLoader.LoadGlobalConfig(globalConfigPath);
                resourceGroupConfigs = PlatformConfigLoader.LoadResourceGroups(platformConfigPath);
                environmentConfigs = PlatformConfigLoader.LoadEnvironments(platformConfigPath);
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidOperationException || ex is JsonException)
            {
                ThrowTerminatingError(new ErrorRecord(ex, "PlatformGitHubEnvironmentInvalidConfig", ErrorCategory.InvalidData, PlatformConfigPath));
                return;
            }

            WriteVerbose($"Found {environmentConfigs.Count} environment(s) in platform config.");

            var owner = globalConfig.SourceControl?.Owner;
            var repository = globalConfig.SourceControl?.Repository;
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repository))
            {
                ThrowTerminatingError(new ErrorRecord(
                    new InvalidOperationException("global-config.jsonc must have a \"sourceControl.owner\" and \"sourceControl.repository\"."),
                    "PlatformGitHubEnvironmentMissingRepository", ErrorCategory.InvalidData, globalConfigPath));
                return;
            }

            var azureContext = AzureContextHelper.GetContext(this, out var azureMessage);
            if (!azureContext.IsSignedIn)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new InvalidOperationException(azureMessage),
                    "PlatformGitHubEnvironmentNotSignedIn", ErrorCategory.AuthenticationError, null));
                return;
            }

            var globalPlaceholders = globalConfig.ToPlaceholderMap();

            // Resolve every resourceGroupId referenced anywhere in this config up front, so a missing phase-1 resource group fails fast with a clear message
            var resourceGroupsById = PlatformResourceGroupResolver.Resolve(
                this, resourceGroupConfigs, globalPlaceholders, "PlatformGitHubEnvironmentResourceGroupMissing");

            var items = new List<PlatformGitHubEnvironmentActionResult>();

            foreach (var config in environmentConfigs)
            {
                var placeholders = new Dictionary<string, string>(globalPlaceholders, StringComparer.Ordinal)
                {
                    ["environmentCode"] = config.EnvironmentCode
                };

                if (!resourceGroupsById.TryGetValue(config.ResourceGroupId, out var resourceGroup))
                {
                    // Missing resource group was already reported by PlatformResourceGroupResolver
                    continue;
                }

                if (!TryResolveClientId(config, resourceGroup, placeholders))
                {
                    continue;
                }

                var actionResult = SyncEnvironment(config, owner, repository, placeholders);
                if (actionResult == null)
                {
                    continue;
                }

                SyncSecrets(actionResult, config.GitHubEnvironment, owner, repository, placeholders);
                SyncVariables(actionResult, config.GitHubEnvironment, owner, repository, placeholders);
                items.Add(actionResult);
            }

            WriteVerbose($"Done. Processed {items.Count} of {environmentConfigs.Count} environment(s).");
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

        // Adds "${clientId}" to the placeholder map by reading the identity created in phase 3; reports a clear error if that phase hasn't run yet
        private bool TryResolveClientId(PlatformEnvironmentConfig config, AzureResourceGroupInfo resourceGroup, Dictionary<string, string> placeholders)
        {
            string identityName;
            try
            {
                identityName = PlatformConfigLoader.ResolvePlaceholders(config.UserAssignedIdentity.Name, placeholders);
            }
            catch (InvalidOperationException ex)
            {
                WriteError(new ErrorRecord(ex, "PlatformGitHubEnvironmentUnresolvedPlaceholder", ErrorCategory.InvalidData, config.EnvironmentCode));
                return false;
            }

            WriteVerbose($"Environment '{config.EnvironmentCode}': resolving '${{clientId}}' from identity '{identityName}'.");
            var identity = AzureUserAssignedIdentityHelper.Get(this, resourceGroup.Name, identityName);
            if (identity == null)
            {
                WriteError(new ErrorRecord(
                    new InvalidOperationException($"Identity '{identityName}' does not exist (or its principal isn't readable yet). Run Set-PlatformEnvironmentIdentity first."),
                    "PlatformGitHubEnvironmentIdentityMissing", ErrorCategory.ObjectNotFound, config.EnvironmentCode));
                return false;
            }

            placeholders["clientId"] = identity.ClientId;
            return true;
        }

        // Always reconciles the environment's protection rules (idempotent PUT); the action label is based only on whether the environment previously existed
        private PlatformGitHubEnvironmentActionResult SyncEnvironment(
            PlatformEnvironmentConfig config, string owner, string repository, Dictionary<string, string> placeholders)
        {
            var githubConfig = config.GitHubEnvironment;

            string name;
            try
            {
                name = PlatformConfigLoader.ResolvePlaceholders(githubConfig.Name, placeholders);
            }
            catch (InvalidOperationException ex)
            {
                WriteError(new ErrorRecord(ex, "PlatformGitHubEnvironmentUnresolvedPlaceholder", ErrorCategory.InvalidData, config.EnvironmentCode));
                return null;
            }

            WriteVerbose($"GitHub environment '{name}': checking whether it already exists.");
            var existed = GitHubEnvironmentHelper.Exists(owner, repository, name, out var existsError);
            if (!existed && existsError != null && !existsError.Contains("HTTP 404"))
            {
                WriteWarning($"Could not confirm whether GitHub environment '{name}' already exists ({existsError.Trim()}); treating it as missing. If it actually exists, PLATFORM_GITHUB_TOKEN likely lacks the 'Environments' read permission.");
            }

            var reviewers = new List<GitHubReviewer>();
            foreach (var reviewer in githubConfig.RequiredReviewers)
            {
                if (!GitHubReviewerResolver.TryResolve(owner, reviewer, out var type, out var id, out var resolveError))
                {
                    WriteError(new ErrorRecord(
                        new InvalidOperationException(resolveError), "PlatformGitHubEnvironmentReviewerUnresolved", ErrorCategory.ObjectNotFound, reviewer));
                    continue;
                }

                reviewers.Add(new GitHubReviewer { Type = type, Id = id });
            }

            if (!ShouldProcess(name, existed ? "Update GitHub environment" : "Create GitHub environment"))
            {
                return new PlatformGitHubEnvironmentActionResult
                {
                    EnvironmentCode = config.EnvironmentCode,
                    Name = name,
                    Action = existed ? "WouldUpdate" : "WouldCreate"
                };
            }

            WriteVerbose($"Applying protection rules to GitHub environment '{name}' (wait timer {githubConfig.WaitTimerMinutes}m, {reviewers.Count} reviewer(s)).");
            if (!GitHubEnvironmentHelper.CreateOrUpdate(owner, repository, name, githubConfig.WaitTimerMinutes, reviewers, out var applyError))
            {
                WriteError(new ErrorRecord(
                    new InvalidOperationException($"Failed to configure GitHub environment '{name}': {applyError}"),
                    "PlatformGitHubEnvironmentConfigureFailed", ErrorCategory.WriteError, name));
                return null;
            }

            return new PlatformGitHubEnvironmentActionResult
            {
                EnvironmentCode = config.EnvironmentCode,
                Name = name,
                Action = existed ? "Updated" : "Created"
            };
        }

        // Sets every configured secret; GitHub never exposes secret values, so each run always (re-)sets them rather than diffing
        private void SyncSecrets(
            PlatformGitHubEnvironmentActionResult actionResult, PlatformGitHubEnvironmentConfig githubConfig, string owner, string repository, Dictionary<string, string> placeholders)
        {
            var existingNames = GitHubSecretHelper.ListNames(owner, repository, actionResult.Name, out var listError) ?? new HashSet<string>();
            if (listError != null)
            {
                WriteWarning($"Could not list existing secrets for '{actionResult.Name}': {listError}");
            }

            foreach (var secretConfig in githubConfig.Secrets)
            {
                string name;
                string value;
                try
                {
                    name = PlatformConfigLoader.ResolvePlaceholders(secretConfig.Name, placeholders);
                    value = PlatformConfigLoader.ResolvePlaceholders(secretConfig.Value, placeholders);
                }
                catch (InvalidOperationException ex)
                {
                    WriteError(new ErrorRecord(ex, "PlatformGitHubEnvironmentUnresolvedPlaceholder", ErrorCategory.InvalidData, actionResult.EnvironmentCode));
                    continue;
                }

                if (!ShouldProcess($"{actionResult.Name}/{name}", "Set secret"))
                {
                    actionResult.Secrets.Add(new PlatformKeyValueActionResult { Name = name, Action = existingNames.Contains(name) ? "WouldUpdate" : "WouldCreate" });
                    continue;
                }

                WriteVerbose($"Setting secret '{name}' on GitHub environment '{actionResult.Name}'.");
                if (!GitHubSecretHelper.Set(owner, repository, actionResult.Name, name, value, out var setError))
                {
                    WriteError(new ErrorRecord(
                        new InvalidOperationException($"Failed to set secret '{name}': {setError}"),
                        "PlatformGitHubEnvironmentSecretFailed", ErrorCategory.WriteError, name));
                    continue;
                }

                actionResult.Secrets.Add(new PlatformKeyValueActionResult { Name = name, Action = existingNames.Contains(name) ? "Updated" : "Created" });
            }
        }

        // Sets every configured variable whose value has drifted; existing values are readable, so unchanged ones are skipped
        private void SyncVariables(
            PlatformGitHubEnvironmentActionResult actionResult, PlatformGitHubEnvironmentConfig githubConfig, string owner, string repository, Dictionary<string, string> placeholders)
        {
            var existingVariables = GitHubVariableHelper.List(owner, repository, actionResult.Name, out var listError) ?? new Dictionary<string, string>(StringComparer.Ordinal);
            if (listError != null)
            {
                WriteWarning($"Could not list existing variables for '{actionResult.Name}': {listError}");
            }

            foreach (var variableConfig in githubConfig.Variables)
            {
                string name;
                string value;
                try
                {
                    name = PlatformConfigLoader.ResolvePlaceholders(variableConfig.Name, placeholders);
                    value = PlatformConfigLoader.ResolvePlaceholders(variableConfig.Value, placeholders);
                }
                catch (InvalidOperationException ex)
                {
                    WriteError(new ErrorRecord(ex, "PlatformGitHubEnvironmentUnresolvedPlaceholder", ErrorCategory.InvalidData, actionResult.EnvironmentCode));
                    continue;
                }

                var existed = existingVariables.TryGetValue(name, out var existingValue);
                if (existed && string.Equals(existingValue, value, StringComparison.Ordinal))
                {
                    actionResult.Variables.Add(new PlatformKeyValueActionResult { Name = name, Action = "Unchanged" });
                    continue;
                }

                if (!ShouldProcess($"{actionResult.Name}/{name}", "Set variable"))
                {
                    actionResult.Variables.Add(new PlatformKeyValueActionResult { Name = name, Action = existed ? "WouldUpdate" : "WouldCreate" });
                    continue;
                }

                WriteVerbose($"Setting variable '{name}' on GitHub environment '{actionResult.Name}'.");
                if (!GitHubVariableHelper.Set(owner, repository, actionResult.Name, name, value, out var setError))
                {
                    WriteError(new ErrorRecord(
                        new InvalidOperationException($"Failed to set variable '{name}': {setError}"),
                        "PlatformGitHubEnvironmentVariableFailed", ErrorCategory.WriteError, name));
                    continue;
                }

                actionResult.Variables.Add(new PlatformKeyValueActionResult { Name = name, Action = existed ? "Updated" : "Created" });
            }
        }
    }
}
