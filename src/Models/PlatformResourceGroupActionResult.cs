namespace MSCKite.Azure.Platform.Models
{
    public class PlatformResourceGroupActionResult
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Location { get; set; }

        // "Created", "Updated", "Unchanged", "PlannedCreate", or "PlannedUpdate"
        public string Action { get; set; }
    }
}
