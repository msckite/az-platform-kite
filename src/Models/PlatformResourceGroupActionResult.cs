using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Models
{
    public class PlatformResourceGroupActionResult
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Location { get; set; }

        // ARM id of the resource group; for "WouldCreate" it is the id the group will get once created
        public string ResourceId { get; set; }

        public Dictionary<string, string> Tags { get; set; }

        // "Created", "Updated", "Unchanged", "WouldCreate", or "WouldUpdate"
        public string Action { get; set; }
    }
}
