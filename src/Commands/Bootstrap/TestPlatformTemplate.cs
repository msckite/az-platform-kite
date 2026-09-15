using System;
using System.IO;
using System.Management.Automation;
using System.Text.Json;
using MSCKite.Azure.Platform.Internal.Common;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Commands.Bootstrap
{
    [Cmdlet(VerbsDiagnostic.Test, "PlatformTemplate")]
    [OutputType(typeof(PlatformTemplateVersionResult))]
    public class TestPlatformTemplate : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [Alias("Path")]
        public string TemplatePath { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string SchemaPath { get; set; }

        [Parameter]
        [ValidateNotNullOrEmpty]
        public string LatestTemplatePath { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                var templatePath = GetUnresolvedProviderPathFromPSPath(TemplatePath);
                var schemaPath = GetUnresolvedProviderPathFromPSPath(SchemaPath);
                var templateVersion = TemplateVersionHelper.ReadVersion(templatePath, "templateVersion");
                var schemaVersion = TemplateVersionHelper.ReadVersion(schemaPath, "schemaVersion");
                var isCompatible = templateVersion == schemaVersion;
                Version latestTemplateVersion = null;

                if (!string.IsNullOrWhiteSpace(LatestTemplatePath))
                {
                    var latestTemplatePath = GetUnresolvedProviderPathFromPSPath(LatestTemplatePath);
                    latestTemplateVersion = TemplateVersionHelper.ReadVersion(latestTemplatePath, "templateVersion");
                }

                WriteObject(new PlatformTemplateVersionResult
                {
                    TemplatePath = templatePath,
                    SchemaPath = schemaPath,
                    TemplateVersion = templateVersion.ToString(3),
                    SchemaVersion = schemaVersion.ToString(3),
                    LatestTemplateVersion = latestTemplateVersion?.ToString(3),
                    IsCompatible = isCompatible,
                    IsUpdateAvailable = latestTemplateVersion != null && latestTemplateVersion > templateVersion,
                    CompatibilityMessage = isCompatible
                        ? "Template and schema versions match."
                        : "Template and schema versions differ; use the schema version that matches the template."
                });
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidOperationException || ex is JsonException)
            {
                ThrowTerminatingError(new ErrorRecord(ex, "PlatformTemplateVersionInvalid", ErrorCategory.InvalidData, TemplatePath));
            }
        }
    }
}
