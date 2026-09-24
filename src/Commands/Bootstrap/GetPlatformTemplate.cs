using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using MSCKite.Azure.Platform.Internal.Common;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Commands.Bootstrap
{
    [Cmdlet(VerbsCommon.Get, "PlatformTemplate", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    [OutputType(typeof(RepoTemplateDownloadResult))]
    public class GetPlatformTemplate : PSCmdlet
    {
        private const string DefaultRepositoryUrl = "https://github.com/msckite/az-platform-kite.git";

        private const string DefaultBranch = "main";

        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string[] IncludedFolders { get; set; } = { "templates" };

        // Defaults to a dedicated folder so reruns never overwrite config already in use
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string OutputFolder { get; set; } = ".tmp";

        [Parameter]
        [ValidateNotNullOrEmpty]
        public string RepositoryUrl { get; set; } = DefaultRepositoryUrl;

        [Parameter]
        [ValidateNotNullOrEmpty]
        public string Branch { get; set; } = DefaultBranch;

        // Overwrites files that already exist at the destination instead of failing
        [Parameter]
        public SwitchParameter Force { get; set; }

        // Shallow-clones the repository to a temp folder, copies the requested folders (with subfolders/files) to OutputFolder, then deletes the temp clone
        protected override void ProcessRecord()
        {
            var outputRootPath = GetUnresolvedProviderPathFromPSPath(OutputFolder);
            var tempClonePath = Path.Combine(Path.GetTempPath(), "az-platform-kite-" + Guid.NewGuid().ToString("N"));

            if (!ShouldProcess(outputRootPath, $"Download folder(s) '{string.Join(", ", IncludedFolders)}' from '{RepositoryUrl}'"))
            {
                return;
            }

            var result = new RepoTemplateDownloadResult { RepositoryUrl = RepositoryUrl, Branch = Branch, OutputFolder = outputRootPath };

            try
            {
                WriteVerbose($"Cloning '{RepositoryUrl}' (branch '{Branch}') to '{tempClonePath}'.");

                if (!GitRepositoryHelper.Clone(RepositoryUrl, Branch, tempClonePath, out var cloneError))
                {
                    ThrowTerminatingError(new ErrorRecord(
                        new InvalidOperationException($"Failed to clone '{RepositoryUrl}': {cloneError}"),
                        "PlatformTemplateCloneFailed",
                        ErrorCategory.ReadError,
                        RepositoryUrl));
                    return;
                }

                foreach (var includedFolder in IncludedFolders)
                {
                    var normalizedFolder = includedFolder.Trim().Replace('\\', '/').Trim('/');
                    var sourcePath = Path.Combine(tempClonePath, normalizedFolder.Replace('/', Path.DirectorySeparatorChar));

                    if (!Directory.Exists(sourcePath))
                    {
                        WriteError(new ErrorRecord(
                            new DirectoryNotFoundException($"Folder '{normalizedFolder}' was not found in '{RepositoryUrl}' (branch '{Branch}')."),
                            "PlatformTemplateFolderNotFound",
                            ErrorCategory.ObjectNotFound,
                            normalizedFolder));
                        continue;
                    }

                    var destinationPath = Path.Combine(outputRootPath, normalizedFolder.Replace('/', Path.DirectorySeparatorChar));
                    CopyDirectory(sourcePath, destinationPath, Force.IsPresent, result.CopiedFiles);
                    result.CopiedFolders.Add(destinationPath);
                    WriteVerbose($"Copied '{sourcePath}' to '{destinationPath}'.");
                }

                result.Success = true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new InvalidOperationException($"Failed to download repository folder(s): {ex.Message}", ex),
                    "PlatformTemplateCopyFailed",
                    ErrorCategory.WriteError,
                    outputRootPath));
                return;
            }
            finally
            {
                RemoveTempClone(tempClonePath);
            }

            WriteObject(result);
        }

        private void RemoveTempClone(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            try
            {
                // git stores pack/object files as read-only, which blocks Directory.Delete
                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }

                Directory.Delete(path, recursive: true);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                WriteWarning($"Failed to remove temporary clone folder '{path}': {ex.Message}");
            }
        }

        private static void CopyDirectory(string sourceDir, string destinationDir, bool force, List<string> copiedFiles)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (var filePath in Directory.GetFiles(sourceDir))
            {
                var destinationFile = Path.Combine(destinationDir, Path.GetFileName(filePath));
                File.Copy(filePath, destinationFile, force);
                copiedFiles.Add(destinationFile);
            }

            foreach (var subDirectory in Directory.GetDirectories(sourceDir))
            {
                var destinationSubDirectory = Path.Combine(destinationDir, Path.GetFileName(subDirectory));
                CopyDirectory(subDirectory, destinationSubDirectory, force, copiedFiles);
            }
        }
    }
}
