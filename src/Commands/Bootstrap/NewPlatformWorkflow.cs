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

        // Falls back to sourceControl.branchStrategy in global-config.jsonc when not specified
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string BranchStrategy { get; set; }

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

            var branchStrategy = ResolveBranchStrategy();
            if (branchStrategy == null)
            {
                return;
            }

            if (!manifest.Strategies.TryGetValue(branchStrategy, out var strategy))
            {
                ThrowTerminatingError(new ErrorRecord(
                    new ArgumentException($"Branch strategy '{branchStrategy}' isn't declared in '{manifestPath}'. Available strategies: {string.Join(", ", manifest.Strategies.Keys)}."),
                    "PlatformWorkflowStrategyNotFound",
                    ErrorCategory.InvalidArgument,
                    branchStrategy));
                return;
            }

            WriteVerbose($"Using branch strategy '{strategy.Name}' from manifest version {manifest.TemplateVersion}.");

            var result = new PlatformWorkflowResult
            {
                BranchStrategy = strategy.Name,
                ManifestPath = manifestPath,
                OutputFolder = outputRootPath,
                Success = true
            };

            result.Environments.AddRange(strategy.Environments);

            foreach (var file in manifest.Shared.Concat(strategy.Files))
            {
                var sourcePath = Path.Combine(inputRootPath, file.Source.Replace('/', Path.DirectorySeparatorChar));
                var destinationPath = Path.Combine(outputRootPath, file.Destination.Replace('/', Path.DirectorySeparatorChar));

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
