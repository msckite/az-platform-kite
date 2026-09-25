using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using MSCKite.Azure.Platform.Internal.Common;
using MSCKite.Azure.Platform.Internal.Platform;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Commands.Bootstrap
{
    [Cmdlet(VerbsCommon.New, "PlatformWorkflow", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    [OutputType(typeof(PlatformWorkflowResult))]
    public class NewPlatformWorkflow : PSCmdlet
    {
        private const string ManifestRelativePath = "github/workflows/manifest.jsonc";

        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string InputFolder { get; set; } = ".tmp/templates";

        // Repository root that holds the .github folder the workflows are copied into
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string OutputFolder { get; set; } = ".";

        // Used only for workload workflows. Platform workflows have a fixed flow.
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string BranchStrategy { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateSet("platform", "workload", "infra", "both", "all")]
        public string WorkflowType { get; set; } = "both";

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string GlobalConfigPath { get; set; } = "config/global-config.jsonc";

        // Overwrites workflow files that already exist at the destination instead of skipping them
        [Parameter]
        public SwitchParameter Force { get; set; }

        // Copies the shared workflow templates plus the ones for the selected branch strategy into the repository, following manifest.jsonc
        protected override void ProcessRecord()
        {
            var inputRootPath = GetUnresolvedProviderPathFromPSPath(InputFolder);
            var outputRootPath = GetUnresolvedProviderPathFromPSPath(OutputFolder);
            var manifestPath = Path.Combine(inputRootPath, ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar));

            WorkflowManifest manifest;
            try
            {
                manifest = WorkflowManifestLoader.Load(manifestPath);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is InvalidOperationException || ex is System.Text.Json.JsonException)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new InvalidOperationException($"Failed to read the workflow manifest: {ex.Message}", ex),
                    "PlatformWorkflowManifestInvalid",
                    ErrorCategory.InvalidData,
                    manifestPath));
                return;
            }

            var workflowTypes = WorkflowType.ToLowerInvariant() == "both"
                ? new[] { "platform", "workload" }
                : WorkflowType.ToLowerInvariant() == "all"
                    ? new[] { "platform", "workload", "infra" }
                    : new[] { WorkflowType.ToLowerInvariant() };
            var branchStrategy = (string)null;
            var selectedSets = new List<Tuple<string, WorkflowManifestSet, WorkflowManifestStrategy>>();

            foreach (var workflowType in workflowTypes)
            {
                if (workflowType == "platform")
                {
                    selectedSets.Add(Tuple.Create(workflowType, manifest.Platform, (WorkflowManifestStrategy)null));
                    continue;
                }

                // "workload" and "infra" both use the branch strategy to select which strategy's files to copy
                var workflowSet = workflowType == "infra" ? manifest.Infra : manifest.Workload;

                branchStrategy = ResolveBranchStrategy();
                if (branchStrategy == null)
                {
                    return;
                }

                if (!workflowSet.Strategies.TryGetValue(branchStrategy, out var strategy))
                {
                    ThrowTerminatingError(new ErrorRecord(
                        new ArgumentException($"Branch strategy '{branchStrategy}' isn't declared for the {workflowType} workflow in '{manifestPath}'. Available strategies: {string.Join(", ", workflowSet.Strategies.Keys)}."),
                        "PlatformWorkflowStrategyNotFound",
                        ErrorCategory.InvalidArgument,
                        branchStrategy));
                    return;
                }

                selectedSets.Add(Tuple.Create(workflowType, workflowSet, strategy));
            }

            if (selectedSets.Count == 0)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new InvalidOperationException("The manifest does not contain a selected workflow set."),
                    "PlatformWorkflowTypeUnavailable",
                    ErrorCategory.InvalidData,
                    manifestPath));
                return;
            }

            WriteVerbose($"Using workflow type '{WorkflowType}'" +
                (branchStrategy == null ? string.Empty : $" with branch strategy '{branchStrategy}'") +
                $" from manifest version {manifest.TemplateVersion}.");

            var result = new PlatformWorkflowResult
            {
                BranchStrategy = branchStrategy,
                WorkflowType = WorkflowType.ToLowerInvariant(),
                ManifestPath = manifestPath,
                OutputFolder = outputRootPath,
                Success = true
            };

            foreach (var selectedSet in selectedSets)
            {
                if (selectedSet.Item3 != null)
                {
                    result.Environments.AddRange(selectedSet.Item3.Environments);
                }
            }

            // Tracks destinations already handled in this invocation, so a file shared by multiple
            // selected bundles (e.g. the setup action, present in platform/workload/infra's own
            // "shared" list) is only copied/checked once instead of warning on every later bundle.
            var handledDestinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var selectedSet in selectedSets)
            {
                var files = selectedSet.Item2.Shared.Concat(selectedSet.Item2.Files);
                if (selectedSet.Item3 != null)
                {
                    files = files.Concat(selectedSet.Item3.Files);
                }

                foreach (var file in files)
                {
                    var sourcePath = Path.Combine(inputRootPath, file.Source.Replace('/', Path.DirectorySeparatorChar));
                    var destinationPath = Path.Combine(outputRootPath, file.Destination.Replace('/', Path.DirectorySeparatorChar));

                    if (!handledDestinations.Add(destinationPath))
                    {
                        continue;
                    }

                    if (!File.Exists(sourcePath))
                    {
                        result.Success = false;
                        WriteError(new ErrorRecord(
                            new FileNotFoundException($"Workflow template '{file.Source}' was not found in '{inputRootPath}'. Run Get-PlatformTemplate first.", sourcePath),
                            "PlatformWorkflowTemplateNotFound",
                            ErrorCategory.ObjectNotFound,
                            sourcePath));
                        continue;
                    }

                    if (File.Exists(destinationPath) && !Force.IsPresent)
                    {
                        result.SkippedFiles.Add(destinationPath);
                        WriteWarning($"'{file.Destination}' already exists and was left untouched. Use -Force to overwrite it.");
                        continue;
                    }

                    if (!ShouldProcess(destinationPath, $"Copy workflow template '{file.Source}'"))
                    {
                        continue;
                    }

                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                        File.Copy(sourcePath, destinationPath, overwrite: true);
                        result.CopiedFiles.Add(destinationPath);
                        WriteVerbose($"Copied '{file.Source}' to '{destinationPath}'.");
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                    {
                        result.Success = false;
                        WriteError(new ErrorRecord(
                            new InvalidOperationException($"Failed to copy '{file.Source}' to '{destinationPath}': {ex.Message}", ex),
                            "PlatformWorkflowCopyFailed",
                            ErrorCategory.WriteError,
                            destinationPath));
                    }
                }
            }

            WriteObject(result);
        }

        private string ResolveBranchStrategy()
        {
            if (!string.IsNullOrWhiteSpace(BranchStrategy))
            {
                return BranchStrategy.Trim();
            }

            var globalConfigPath = GetUnresolvedProviderPathFromPSPath(GlobalConfigPath);

            try
            {
                var globalConfig = PlatformConfigLoader.LoadGlobalConfig(globalConfigPath);
                var strategy = globalConfig.SourceControl?.BranchStrategy;

                if (string.IsNullOrWhiteSpace(strategy))
                {
                    ThrowTerminatingError(new ErrorRecord(
                        new InvalidOperationException($"'{globalConfigPath}' has no 'sourceControl.branchStrategy'. Set it, or pass -BranchStrategy."),
                        "PlatformWorkflowBranchStrategyMissing",
                        ErrorCategory.InvalidData,
                        globalConfigPath));
                    return null;
                }

                WriteVerbose($"Resolved branch strategy '{strategy}' from '{globalConfigPath}'.");
                return strategy.Trim();
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is InvalidOperationException || ex is System.Text.Json.JsonException)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new InvalidOperationException($"Failed to resolve the branch strategy from '{globalConfigPath}': {ex.Message}", ex),
                    "PlatformWorkflowBranchStrategyUnresolved",
                    ErrorCategory.InvalidData,
                    globalConfigPath));
                return null;
            }
        }
    }
}
