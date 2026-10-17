using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Internal.Platform
{
    // Raw (unresolved) representation of one entry in resource-groups.jsonc's "resourceGroups" array
    internal class PlatformResourceGroupConfig
    {
        internal string Id { get; set; }

        internal string Name { get; set; }

        internal string Location { get; set; }

        internal Dictionary<string, string> Tags { get; } = new Dictionary<string, string>();
    }
}
