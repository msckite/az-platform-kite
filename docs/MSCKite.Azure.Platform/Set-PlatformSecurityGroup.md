---
document type: cmdlet
external help file: MSCKite.Azure.Platform.dll-Help.xml
HelpUri: ''
Locale: en-NL
Module Name: MSCKite.Azure.Platform
ms.date: 09/23/2026
PlatyPS schema version: 2024-05-01
title: Set-PlatformSecurityGroup
---

# Set-PlatformSecurityGroup

## SYNOPSIS

Creates or updates the Microsoft Entra security groups declared in platform-config.jsonc, and
assigns their RBAC roles against the resource groups from phase 1.

## SYNTAX

### __AllParameterSets

```
Set-PlatformSecurityGroup [[-GlobalConfigPath] <string>] [[-PlatformConfigPath] <string>]
 [-AsHashtable] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Phase 2 of the platform-config.jsonc provisioning workflow.
Reads the `securityGroups` array,
resolves `${placeholder}` tokens against global-config.jsonc, and creates any security group
(matched by `mailNickname`) that doesn't exist yet, or updates its display name/description if
they've drifted. A newly created group is not used until its principal is confirmed readable, to
absorb Microsoft Entra's directory propagation delay. Each group's `roleAssignments` are then
resolved against the resource groups created in phase 1 and any role the group doesn't already
hold is assigned; existing role assignments outside this list are never removed.

Every `resourceGroupId` referenced by a role assignment must already exist, so
`Set-PlatformResourceGroup` must be run first. A role assignment can instead set `scope` to
`subscription` to assign at the subscription scope rather than a single resource group.

This cmdlet supports `-WhatIf`/`-Confirm` and requires the `Az.Resources` module, signed in via
`Connect-AzAccount` with permission to manage Microsoft Entra security groups and assign RBAC
roles. The active Az context tenant and subscription must match the `tenantId` and `subscriptionId`
in global-config.jsonc before processing begins.

By default, each security group's result is emitted once processing finishes; under
`-WhatIf`, `Action` reports actions that would happen instead of applying a mutation. Pass
`-AsHashtable` to collect every result and get a single summary object instead.

## EXAMPLES

### Example 1 - Sync security groups using the default config paths

```powershell
Set-PlatformSecurityGroup
```

Creates or updates the security groups declared in `config/global-config.jsonc` and
`config/platform-config.jsonc`, and assigns their configured RBAC roles.

### Example 2 - Preview changes without applying them

```powershell
Set-PlatformSecurityGroup -GlobalConfigPath ./config/global-config.jsonc -PlatformConfigPath ./config/platform-config.jsonc -WhatIf
```

## PARAMETERS

### -AsHashtable

Collects every result in memory instead of streaming it, and emits a single `Hashtable` at the end
with an `IsWhatIf` key and a `SecurityGroups` key holding the list of
`PlatformSecurityGroupActionResult` objects.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
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

Path to the platform-config.jsonc file whose `securityGroups` array is provisioned.
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

### MSCKite.Azure.Platform.Models.PlatformSecurityGroupActionResult

Emitted once processing finishes (the default). Has the
DisplayName, MailNickname, ObjectId, and Action of the security group, and the Role, Scope, and
Action of every role assignment synced. Group actions are `Created`, `Updated`, or `Unchanged`;
`-WhatIf` returns `WouldCreate` or `WouldUpdate`. Role actions are `Added` or `Unchanged`;
`-WhatIf` returns `WouldAdd` for an intended assignment.

### System.Collections.Hashtable

Emitted once at the end instead, only when `-AsHashtable` is passed. Has an
`IsWhatIf` key, a `Count` key, and a `SecurityGroups` key holding the list of
`PlatformSecurityGroupActionResult` objects collected during the run.

## NOTES

This cmdlet requires the `Az.Resources` module and an active Azure sign-in
(`Connect-AzAccount`) with permission to create Microsoft Entra security groups and assign RBAC
roles on the target resource groups. Role assignment creation automatically retries to absorb the
propagation delay between a newly created group and RBAC accepting its principal.

## RELATED LINKS

- [Set-PlatformResourceGroup](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/Set-PlatformResourceGroup.md)
- [Set-PlatformEnvironmentIdentity](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/Set-PlatformEnvironmentIdentity.md)
