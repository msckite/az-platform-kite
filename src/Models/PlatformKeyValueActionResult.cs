namespace MSCKite.Azure.Platform.Models
{
    public class PlatformKeyValueActionResult
    {
        public string Name { get; set; }

        // "Created", "Updated", "Unchanged", "Set", "PlannedCreate", or "PlannedUpdate"
        public string Action { get; set; }
    }
}
