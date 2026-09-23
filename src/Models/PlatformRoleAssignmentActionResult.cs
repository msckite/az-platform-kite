namespace MSCKite.Azure.Platform.Models
{
    public class PlatformRoleAssignmentActionResult
    {
        public string Role { get; set; }

        public string Scope { get; set; }

        // "Added", "Unchanged", or "WouldAdd"
        public string Action { get; set; }
    }
}
