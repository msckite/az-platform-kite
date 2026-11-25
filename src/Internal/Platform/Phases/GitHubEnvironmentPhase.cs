using System;
using System.Collections.Generic;
using System.Management.Automation;
using MSCKite.Azure.Platform.Internal.Azure;
using MSCKite.Azure.Platform.Internal.GitHub;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Internal.Platform.Phases
{
    // Phase 4: creates or updates each environment's GitHub deployment environment declared in the platform config files (protection rules, secrets, and variables)
    internal static class GitHubEnvironmentPhase
    {
        // Stands in for the client id of an identity that -WhatIf did not create, so the preview can still show every secret and variable
        internal const string UnknownClientId = "(known after apply)";

        internal static string IdentityKey(string resourceGroupName, string identityName)
        {
            return $"{resourceGroupName}/{identityName}";
        }

        // clientIdsByIdentity holds the identities from phase 3, keyed by IdentityKey; when null, each identity is read from Azure instead
        internal static List<PlatformGitHubEnvironmentActionResult> Run(
            PSCmdlet cmdlet,
            GlobalConfig globalConfig,
            List<PlatformEnvironmentConfig> environmentConfigs,
            Dictionary<string, AzureResourceGroupInfo> resourceGroupsById,
            Dictionary<string, string> clientIdsByIdentity = null)
        {
            var owner = globalConfig.SourceControl.Owner;
            var repository = globalConfig.SourceControl.Repository;
            var globalPlaceholders = globalConfig.ToPlaceholderMap();
            var items = new List<PlatformGitHubEnvironmentActionResult>();

            foreach (var config in environmentConfigs)
            {
                var basePlaceholders = new Dictionary<string, string>(globalPlaceholders, StringComparer.Ordinal)
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
                    // Each identity resolves its own "${clientId}", so copy the placeholders per pipeline instead of sharing one map
                    var placeholders = new Dictionary<string, string>(basePlaceholders, StringComparer.Ordinal);

                    if (!TryResolveClientId(cmdlet, config, pipeline.UserAssignedIdentity, resourceGroup, placeholders, clientIdsByIdentity))
                    {
                        continue;
                    }

                    var actionResult = SyncEnvironment(cmdlet, config, pipeline.Purpose, pipeline.GitHubEnvironment, owner, repository, placeholders);
                    if (actionResult == null)
                    {
                        continue;
                    }

                    SyncDeploymentBranchPolicies(cmdlet, actionResult, pipeline.GitHubEnvironment, owner, repository);
                    SyncSecrets(cmdlet, actionResult, pipeline.GitHubEnvironment, owner, repository, placeholders);
                    SyncVariables(cmdlet, actionResult, pipeline.GitHubEnvironment, owner, repository, placeholders);
                    items.Add(actionResult);
                }
            }

            cmdlet.WriteVerbose($"Done. Processed {items.Count} GitHub environment(s) across {environmentConfigs.Count} configured environment(s).");
            return items;
        }

        // Adds "${clientId}" to the placeholder map by reading the identity created in phase 3; reports a clear error if that phase hasn't run yet
        private static bool TryResolveClientId(
            PSCmdlet cmdlet,
            PlatformEnvironmentConfig config,
            PlatformUserAssignedIdentityConfig identityConfig,
            AzureResourceGroupInfo resourceGroup,
            Dictionary<string, string> placeholders,
            Dictionary<string, string> clientIdsByIdentity)
        {
            string identityName;
            try
            {
                identityName = PlatformConfigLoader.ResolvePlaceholders(identityConfig.Name, placeholders);
            }
            catch (InvalidOperationException ex)
            {
                cmdlet.WritePhaseError(new ErrorRecord(ex, "PlatformGitHubEnvironmentUnresolvedPlaceholder", ErrorCategory.InvalidData, config.EnvironmentCode));
                return false;
            }

            if (clientIdsByIdentity != null)
            {
                cmdlet.WriteVerbose($"Environment '{config.EnvironmentCode}': resolving '${{clientId}}' from phase 3 identity '{identityName}'.");
                if (!clientIdsByIdentity.TryGetValue(IdentityKey(resourceGroup.Name, identityName), out var clientId) || string.IsNullOrEmpty(clientId))
                {
                    cmdlet.WritePhaseError(new ErrorRecord(
                        new InvalidOperationException($"Identity '{identityName}' was not created or updated in phase 3, so its GitHub environment is skipped."),
                        "PlatformGitHubEnvironmentIdentityMissing", ErrorCategory.ObjectNotFound, config.EnvironmentCode));
                    return false;
                }

                placeholders["clientId"] = clientId;
                return true;
            }

            cmdlet.WriteVerbose($"Environment '{config.EnvironmentCode}': resolving '${{clientId}}' from identity '{identityName}'.");
            var identity = AzureUserAssignedIdentityHelper.Get(cmdlet, resourceGroup.Name, identityName);
            if (identity == null)
            {
                cmdlet.WritePhaseError(new ErrorRecord(
                    new InvalidOperationException($"Identity '{identityName}' does not exist (or its principal isn't readable yet). Run Set-PlatformEnvironmentIdentity first."),
                    "PlatformGitHubEnvironmentIdentityMissing", ErrorCategory.ObjectNotFound, config.EnvironmentCode));
                return false;
            }

            placeholders["clientId"] = identity.ClientId;
            return true;
        }

        // Always reconciles the environment's protection rules (idempotent PUT); the action label is based only on whether the environment previously existed
        private static PlatformGitHubEnvironmentActionResult SyncEnvironment(
            PSCmdlet cmdlet, PlatformEnvironmentConfig config, string purpose, PlatformGitHubEnvironmentConfig githubConfig, string owner, string repository, Dictionary<string, string> placeholders)
        {
            string name;
            try
            {
                name = PlatformConfigLoader.ResolvePlaceholders(githubConfig.Name, placeholders);
            }
            catch (InvalidOperationException ex)
            {
                cmdlet.WritePhaseError(new ErrorRecord(ex, "PlatformGitHubEnvironmentUnresolvedPlaceholder", ErrorCategory.InvalidData, config.EnvironmentCode));
                return null;
            }

            cmdlet.WriteVerbose($"GitHub environment '{name}': checking whether it already exists.");
            var existed = GitHubEnvironmentHelper.Exists(owner, repository, name, out var existsError);
            if (!existed && existsError != null && !existsError.Contains("HTTP 404"))
            {
                cmdlet.WriteWarning($"Could not confirm whether GitHub environment '{name}' already exists ({existsError.Trim()}); treating it as missing. If it actually exists, PLATFORM_GITHUB_TOKEN likely lacks the 'Environments' read permission.");
            }

            var reviewers = new List<GitHubReviewer>();
            foreach (var reviewer in githubConfig.RequiredReviewers)
            {
                if (!GitHubReviewerResolver.TryResolve(owner, reviewer, out var type, out var id, out var resolveError))
                {
                    cmdlet.WritePhaseError(new ErrorRecord(
                        new InvalidOperationException(resolveError), "PlatformGitHubEnvironmentReviewerUnresolved", ErrorCategory.ObjectNotFound, reviewer));
                    continue;
                }

                reviewers.Add(new GitHubReviewer { Type = type, Id = id });
            }

            if (!cmdlet.ShouldProcess(name, existed ? "Update GitHub environment" : "Create GitHub environment"))
            {
                return new PlatformGitHubEnvironmentActionResult
                {
                    EnvironmentCode = config.EnvironmentCode,
                    Purpose = purpose,
                    Name = name,
                    Action = existed ? "WouldUpdate" : "WouldCreate"
                };
            }

            var branchPolicy = githubConfig.DeploymentBranchPolicy;
            cmdlet.WriteVerbose($"Applying protection rules to GitHub environment '{name}' (wait timer {githubConfig.WaitTimerMinutes}m, {reviewers.Count} reviewer(s), " +
                (branchPolicy == null ? "branch policy unmanaged)." : $"{branchPolicy.Branches.Count} branch and {branchPolicy.Tags.Count} tag pattern(s))."));
            if (!GitHubEnvironmentHelper.CreateOrUpdate(owner, repository, name, githubConfig.WaitTimerMinutes, reviewers, branchPolicy != null, out var applyError))
            {
                cmdlet.WritePhaseError(new ErrorRecord(
                    new InvalidOperationException($"Failed to configure GitHub environment '{name}': {applyError}"),
                    "PlatformGitHubEnvironmentConfigureFailed", ErrorCategory.WriteError, name));
                return null;
            }

            return new PlatformGitHubEnvironmentActionResult
            {
                EnvironmentCode = config.EnvironmentCode,
                Purpose = purpose,
                Name = name,
                Action = existed ? "Updated" : "Created"
            };
        }

        // Reconciles the environment's custom branch/tag policies to exactly the configured patterns: missing ones are created, unlisted ones deleted
        private static void SyncDeploymentBranchPolicies(
            PSCmdlet cmdlet, PlatformGitHubEnvironmentActionResult actionResult, PlatformGitHubEnvironmentConfig githubConfig, string owner, string repository)
        {
            var policyConfig = githubConfig.DeploymentBranchPolicy;
            if (policyConfig == null)
            {
                return;
            }

            // A not-yet-created environment (WhatIf) has no policies to list, and listing it would only produce a misleading 404 warning
            var existing = new List<GitHubDeploymentBranchPolicy>();
            if (actionResult.Action != "WouldCreate")
            {
                existing = GitHubDeploymentBranchPolicyHelper.List(owner, repository, actionResult.Name, out var listError);

                // GitHub answers 404 while custom branch policies aren't enabled yet on the environment (only reachable under WhatIf, since the PUT above enables them)
                if (existing == null && listError != null && listError.Contains("HTTP 404"))
                {
                    existing = new List<GitHubDeploymentBranchPolicy>();
                }

                if (existing == null)
                {
                    cmdlet.WritePhaseError(new ErrorRecord(
                        new InvalidOperationException($"Could not list deployment branch policies for '{actionResult.Name}': {listError}"),
                        "PlatformGitHubEnvironmentBranchPolicyListFailed", ErrorCategory.ReadError, actionResult.Name));
                    return;
                }
            }

            var desired = new List<Tuple<string, string>>();
            policyConfig.Branches.ForEach(pattern => desired.Add(Tuple.Create("branch", pattern)));
            policyConfig.Tags.ForEach(pattern => desired.Add(Tuple.Create("tag", pattern)));

            foreach (var policy in desired)
            {
                var label = $"{policy.Item1}:{policy.Item2}";
                if (existing.Exists(e => e.Type == policy.Item1 && e.Name == policy.Item2))
                {
                    actionResult.DeploymentBranchPolicies.Add(new PlatformKeyValueActionResult { Name = label, Action = "Unchanged" });
                    continue;
                }

                if (!cmdlet.ShouldProcess($"{actionResult.Name}/{label}", "Allow deployments from"))
                {
                    actionResult.DeploymentBranchPolicies.Add(new PlatformKeyValueActionResult { Name = label, Action = "WouldCreate" });
                    continue;
                }

                cmdlet.WriteVerbose($"Allowing deployments from {policy.Item1} pattern '{policy.Item2}' on GitHub environment '{actionResult.Name}'.");
                if (!GitHubDeploymentBranchPolicyHelper.Create(owner, repository, actionResult.Name, policy.Item2, policy.Item1, out var createError))
                {
                    cmdlet.WritePhaseError(new ErrorRecord(
                        new InvalidOperationException($"Failed to add deployment {policy.Item1} policy '{policy.Item2}': {createError}"),
                        "PlatformGitHubEnvironmentBranchPolicyFailed", ErrorCategory.WriteError, label));
                    continue;
                }

                actionResult.DeploymentBranchPolicies.Add(new PlatformKeyValueActionResult { Name = label, Action = "Created" });
            }

            foreach (var policy in existing)
            {
                var label = $"{policy.Type}:{policy.Name}";
                if (desired.Exists(d => d.Item1 == policy.Type && d.Item2 == policy.Name))
                {
                    continue;
                }

                if (!cmdlet.ShouldProcess($"{actionResult.Name}/{label}", "Stop allowing deployments from"))
                {
                    actionResult.DeploymentBranchPolicies.Add(new PlatformKeyValueActionResult { Name = label, Action = "WouldDelete" });
                    continue;
                }

                cmdlet.WriteVerbose($"Removing {policy.Type} pattern '{policy.Name}' from GitHub environment '{actionResult.Name}', since the config no longer lists it.");
                if (!GitHubDeploymentBranchPolicyHelper.Delete(owner, repository, actionResult.Name, policy.Id, out var deleteError))
                {
                    cmdlet.WritePhaseError(new ErrorRecord(
                        new InvalidOperationException($"Failed to remove deployment {policy.Type} policy '{policy.Name}': {deleteError}"),
                        "PlatformGitHubEnvironmentBranchPolicyFailed", ErrorCategory.WriteError, label));
                    continue;
                }

                actionResult.DeploymentBranchPolicies.Add(new PlatformKeyValueActionResult { Name = label, Action = "Deleted" });
            }
        }

        // Sets every configured secret; GitHub never exposes secret values, so each run always (re-)sets them rather than diffing
        private static void SyncSecrets(
            PSCmdlet cmdlet, PlatformGitHubEnvironmentActionResult actionResult, PlatformGitHubEnvironmentConfig githubConfig, string owner, string repository, Dictionary<string, string> placeholders)
        {
            var existingNames = GitHubSecretHelper.ListNames(owner, repository, actionResult.Name, out var listError) ?? new HashSet<string>();
            if (listError != null)
            {
                cmdlet.WriteWarning($"Could not list existing secrets for '{actionResult.Name}': {listError}");
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
                    cmdlet.WritePhaseError(new ErrorRecord(ex, "PlatformGitHubEnvironmentUnresolvedPlaceholder", ErrorCategory.InvalidData, actionResult.EnvironmentCode));
                    continue;
                }

                if (!cmdlet.ShouldProcess($"{actionResult.Name}/{name}", "Set secret"))
                {
                    actionResult.Secrets.Add(new PlatformKeyValueActionResult { Name = name, Action = existingNames.Contains(name) ? "WouldUpdate" : "WouldCreate" });
                    continue;
                }

                cmdlet.WriteVerbose($"Setting secret '{name}' on GitHub environment '{actionResult.Name}'.");
                if (!GitHubSecretHelper.Set(owner, repository, actionResult.Name, name, value, out var setError))
                {
                    cmdlet.WritePhaseError(new ErrorRecord(
                        new InvalidOperationException($"Failed to set secret '{name}': {setError}"),
                        "PlatformGitHubEnvironmentSecretFailed", ErrorCategory.WriteError, name));
                    continue;
                }

                actionResult.Secrets.Add(new PlatformKeyValueActionResult { Name = name, Action = existingNames.Contains(name) ? "Updated" : "Created" });
            }
        }

        // Sets every configured variable whose value has drifted; existing values are readable, so unchanged ones are skipped
        private static void SyncVariables(
            PSCmdlet cmdlet, PlatformGitHubEnvironmentActionResult actionResult, PlatformGitHubEnvironmentConfig githubConfig, string owner, string repository, Dictionary<string, string> placeholders)
        {
            var existingVariables = GitHubVariableHelper.List(owner, repository, actionResult.Name, out var listError) ?? new Dictionary<string, string>(StringComparer.Ordinal);
            if (listError != null)
            {
                cmdlet.WriteWarning($"Could not list existing variables for '{actionResult.Name}': {listError}");
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
                    cmdlet.WritePhaseError(new ErrorRecord(ex, "PlatformGitHubEnvironmentUnresolvedPlaceholder", ErrorCategory.InvalidData, actionResult.EnvironmentCode));
                    continue;
                }

                var existed = existingVariables.TryGetValue(name, out var existingValue);
                if (existed && string.Equals(existingValue, value, StringComparison.Ordinal))
                {
                    actionResult.Variables.Add(new PlatformKeyValueActionResult { Name = name, Action = "Unchanged" });
                    continue;
                }

                if (!cmdlet.ShouldProcess($"{actionResult.Name}/{name}", "Set variable"))
                {
                    actionResult.Variables.Add(new PlatformKeyValueActionResult { Name = name, Action = existed ? "WouldUpdate" : "WouldCreate" });
                    continue;
                }

                cmdlet.WriteVerbose($"Setting variable '{name}' on GitHub environment '{actionResult.Name}'.");
                if (!GitHubVariableHelper.Set(owner, repository, actionResult.Name, name, value, out var setError))
                {
                    cmdlet.WritePhaseError(new ErrorRecord(
                        new InvalidOperationException($"Failed to set variable '{name}': {setError}"),
                        "PlatformGitHubEnvironmentVariableFailed", ErrorCategory.WriteError, name));
                    continue;
                }

                actionResult.Variables.Add(new PlatformKeyValueActionResult { Name = name, Action = existed ? "Updated" : "Created" });
            }
        }
    }
}
