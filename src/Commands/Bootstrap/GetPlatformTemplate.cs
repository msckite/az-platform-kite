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

        private const string FallbackBranch = "main";

        private const string LatestReference = "latest";

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

        // Branch or tag to download. Defaults to the release tag of the running module (e.g. "v1.1.0"), so templates and schemas always match
        // the module that consumes them; "latest" resolves to the newest stable release tag
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string Branch { get; set; }

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

            var reference = ResolveReference();
            var result = new RepoTemplateDownloadResult { RepositoryUrl = RepositoryUrl, Branch = reference, OutputFolder = outputRootPath };

            try
            {
                WriteVerbose($"Cloning '{RepositoryUrl}' (ref '{reference}') to '{tempClonePath}'.");

                if (!GitRepositoryHelper.Clone(RepositoryUrl, reference, tempClonePath, out var cloneError))
                {
                    var hint = string.IsNullOrEmpty(Branch) && GitRepositoryHelper.IsReleaseTag(reference)
                        ? $" Release tag '{reference}' matches the running module version; pass -Branch latest or -Branch main to download another version."
                        : string.Empty;
                    ThrowTerminatingError(new ErrorRecord(
                        new InvalidOperationException($"Failed to clone '{RepositoryUrl}': {cloneError?.Trim()}{hint}"),
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
                            new DirectoryNotFoundException($"Folder '{normalizedFolder}' was not found in '{RepositoryUrl}' (ref '{reference}')."),
                            "PlatformTemplateFolderNotFound",
                            ErrorCategory.ObjectNotFound,
                            normalizedFolder));
                        continue;
                    }

                    var destinationPath = Path.Combine(outputRootPath, normalizedFolder.Replace('/', Path.DirectorySeparatorChar));
                    var pinTag = GitRepositoryHelper.IsReleaseTag(reference) ? reference : null;
                    CopyDirectory(sourcePath, destinationPath, Force.IsPresent, pinTag, result.CopiedFiles);
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

        // Explicit -Branch wins; otherwise the running module's release tag, falling back to main for a development build without a release version
        private string ResolveReference()
        {
            if (string.Equals(Branch, LatestReference, StringComparison.OrdinalIgnoreCase))
            {
                var latestTag = GitRepositoryHelper.GetLatestReleaseTag(RepositoryUrl, out var tagError);
                if (latestTag == null)
                {
                    ThrowTerminatingError(new ErrorRecord(
                        new InvalidOperationException($"Could not resolve the latest release of '{RepositoryUrl}': {tagError?.Trim()}"),
                        "PlatformTemplateLatestReleaseNotFound",
                        ErrorCategory.ReadError,
                        RepositoryUrl));
                }

                WriteVerbose($"Resolved 'latest' to release tag '{latestTag}'.");
                return latestTag;
            }

            if (!string.IsNullOrEmpty(Branch))
            {
                return Branch;
            }

            var kiteVersion = ModuleVersionHelper.GetKiteVersion(MyInvocation.MyCommand.Module);
            if (kiteVersion == null)
            {
                WriteWarning($"The running MSCKite.Azure.Platform module has no release version (development build); downloading from '{FallbackBranch}'. Pass -Branch to choose a release tag.");
                return FallbackBranch;
            }

            return $"v{kiteVersion}";
        }

        private static void CopyDirectory(string sourceDir, string destinationDir, bool force, string pinTag, List<string> copiedFiles)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (var filePath in Directory.GetFiles(sourceDir))
            {
                var destinationFile = Path.Combine(destinationDir, Path.GetFileName(filePath));
                File.Copy(filePath, destinationFile, force);

                // Point $schema/$id references at the downloaded release instead of main
                var extension = Path.GetExtension(filePath);
                if (pinTag != null && (extension == ".json" || extension == ".jsonc"))
                {
                    var content = File.ReadAllText(destinationFile);
                    var pinned = SchemaUrlHelper.PinToTag(content, pinTag);
                    if (content != pinned)
                    {
                        File.WriteAllText(destinationFile, pinned);
                    }
                }

                copiedFiles.Add(destinationFile);
            }

            foreach (var subDirectory in Directory.GetDirectories(sourceDir))
            {
                var destinationSubDirectory = Path.Combine(destinationDir, Path.GetFileName(subDirectory));
                CopyDirectory(subDirectory, destinationSubDirectory, force, pinTag, copiedFiles);
            }
        }
    }
}
