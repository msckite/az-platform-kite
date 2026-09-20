using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MSCKite.Azure.Platform.Internal.Azure
{
    // Current state of an Azure resource group, as returned by Get-AzResourceGroup
    internal class AzureResourceGroupInfo
    {
        internal string Name { get; set; }

        internal string Location { get; set; }

        internal string ResourceId { get; set; }

        internal Dictionary<string, string> Tags { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    // Creates, updates, and reads Azure resource groups via the Az.Resources module
    internal static class AzureResourceGroupHelper
    {
        private const string GetScript = @"
param($Name)
$rg = Get-AzResourceGroup -Name $Name -ErrorAction SilentlyContinue
if ($rg) {
    [PSCustomObject]@{
        Name       = $rg.ResourceGroupName
        Location   = $rg.Location
        ResourceId = $rg.ResourceId
        Tags       = $rg.Tags
    }
}
";

        private const string CreateScript = @"
param($Name, $Location, $Tags)
New-AzResourceGroup -Name $Name -Location $Location -Tag $Tags | Out-Null
";

        private const string UpdateTagsScript = @"
param($Name, $Tags)
Set-AzResourceGroup -Name $Name -Tag $Tags | Out-Null
";

        // Returns null if the resource group doesn't exist (or isn't yet visible)
        internal static AzureResourceGroupInfo Get(PSCmdlet cmdlet, string name)
        {
            var result = cmdlet.InvokeCommand.InvokeScript(GetScript, name).FirstOrDefault();
            if (result == null)
            {
                return null;
            }

            var info = new AzureResourceGroupInfo
            {
                Name = result.Properties["Name"]?.Value as string,
                Location = result.Properties["Location"]?.Value as string,
                ResourceId = result.Properties["ResourceId"]?.Value as string
            };

            if (result.Properties["Tags"]?.Value is IDictionary tags)
            {
                foreach (DictionaryEntry entry in tags)
                {
                    info.Tags[entry.Key.ToString()] = entry.Value?.ToString() ?? string.Empty;
                }
            }

            return info;
        }

        // Creates the resource group, then waits for it to become readable before returning (protects callers that immediately assign roles to it)
        internal static AzureResourceGroupInfo Create(PSCmdlet cmdlet, string name, string location, Dictionary<string, string> tags)
        {
            cmdlet.InvokeCommand.InvokeScript(CreateScript, name, location, ToHashtable(tags));
            return AzurePropagationHelper.WaitUntilReadable(() => Get(cmdlet, name), cmdlet.WriteVerbose);
        }

        internal static void UpdateTags(PSCmdlet cmdlet, string name, Dictionary<string, string> tags)
        {
            cmdlet.InvokeCommand.InvokeScript(UpdateTagsScript, name, ToHashtable(tags));
        }

        private static Hashtable ToHashtable(Dictionary<string, string> tags)
        {
            var hashtable = new Hashtable();
            foreach (var tag in tags)
            {
                hashtable[tag.Key] = tag.Value;
            }

            return hashtable;
        }
    }
}
