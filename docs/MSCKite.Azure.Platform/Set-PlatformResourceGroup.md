---
document type: cmdlet
external help file: MSCKite.Azure.Platform.dll-Help.xml
HelpUri: ''
Locale: en-NL
Module Name: MSCKite.Azure.Platform
ms.date: 09/20/2026
PlatyPS schema version: 2024-05-01
title: Set-PlatformResourceGroup
---

# Set-PlatformResourceGroup

## SYNOPSIS

Creates or updates the Azure resource groups declared in platform-config.jsonc.

## SYNTAX

### __AllParameterSets

```
Set-PlatformResourceGroup [[-GlobalConfigPath] <string>] [[-PlatformConfigPath] <string>] [-WhatIf]
 [-Confirm]
```

## ALIASES

## DESCRIPTION

Phase 1 of the platform-config.jsonc provisioning workflow.
Reads the `resourceGroups` array,
resolves `${placeholder}` tokens against global-config.jsonc, and creates any resource group that
doesn't exist yet or updates its tags if they've drifted from the configured values. A resource
group's location cannot be changed after creation, so a configured location that no longer
matches the live resource group only produces a warning.

This cmdlet supports `-WhatIf`/`-Confirm` and requires the `Az.Resources` module, signed in via
`Connect-AzAccount` with permission to create and tag resource groups in the target subscription.

## EXAMPLES

### Example 1 - Sync resource groups using the default config paths

```powershell
Set-PlatformResourceGroup
```

Creates or updates the resource groups declared in `config/global-config.jsonc` and
`config/platform-config.jsonc`.

### Example 2 - Preview changes without applying them

```powershell
Set-PlatformResourceGroup -GlobalConfigPath ./config/global-config.jsonc -PlatformConfigPath ./config/platform-config.jsonc -WhatIf
```

## PARAMETERS

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases:
- cf
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -GlobalConfigPath

Path to the global-config.jsonc file whose values resolve `${placeholder}` tokens in
platform-config.jsonc.
Defaults to `config/global-config.jsonc`.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -PlatformConfigPath

Path to the platform-config.jsonc file whose `resourceGroups` array is provisioned.
Defaults to
`config/platform-config.jsonc`.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -WhatIf

Runs the command in a mode that only reports what would happen without performing the actions.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases:
- wi
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

You can pipe the global config path or platform config path to this cmdlet by property name.

## OUTPUTS

### MSCKite.Azure.Platform.Models.PlatformResourceGroupSyncResult

The Id, Name, Location, and Action (`Created`, `Updated`, or `Unchanged`) of each resource group
processed.

## NOTES

This cmdlet requires the `Az.Resources` module and an active Azure sign-in
(`Connect-AzAccount`) with permission to create and tag resource groups in the target
subscription.

## RELATED LINKS

- [Set-PlatformSecurityGroup](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/Set-PlatformSecurityGroup.md)

