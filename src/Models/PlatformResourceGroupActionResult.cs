using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Models
{
    public class PlatformResourceGroupActionResult
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Location { get; set; }

        public Dictionary<string, string> Tags { get; set; }

        // "Created", "Updated", "Unchanged", "WouldCreate", or "WouldUpdate"
        public string Action { get; set; }
    }
}
