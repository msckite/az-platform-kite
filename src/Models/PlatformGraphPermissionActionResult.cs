namespace MSCKite.Azure.Platform.Models
{
    public class PlatformGraphPermissionActionResult
    {
        public string Permission { get; set; }

        // "Granted" or "Unchanged"
        public string Action { get; set; }
    }
}
