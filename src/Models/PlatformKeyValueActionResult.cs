namespace MSCKite.Azure.Platform.Models
{
    public class PlatformKeyValueActionResult
    {
        public string Name { get; set; }

        // "Created", "Updated", "Unchanged", or "Set" (secrets are never readable back, so their action is always "Set")
        public string Action { get; set; }
    }
}
