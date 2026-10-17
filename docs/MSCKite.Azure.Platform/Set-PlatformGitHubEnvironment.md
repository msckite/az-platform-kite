---
document type: cmdlet
external help file: MSCKite.Azure.Platform.dll-Help.xml
HelpUri: ''
Locale: en-NL
Module Name: MSCKite.Azure.Platform
ms.date: 10/16/2026
PlatyPS schema version: 2024-05-01
title: Set-PlatformGitHubEnvironment
---

# Set-PlatformGitHubEnvironment

## SYNOPSIS

Creates or updates each environment's GitHub deployment environment declared in
environments.jsonc, including its protection rules, secrets, and variables.

## SYNTAX

### __AllParameterSets

```
Set-PlatformGitHubEnvironment [[-GlobalConfigPath] <string>] [[-ResourceGroupsConfigPath] <string>]
 [[-EnvironmentsConfigPath] <string>] [-AsHashtable] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Phase 4 (final) of the platform provisioning workflow.
For each pipeline of each entry in the
`environments` array, resolves `${clientId}` by reading the identity created in phase 3, then
creates or updates the GitHub deployment environment's protection rules (wait timer and required
reviewers, resolved to their live GitHub user or team id), sets its `secrets` (always re-applied,
since GitHub never exposes secret values so they can't be diffed), and syncs its `variables`
(only values that have actually drifted are re-applied).

The identity referenced by each environment must already exist, so
`Set-PlatformEnvironmentIdentity` must be run first.

Each environment declares its GitHub environments in a `pipelines` array, where every entry pairs
a `githubEnvironment` with the `userAssignedIdentity` it receives the client id from. This cmdlet
creates or updates the GitHub environment of every pipeline, for example an `infra` and a
`workload` one, in the same run, each resolving `${clientId}` from its own identity so their
secrets never collide, and reports the pipeline's `purpose` as `Purpose`.

`protectionRules.deploymentBranchPolicy` restricts which branches and tags may deploy to the GitHub
environment, for example `{ "branches": ["main"], "tags": ["infra/*"] }`. GitHub enforces this before
a job receives the environment's secrets or its OIDC token, so a workflow on any other branch cannot
sign in as that environment's identity. The environment is switched to "selected branches and tags",
missing patterns are added, and patterns no longer listed are removed; `DeploymentBranchPolicies` in
the result reports each pattern as `branch:<pattern>` or `tag:<pattern>`. A published release runs
on its tag, so an environment deployed from a release workflow needs that tag prefix listed. Omit
`deploymentBranchPolicy` to leave the environment's branch policy unmanaged. Like required reviewers,
branch policies on private repositories need a GitHub plan that supports them.

This cmdlet supports `-WhatIf`/`-Confirm` and requires the `Az.Resources` and
`Az.ManagedServiceIdentity` modules, signed in via `Connect-AzAccount`, and the GitHub CLI (`gh`)
signed in with permission to manage environments, secrets, and variables on the target repository.
The active Az context tenant and subscription must match the `tenantId` and `subscriptionId` in
global-config.jsonc before processing begins.
By default, each environment's result is emitted once processing finishes; under
`-WhatIf`, `Action` reports actions that would happen instead of applying a mutation. Pass `-AsHashtable` to
collect every result and get a single summary object instead.

## EXAMPLES

### Example 1 - Sync GitHub environments using the default config paths

```powershell
Set-PlatformGitHubEnvironment
```

Creates or updates the GitHub deployment environment, secrets, and variables for every environment
declared in `config/global-config.jsonc` and `config/platform/resource-groups.jsonc` and `config/platform/environments.jsonc`.

### Example 2 - Preview changes without applying them

```powershell
Set-PlatformGitHubEnvironment -GlobalConfigPath ./config/global-config.jsonc -ResourceGroupsConfigPath ./config/platform/resource-groups.jsonc -EnvironmentsConfigPath ./config/platform/environments.jsonc -WhatIf
```

## PARAMETERS

### -AsHashtable

Collects every result in memory instead of streaming it, and emits a single `Hashtable` at the end
with an `IsWhatIf` key and an `Environments` key holding the list of
`PlatformGitHubEnvironmentActionResult` objects.

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

### -EnvironmentsConfigPath

Path to the environments.jsonc file whose `environments` array is provisioned.
Defaults to `config/platform/environments.jsonc`.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 2
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -GlobalConfigPath

Path to the global-config.jsonc file whose values resolve `${placeholder}` tokens in
the platform config files.
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

### -ResourceGroupsConfigPath

Path to the resource-groups.jsonc file whose `resourceGroups` array is provisioned or, in later phases, used to resolve `resourceGroupId` references.
Defaults to `config/platform/resource-groups.jsonc`.

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

### MSCKite.Azure.Platform.Models.PlatformGitHubEnvironmentActionResult

Emitted once processing finishes (the default). Has the
EnvironmentCode, Name, and Action of the GitHub environment, and the Name and Action of every
secret and variable synced. Environment actions are `Created` or `Updated`; `-WhatIf` returns
`WouldCreate` or `WouldUpdate`. Secret and variable actions are `Created`, `Updated`, or
`Unchanged`; `-WhatIf` returns `WouldCreate` or `WouldUpdate`.

### System.Collections.Hashtable

Emitted once at the end instead, only when `-AsHashtable` is passed. Has an
`IsWhatIf` key, a `Count` key, and an `Environments` key holding the list of
`PlatformGitHubEnvironmentActionResult` objects collected during the run.

## NOTES

This cmdlet requires the `Az.Resources` and `Az.ManagedServiceIdentity` modules, an active Azure
sign-in (`Connect-AzAccount`) to read the identity created by `Set-PlatformEnvironmentIdentity`,
and the GitHub CLI (`gh`) signed in with permission to manage environments, secrets, and variables
on the target repository.

## RELATED LINKS

- [Set-PlatformEnvironmentIdentity](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/Set-PlatformEnvironmentIdentity.md)
