namespace MSCKite.Azure.Platform.Models
{
    public readonly struct PowerShellModule
    {
        public PowerShellModule(string moduleName, string minimumVersion)
        {
            ModuleName = moduleName;
            MinimumVersion = minimumVersion;
        }

        public string ModuleName { get; }
        public string MinimumVersion { get; }
    }
}
