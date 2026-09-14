using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text.Json;
using MSCKite.Azure.Platform.Internal.GitHub;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Commands.GitHub
{
    [Cmdlet(VerbsCommon.Set, "GitHubLabels", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
    [OutputType(typeof(GitHubLabelSyncResult))]
    public class SetGitHubLabels : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [Alias("Path")]
        public string LabelFilePath { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [AllowNull]
        public string Owner { get; set; }

        [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
        [AllowNull]
        public string Repository { get; set; }

        // Preserves repository labels that aren't present in the input file instead of removing them
        [Parameter]
        public SwitchParameter KeepExistingLabels { get; set; }

        // Reconciles a repository's labels with a jsonc definitions file: creates missing labels, updates changed ones, and (unless -KeepExistingLabels) removes the rest. Identification is by label name.
        protected override void ProcessRecord()
        {
            var defaults = GitHubConfigStore.Load();
            var owner = string.IsNullOrEmpty(Owner) ? defaults.Owner : Owner;
            var repository = string.IsNullOrEmpty(Repository) ? defaults.Repository : Repository;

            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repository))
            {
                ThrowTerminatingError(new ErrorRecord(
                    new InvalidOperationException("Owner and Repository are required. Pass them explicitly or set defaults via Set-GitHubDefault."),
                    "GitHubLabelsMissingRepository",
                    ErrorCategory.InvalidArgument,
                    null));
                return;
            }

            var resolvedPath = GetUnresolvedProviderPathFromPSPath(LabelFilePath);

            List<GitHubLabel> desired;
            try
            {
                desired = GitHubLabelHelper.LoadFromFile(resolvedPath);
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidOperationException || ex is JsonException)
            {
                ThrowTerminatingError(new ErrorRecord(ex, "GitHubLabelsInvalidFile", ErrorCategory.InvalidData, LabelFilePath));
                return;
            }

            var existing = GitHubLabelHelper.GetRemoteLabels(owner, repository, out var listError);
            if (existing == null)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new InvalidOperationException($"Failed to list labels for {owner}/{repository}: {listError}"),
                    "GitHubLabelsListFailed",
                    ErrorCategory.ReadError,
                    null));
                return;
            }

            var existingByName = existing.ToDictionary(label => label.Name, label => label, StringComparer.Ordinal);
            var desiredNames = new HashSet<string>(desired.Select(label => label.Name), StringComparer.Ordinal);
            var result = new GitHubLabelSyncResult { Owner = owner, Repository = repository };
            var target = $"{owner}/{repository}";

            foreach (var label in desired)
            {
                if (!existingByName.TryGetValue(label.Name, out var current))
                {
                    if (!ShouldProcess(target, $"Create label '{label.Name}'"))
                    {
                        continue;
                    }

                    if (GitHubLabelHelper.CreateLabel(owner, repository, label, out var createError))
                    {
                        result.Added.Add(label.Name);
                    }
                    else
                    {
                        WriteError(new ErrorRecord(
                            new InvalidOperationException($"Failed to create label '{label.Name}': {createError}"),
                            "GitHubLabelCreateFailed", ErrorCategory.WriteError, label.Name));
                    }

                    continue;
                }

                var isUnchanged = string.Equals(current.Color, label.Color, StringComparison.OrdinalIgnoreCase) &&
                                   string.Equals(current.Description ?? string.Empty, label.Description ?? string.Empty, StringComparison.Ordinal);

                if (isUnchanged)
                {
                    result.Unchanged.Add(label.Name);
                    continue;
                }

                if (!ShouldProcess(target, $"Update label '{label.Name}'"))
                {
                    continue;
                }

                if (GitHubLabelHelper.UpdateLabel(owner, repository, label, out var updateError))
                {
                    result.Updated.Add(label.Name);
                }
                else
                {
                    WriteError(new ErrorRecord(
                        new InvalidOperationException($"Failed to update label '{label.Name}': {updateError}"),
                        "GitHubLabelUpdateFailed", ErrorCategory.WriteError, label.Name));
                }
            }

            if (!KeepExistingLabels)
            {
                foreach (var current in existing.Where(label => !desiredNames.Contains(label.Name)))
                {
                    if (!ShouldProcess(target, $"Remove label '{current.Name}'"))
                    {
                        continue;
                    }

                    if (GitHubLabelHelper.DeleteLabel(owner, repository, current.Name, out var deleteError))
                    {
                        result.Removed.Add(current.Name);
                    }
                    else
                    {
                        WriteError(new ErrorRecord(
                            new InvalidOperationException($"Failed to remove label '{current.Name}': {deleteError}"),
                            "GitHubLabelDeleteFailed", ErrorCategory.WriteError, current.Name));
                    }
                }
            }

            WriteObject(result);
        }
    }
}
