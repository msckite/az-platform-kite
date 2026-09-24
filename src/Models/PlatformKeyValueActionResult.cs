namespace MSCKite.Azure.Platform.Models
{
    public class PlatformKeyValueActionResult
    {
        public string Name { get; set; }

        // "Created", "Updated", "Unchanged", "Set", "WouldCreate", or "WouldUpdate"
        public string Action { get; set; }
    }
}
