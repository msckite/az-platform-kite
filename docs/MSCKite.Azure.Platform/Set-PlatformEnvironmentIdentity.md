---
document type: cmdlet
external help file: MSCKite.Azure.Platform.dll-Help.xml
HelpUri: ''
Locale: en-NL
Module Name: MSCKite.Azure.Platform
ms.date: 09/23/2026
PlatyPS schema version: 2024-05-01
title: Set-PlatformEnvironmentIdentity
---

# Set-PlatformEnvironmentIdentity

## SYNOPSIS

Creates the federated user-assigned managed identity for each environment declared in
platform-config.jsonc, and assigns its RBAC roles against the resource groups from phase 1.

## SYNTAX

### __AllParameterSets

```
Set-PlatformEnvironmentIdentity [[-GlobalConfigPath] <string>] [[-PlatformConfigPath] <string>]
 [-AsHashtable] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Phase 3 of the platform-config.jsonc provisioning workflow.
For each entry in the `environments`
array, resolves `${placeholder}` tokens (including the environment's own `${environmentCode}` and
`${tool}`) and creates the user-assigned managed identity in its resource group if it doesn't
exist yet, waiting for its principal to become readable before it's used further. It then creates
or updates the identity's federated credential using an immutable GitHub Actions OIDC subject
computed from the live repository (the owner's and repository's numeric ids, per GitHub's
immutable subject claim format), and finally assigns any role from `roleAssignments` that the
identity doesn't already hold; existing role assignments outside this list are never removed.

Every `resourceGroupId` referenced by a role assignment must already exist, so
`Set-PlatformResourceGroup` must be run first. Reading the live repository requires the GitHub CLI
(`gh`) to be installed and signed in.

This cmdlet supports `-WhatIf`/`-Confirm` and requires the `Az.Resources` and
`Az.ManagedServiceIdentity` modules, signed in via `Connect-AzAccount` with permission to create
managed identities, federated credentials, and RBAC role assignments. The active Az context
tenant and subscription must match the `tenantId` and `subscriptionId` in global-config.jsonc
before processing begins.

By default, each environment's result is emitted once processing finishes; under `-WhatIf`,
`Action` reports actions that would happen instead of applying a mutation. Pass `-AsHashtable` to collect
every result and get a single summary object instead.

## EXAMPLES

### Example 1 - Sync environment identities using the default config paths

```powershell
Set-PlatformEnvironmentIdentity
```

Creates or updates the identity, federated credential, and role assignments for every environment
declared in `config/global-config.jsonc` and `config/platform-config.jsonc`.

### Example 2 - Preview changes without applying them

```powershell
Set-PlatformEnvironmentIdentity -GlobalConfigPath ./config/global-config.jsonc -PlatformConfigPath ./config/platform-config.jsonc -WhatIf
```

## PARAMETERS

### -AsHashtable

Collects every result in memory instead of streaming it, and emits a single `Hashtable` at the end
with an `IsWhatIf` key and an `Environments` key holding the list of
`PlatformEnvironmentIdentityActionResult` objects.

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

Path to the platform-config.jsonc file whose `environments` array is provisioned.
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

### MSCKite.Azure.Platform.Models.PlatformEnvironmentIdentityActionResult

Emitted once processing finishes (the default). Has the
EnvironmentCode, IdentityName, PrincipalId, ClientId, and Action of the identity; the
FederatedCredentialAction; and the Role, Scope, and Action of every role assignment synced.
Identity actions are `Created` or `Unchanged`; `-WhatIf` returns `WouldCreate`. Federated
credential actions are `Created`, `Updated`, or `Unchanged`; `-WhatIf` returns `WouldCreate` or
`WouldUpdate`. Role actions are `Added` or `Unchanged`; `-WhatIf` returns `WouldAdd` for an
intended assignment.

### System.Collections.Hashtable

Emitted once at the end instead, only when `-AsHashtable` is passed. Has an
`IsWhatIf` key, a `Count` key, and an `Environments` key holding the list of
`PlatformEnvironmentIdentityActionResult` objects collected during the run.

## NOTES

This cmdlet requires the `Az.Resources` and `Az.ManagedServiceIdentity` modules, an active Azure
sign-in (`Connect-AzAccount`) with permission to create managed identities and federated
credentials and assign RBAC roles, and the GitHub CLI (`gh`) signed in with read access to the
repository configured in `sourceControl`.

## RELATED LINKS

- [Set-PlatformSecurityGroup](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/Set-PlatformSecurityGroup.md)
- [Set-PlatformGitHubEnvironment](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/Set-PlatformGitHubEnvironment.md)
