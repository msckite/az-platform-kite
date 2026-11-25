using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Text.Json;
using MSCKite.Azure.Platform.Internal.Azure;
using MSCKite.Azure.Platform.Internal.Common;
using MSCKite.Azure.Platform.Internal.Platform;
using MSCKite.Azure.Platform.Internal.Platform.Phases;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Commands.Platform
{
    // Phase 2: creates or updates the Microsoft Entra security groups declared in the platform config files, and assigns their RBAC roles against the resource groups from phase 1
    [Cmdlet(VerbsCommon.Set, "PlatformSecurityGroup", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
    [OutputType(typeof(PlatformSecurityGroupActionResult))]
    [OutputType(typeof(Hashtable))]
    public class SetPlatformSecurityGroup : PSCmdlet
    {
        private static readonly PowerShellModule[] RequiredModules = {
            new PowerShellModule("Az.Resources", "6.0")
        };

        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string GlobalConfigPath { get; set; } = Path.Combine("config", "global-config.jsonc");

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string ResourceGroupsConfigPath { get; set; } = Path.Combine("config", "platform", "resource-groups.jsonc");

        [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string SecurityGroupsConfigPath { get; set; } = Path.Combine("config", "platform", "security-groups.jsonc");

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
            var resourceGroupsConfigPath = GetUnresolvedProviderPathFromPSPath(ResourceGroupsConfigPath);
            var securityGroupsConfigPath = GetUnresolvedProviderPathFromPSPath(SecurityGroupsConfigPath);

            WriteVerbose($"Loading global config from '{globalConfigPath}'.");
            WriteVerbose($"Loading resource groups config from '{resourceGroupsConfigPath}'.");
            WriteVerbose($"Loading security groups config from '{securityGroupsConfigPath}'.");

            GlobalConfig globalConfig;
            List<PlatformResourceGroupConfig> resourceGroupConfigs;
            List<PlatformSecurityGroupConfig> securityGroupConfigs;
            try
            {
                globalConfig = PlatformConfigLoader.LoadGlobalConfig(globalConfigPath);
                resourceGroupConfigs = PlatformConfigLoader.LoadResourceGroups(resourceGroupsConfigPath);
                securityGroupConfigs = PlatformConfigLoader.LoadSecurityGroups(securityGroupsConfigPath);
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidOperationException || ex is JsonException)
            {
                ThrowTerminatingError(new ErrorRecord(ex, "PlatformSecurityGroupInvalidConfig", ErrorCategory.InvalidData, SecurityGroupsConfigPath));
                return;
            }

            WriteVerbose($"Found {securityGroupConfigs.Count} security group(s) in the config.");

            if (!AzureContextHelper.TryGetConfiguredContext(this, globalConfig, out var azureContext, out var azureMessage))
            {
                ThrowTerminatingError(new ErrorRecord(
                    new InvalidOperationException(azureMessage),
                    azureContext.IsSignedIn ? "PlatformSecurityGroupContextMismatch" : "PlatformSecurityGroupNotSignedIn",
                    azureContext.IsSignedIn ? ErrorCategory.SecurityError : ErrorCategory.AuthenticationError,
                    null));
                return;
            }

            // Resolve every resourceGroupId referenced anywhere in this config up front, so a missing phase-1 resource group fails fast with a clear message
            var resourceGroupsById = PlatformResourceGroupResolver.Resolve(
                this, resourceGroupConfigs, globalConfig.ToPlaceholderMap(), "PlatformSecurityGroupResourceGroupMissing");

            var items = SecurityGroupPhase.Run(this, globalConfig, securityGroupConfigs, resourceGroupsById);

            if (AsHashtable.IsPresent)
            {
                WriteObject(new Hashtable
                {
                    ["IsWhatIf"] = MyInvocation.BoundParameters.ContainsKey("WhatIf"),
                    ["Count"] = items.Count,
                    ["SecurityGroups"] = items
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
    }
}
