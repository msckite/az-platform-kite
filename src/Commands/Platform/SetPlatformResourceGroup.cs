using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text.Json;
using MSCKite.Azure.Platform.Internal.Azure;
using MSCKite.Azure.Platform.Internal.Common;
using MSCKite.Azure.Platform.Internal.Platform;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Commands.Platform
{
    // Phase 1: creates or updates the Azure resource groups declared in platform-config.jsonc, ahead of any security group, identity, or GitHub environment resources
    [Cmdlet(VerbsCommon.Set, "PlatformResourceGroup", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
    [OutputType(typeof(PlatformResourceGroupActionResult))]
    [OutputType(typeof(Hashtable))]
    public class SetPlatformResourceGroup : PSCmdlet
    {
        private static readonly PowerShellModule[] RequiredModules = {
            new PowerShellModule("Az.Resources", "6.0")
        };

        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string GlobalConfigPath { get; set; } = Path.Combine("config", "global-config.jsonc");

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string PlatformConfigPath { get; set; } = Path.Combine("config", "platform-config.jsonc");

        // Collects every result in memory and emits a single summary Hashtable at the end, instead of streaming each result as it's created
        [Parameter]
        public SwitchParameter AsHashtable { get; set; }

        protected override void BeginProcessing()
        {
            PowerShellModuleLoader.Import(this, (PowerShellModule[])(object)RequiredModules);
        }

        protected override void ProcessRecord()
        {
            var globalConfigPath = GetUnresolvedProviderPathFromPSPath(GlobalConfigPath);
            var platformConfigPath = GetUnresolvedProviderPathFromPSPath(PlatformConfigPath);

            WriteVerbose($"Loading global config from '{globalConfigPath}'.");
            WriteVerbose($"Loading platform config from '{platformConfigPath}'.");

            GlobalConfig globalConfig;
            List<PlatformResourceGroupConfig> resourceGroups;
            try
            {
                globalConfig = PlatformConfigLoader.LoadGlobalConfig(globalConfigPath);
                resourceGroups = PlatformConfigLoader.LoadResourceGroups(platformConfigPath);
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidOperationException || ex is JsonException)
            {
                ThrowTerminatingError(new ErrorRecord(ex, "PlatformResourceGroupInvalidConfig", ErrorCategory.InvalidData, PlatformConfigPath));
                return;
            }

            WriteVerbose($"Found {resourceGroups.Count} resource group(s) in platform config.");

            var azureContext = AzureContextHelper.GetContext(this, out var azureMessage);
            if (!azureContext.IsSignedIn)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new InvalidOperationException(azureMessage),
                    "PlatformResourceGroupNotSignedIn", ErrorCategory.AuthenticationError, null));
                return;
            }

            var placeholders = globalConfig.ToPlaceholderMap();
            var items = new List<PlatformResourceGroupActionResult>();

            // Buffers each result instead of streaming it, so ShouldProcess's "What if:" host messages don't interleave with pipeline output
            void Emit(PlatformResourceGroupActionResult item)
            {
                items.Add(item);
            }

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
                    WriteError(new ErrorRecord(ex, "PlatformResourceGroupUnresolvedPlaceholder", ErrorCategory.InvalidData, config.Id));
                    continue;
                }

                WriteVerbose($"Resource group '{config.Id}': checking for '{name}'.");
                var existing = AzureResourceGroupHelper.Get(this, name);

                if (existing == null)
                {
                    WriteVerbose($"Resource group '{name}' does not exist yet.");
                    if (!ShouldProcess(name, "Create resource group"))
                    {
                        Emit(new PlatformResourceGroupActionResult
                        {
                            Id = config.Id,
                            Name = name,
                            Location = location,
                            Tags = tags,
                            Action = "WouldCreate"
                        });
                        continue;
                    }

                    WriteVerbose($"Creating resource group '{name}' in '{location}'.");
                    var created = AzureResourceGroupHelper.Create(this, name, location, tags);
                    if (created == null)
                    {
                        WriteError(new ErrorRecord(
                            new InvalidOperationException($"Resource group '{name}' was created but is not yet readable. It may still be propagating; re-run this command to verify."),
                            "PlatformResourceGroupNotReadable", ErrorCategory.ReadError, name));
                        continue;
                    }

                    WriteVerbose($"Resource group '{name}' created and confirmed readable.");
                    Emit(new PlatformResourceGroupActionResult
                    {
                        Id = config.Id,
                        Name = created.Name,
                        Location = created.Location,
                        Tags = created.Tags,
                        Action = "Created"
                    });
                    continue;
                }

                WriteVerbose($"Resource group '{name}' already exists in '{existing.Location}'.");

                if (!string.Equals(existing.Location, location, StringComparison.OrdinalIgnoreCase))
                {
                    WriteWarning($"Resource group '{name}' already exists in location '{existing.Location}'; the configured location '{location}' is ignored because location cannot be changed after creation.");
                }

                if (TagsEqual(existing.Tags, tags))
                {
                    WriteVerbose($"Resource group '{name}' tags already match the configured values; nothing to do.");
                    Emit(new PlatformResourceGroupActionResult
                    {
                        Id = config.Id,
                        Name = existing.Name,
                        Location = existing.Location,
                        Tags = existing.Tags,
                        Action = "Unchanged"
                    });
                    continue;
                }

                WriteVerbose($"Resource group '{name}' tags have drifted from the configured values.");
                if (!ShouldProcess(name, "Update resource group tags"))
                {
                    Emit(new PlatformResourceGroupActionResult
                    {
                        Id = config.Id,
                        Name = existing.Name,
                        Location = existing.Location,
                        Tags = tags,
                        Action = "WouldUpdate"
                    });
                    continue;
                }

                WriteVerbose($"Updating tags on resource group '{name}'.");
                AzureResourceGroupHelper.UpdateTags(this, name, tags);
                Emit(new PlatformResourceGroupActionResult
                {
                    Id = config.Id,
                    Name = existing.Name,
                    Location = existing.Location,
                    Tags = tags,
                    Action = "Updated"
                });
            }

            WriteVerbose($"Done. Processed {items.Count} of {resourceGroups.Count} resource group(s).");
            if (AsHashtable.IsPresent)
            {
                WriteObject(new Hashtable
                {
                    ["IsWhatIf"] = MyInvocation.BoundParameters.ContainsKey("WhatIf"),
                    ["Count"] = items.Count,
                    ["ResourceGroups"] = items
                });
            }
            else
            {
                foreach (var item in items)
                {
                    WriteObject(item);
                }
            }
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
