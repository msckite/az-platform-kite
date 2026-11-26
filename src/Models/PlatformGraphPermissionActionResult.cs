namespace MSCKite.Azure.Platform.Models
{
    public class PlatformGraphPermissionActionResult
    {
        public string Permission { get; set; }

        // "Granted", "Unchanged", or "WouldGrant"
        public string Action { get; set; }
    }
}
