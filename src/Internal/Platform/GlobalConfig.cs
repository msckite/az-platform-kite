using System.Collections.Generic;

namespace MSCKite.Azure.Platform.Internal.Platform
{
    // Parsed representation of global-config.jsonc, used to resolve ${placeholder} tokens in platform-config.jsonc
    internal class GlobalConfig
    {
        internal string TenantId { get; set; }

        internal string SubscriptionId { get; set; }

        internal string UniqueId { get; set; }

        internal string ServiceShort { get; set; }

        internal string DisplayName { get; set; }

        internal string Location { get; set; }

        internal string RegionCode { get; set; }

        internal SourceControlConfig SourceControl { get; set; }

        // Every value a platform-config.jsonc template may reference via ${key}
        internal Dictionary<string, string> ToPlaceholderMap()
        {
            var map = new Dictionary<string, string>
            {
                ["tenantId"] = TenantId,
                ["subscriptionId"] = SubscriptionId,
                ["uniqueId"] = UniqueId,
                ["serviceShort"] = ServiceShort,
                ["displayName"] = DisplayName,
                ["location"] = Location,
                ["regionCode"] = RegionCode
            };

            if (SourceControl != null)
            {
                map["owner"] = SourceControl.Owner;
                map["repository"] = SourceControl.Repository;
                map["tool"] = SourceControl.Tool;
            }

            return map;
        }
    }

    internal class SourceControlConfig
    {
        internal string Tool { get; set; }

        internal string Owner { get; set; }

        internal string Repository { get; set; }

        internal string BranchStrategy { get; set; }
    }
}
