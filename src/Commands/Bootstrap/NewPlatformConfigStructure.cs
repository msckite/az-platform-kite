using System;
using System.IO;
using System.Management.Automation;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Commands.Bootstrap
{
    [Cmdlet(VerbsCommon.New, "PlatformConfigStructure", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    [OutputType(typeof(PlatformConfigFolderResult))]
    public class NewPlatformConfigStructure : PSCmdlet
    {
        private static readonly string[] SubFolders = { }; // "azure", "entra", "github"

        private const string GlobalConfigFileName = "global-config.jsonc";

        private const string DefaultGlobalConfigTemplate =
@"{
  ""$schema"": ""https://raw.githubusercontent.com/msckite/az-platform-kite/refs/heads/main/schemas/global-config.schema.json"",
    ""templateVersion"": ""1.0.0"",
  ""tenantId"": """",
  ""subscriptionId"": """",
  ""uniqueId"": """",
  ""serviceShort"": """",
  ""displayName"": """",
  ""location"": ""westeurope"",
  ""regionCode"": ""weu"",
  ""sourceControl"": {
    ""tool"": ""github"",
    ""owner"": """",
    ""repository"": """",
    ""branchStrategy"": ""github"" // Default ""github""
  }
}
";

        [Parameter]
        [ValidateNotNullOrEmpty]
        public string InputFolder { get; set; }

        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string OutputFolder { get; set; } = "config";

        // Overwrites an already-initialized configuration folder instead of failing
        [Parameter]
        public SwitchParameter Force { get; set; }

        // Scaffolds the standard platform configuration folder structure (global-config.jsonc plus azure/devops/entra/github subfolders)
        protected override void ProcessRecord()
        {
            var configRootPath = GetUnresolvedProviderPathFromPSPath(OutputFolder);
            var hasInputFolder = !string.IsNullOrWhiteSpace(InputFolder);

            string sourceGlobalSettingsPath = null;
            var globalConfigPath = System.IO.Path.Combine(configRootPath, GlobalConfigFileName);

            if (hasInputFolder)
            {
                var resolvedInputFolder = GetUnresolvedProviderPathFromPSPath(InputFolder);
                sourceGlobalSettingsPath = System.IO.Path.Combine(resolvedInputFolder, "global-config.jsonc");
            }

            var alreadyInitialized = Directory.Exists(configRootPath) &&
                (File.Exists(globalConfigPath) || Array.Exists(SubFolders, folder => Directory.Exists(System.IO.Path.Combine(configRootPath, folder))));

            if (alreadyInitialized && !Force.IsPresent)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new InvalidOperationException($"'{configRootPath}' already contains a platform configuration structure. Use -Force to overwrite it."),
                    "PlatformConfigFolderAlreadyInitialized",
                    ErrorCategory.ResourceExists,
                    configRootPath));
                return;
            }

            if (!ShouldProcess(configRootPath, "Create platform configuration folder"))
            {
                return;
            }

            var result = new PlatformConfigFolderResult { ConfigRootPath = configRootPath };

            try
            {
                if (hasInputFolder && !File.Exists(sourceGlobalSettingsPath))
                {
                    ThrowTerminatingError(new ErrorRecord(
                        new FileNotFoundException($"Could not find 'global-config.jsonc' in '{InputFolder}'.", sourceGlobalSettingsPath),
                        "PlatformConfigSourceFileNotFound",
                        ErrorCategory.ObjectNotFound,
                        sourceGlobalSettingsPath));
                    return;
                }

                if (!Directory.Exists(configRootPath))
                {
                    Directory.CreateDirectory(configRootPath);
                    WriteVerbose($"Created folder '{configRootPath}'.");
                    result.CreatedFolders.Add(configRootPath);
                }

                foreach (var subFolder in SubFolders)
                {
                    var subFolderPath = System.IO.Path.Combine(configRootPath, subFolder);
                    if (Directory.Exists(subFolderPath))
                    {
                        continue;
                    }

                    Directory.CreateDirectory(subFolderPath);
                    WriteVerbose($"Created folder '{subFolderPath}'.");
                    result.CreatedFolders.Add(subFolderPath);
                }

                if (hasInputFolder)
                {
                    File.Copy(sourceGlobalSettingsPath, globalConfigPath, Force.IsPresent);
                    WriteVerbose($"Copied '{sourceGlobalSettingsPath}' to '{globalConfigPath}'.");
                }
                else
                {
                    File.WriteAllText(globalConfigPath, DefaultGlobalConfigTemplate);
                    WriteVerbose($"Created configuration file '{globalConfigPath}'.");
                }

                result.GlobalConfigPath = globalConfigPath;
                result.Success = true;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new InvalidOperationException($"Failed to create platform configuration folder at '{configRootPath}': {ex.Message}", ex),
                    "PlatformConfigFolderCreateFailed",
                    ErrorCategory.WriteError,
                    configRootPath));
                return;
            }

            WriteObject(result);
        }
    }
}
