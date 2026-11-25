using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using MSCKite.Azure.Platform.Internal.Azure;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Internal.Platform.Phases
{
    // Phase 1: creates or updates the Azure resource groups declared in the platform config files, ahead of any security group, identity, or GitHub environment resources
    internal static class ResourceGroupPhase
    {
        // Returns the results instead of streaming them, so ShouldProcess's "What if:" host messages don't interleave with pipeline output
        internal static List<PlatformResourceGroupActionResult> Run(
            PSCmdlet cmdlet,
            GlobalConfig globalConfig,
            List<PlatformResourceGroupConfig> resourceGroups)
        {
            var placeholders = globalConfig.ToPlaceholderMap();
            var items = new List<PlatformResourceGroupActionResult>();

            foreach (var config in resourceGroups)
            {
                string name;
                string location;
                Dictionary<string, string> tags;
                try
                {
                    name = PlatformConfigLoader.ResolvePlaceholders(config.Name, placeholders);
                    location = PlatformConfigLoader.ResolvePlaceholders(config.Location, placeholders);
                    tags = config.Tags.ToDictionary(
                        tag => tag.Key,
                        tag => PlatformConfigLoader.ResolvePlaceholders(tag.Value, placeholders));
                }
                catch (InvalidOperationException ex)
                {
                    cmdlet.WritePhaseError(new ErrorRecord(ex, "PlatformResourceGroupUnresolvedPlaceholder", ErrorCategory.InvalidData, config.Id));
                    continue;
                }

                cmdlet.WriteVerbose($"Resource group '{config.Id}': checking for '{name}'.");
                var existing = AzureResourceGroupHelper.Get(cmdlet, name);

                if (existing == null)
                {
                    cmdlet.WriteVerbose($"Resource group '{name}' does not exist yet.");
                    if (!cmdlet.ShouldProcess(name, "Create resource group"))
                    {
                        items.Add(new PlatformResourceGroupActionResult
                        {
                            Id = config.Id,
                            Name = name,
                            Location = location,
                            ResourceId = $"/subscriptions/{globalConfig.SubscriptionId}/resourceGroups/{name}",
                            Tags = tags,
                            Action = "WouldCreate"
                        });
                        continue;
                    }

                    cmdlet.WriteVerbose($"Creating resource group '{name}' in '{location}'.");
                    var created = AzureResourceGroupHelper.Create(cmdlet, name, location, tags);
                    if (created == null)
                    {
                        cmdlet.WritePhaseError(new ErrorRecord(
                            new InvalidOperationException($"Resource group '{name}' was created but is not yet readable. It may still be propagating; re-run this command to verify."),
                            "PlatformResourceGroupNotReadable", ErrorCategory.ReadError, name));
                        continue;
                    }

                    cmdlet.WriteVerbose($"Resource group '{name}' created and confirmed readable.");
                    items.Add(new PlatformResourceGroupActionResult
                    {
                        Id = config.Id,
                        Name = created.Name,
                        Location = created.Location,
                        ResourceId = created.ResourceId,
                        Tags = created.Tags,
                        Action = "Created"
                    });
                    continue;
                }

                cmdlet.WriteVerbose($"Resource group '{name}' already exists in '{existing.Location}'.");

                if (!string.Equals(existing.Location, location, StringComparison.OrdinalIgnoreCase))
                {
                    cmdlet.WriteWarning($"Resource group '{name}' already exists in location '{existing.Location}'; the configured location '{location}' is ignored because location cannot be changed after creation.");
                }

                if (TagsEqual(existing.Tags, tags))
                {
                    cmdlet.WriteVerbose($"Resource group '{name}' tags already match the configured values; nothing to do.");
                    items.Add(new PlatformResourceGroupActionResult
                    {
                        Id = config.Id,
                        Name = existing.Name,
                        Location = existing.Location,
                        ResourceId = existing.ResourceId,
                        Tags = existing.Tags,
                        Action = "Unchanged"
                    });
                    continue;
                }

                cmdlet.WriteVerbose($"Resource group '{name}' tags have drifted from the configured values.");
                if (!cmdlet.ShouldProcess(name, "Update resource group tags"))
                {
                    items.Add(new PlatformResourceGroupActionResult
                    {
                        Id = config.Id,
                        Name = existing.Name,
                        Location = existing.Location,
                        ResourceId = existing.ResourceId,
                        Tags = tags,
                        Action = "WouldUpdate"
                    });
                    continue;
                }

                cmdlet.WriteVerbose($"Updating tags on resource group '{name}'.");
                AzureResourceGroupHelper.UpdateTags(cmdlet, name, tags);
                items.Add(new PlatformResourceGroupActionResult
                {
                    Id = config.Id,
                    Name = existing.Name,
                    Location = existing.Location,
                    ResourceId = existing.ResourceId,
                    Tags = tags,
                    Action = "Updated"
                });
            }

            cmdlet.WriteVerbose($"Done. Processed {items.Count} of {resourceGroups.Count} resource group(s).");
            return items;
        }

        private static bool TagsEqual(Dictionary<string, string> current, Dictionary<string, string> desired)
        {
            if (current.Count != desired.Count)
            {
                return false;
            }

            foreach (var tag in desired)
            {
                if (!current.TryGetValue(tag.Key, out var value) || !string.Equals(value, tag.Value, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
