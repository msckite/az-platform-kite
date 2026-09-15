namespace MSCKite.Azure.Platform.Models
{
    public class PlatformTemplateVersionResult
    {
        public string TemplatePath { get; set; }

        public string SchemaPath { get; set; }

        public string TemplateVersion { get; set; }

        public string SchemaVersion { get; set; }

        public string LatestTemplateVersion { get; set; }

        public bool IsCompatible { get; set; }

        public bool IsUpdateAvailable { get; set; }

        public string CompatibilityMessage { get; set; }
    }
}
