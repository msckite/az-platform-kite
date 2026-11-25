---
document type: cmdlet
external help file: MSCKite.Azure.Platform.dll-Help.xml
HelpUri: ''
Locale: en-NL
Module Name: MSCKite.Azure.Platform
ms.date: 11/25/2026
PlatyPS schema version: 2024-05-01
title: Set-Platform
---

# Set-Platform

## SYNOPSIS

Runs the four platform phases in order with one command, handing each phase's results to the next.

## SYNTAX

### __AllParameterSets

```
Set-Platform [[-GlobalConfigPath] <string>] [[-PlatformConfigFolder] <string>] [-WhatIf] [-Confirm]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Runs `Set-PlatformResourceGroup`, `Set-PlatformSecurityGroup`, `Set-PlatformEnvironmentIdentity`
and `Set-PlatformGitHubEnvironment` in that order, from the config set in global-config.jsonc and
the `resource-groups.jsonc`, `security-groups.jsonc` and `environments.jsonc` files in
`-PlatformConfigFolder`. Each phase does exactly what its own cmdlet does.

Run on their own, the later phases read back from Azure what the earlier phases created. This
cmdlet hands the results over in memory instead: the resource groups from phase 1 to phases 2 to 4,
and the client id of each identity from phase 3 to phase 4. That makes `-WhatIf` work on a fresh
subscription, where nothing exists yet: a resource group that would be created gets the id it will
have, and `${clientId}` of an identity that would be created resolves to `(known after apply)`.

Before any phase runs, the cmdlet loads all four config files, checks that the active Az context
matches the `tenantId` and `subscriptionId` in global-config.jsonc, and reads the repository named
by `sourceControl` from GitHub. A missing file, a wrong Azure context or a missing GitHub sign-in
therefore stops the run before anything is changed.

When a phase reports errors, the phases that depend on it don't run: errors in phase 1 (resource
groups) or phase 3 (environment identities) stop the run with a warning. Phases 3 and 4 don't need
the security groups, so errors in phase 2 are reported and the run continues. Errors in phase 4 are
reported at the end.

This cmdlet supports `-WhatIf`/`-Confirm` and needs the permissions of all four phase cmdlets: the
`Az.Resources` and `Az.ManagedServiceIdentity` modules, an Azure sign-in (`Connect-AzAccount`) that
can create resource groups, Entra groups, identities and role assignments, and a GitHub CLI
sign-in (`gh auth login`) that can manage environments, secrets and variables on the repository.

## EXAMPLES

### Example 1 - Deploy the platform using the default config paths

```powershell
Set-Platform
```

Runs all four phases from `config/global-config.jsonc` and the files in `config/platform/`.

### Example 2 - Preview the whole deployment

```powershell
Set-Platform -WhatIf -Verbose
```

Shows what each phase would change without changing anything, including on a subscription where
nothing has been deployed yet. `-Verbose` adds a `Phase n of 4` line per phase and every check the
phases make.

### Example 3 - Check whether a run stopped early

```powershell
$result = Set-Platform -GlobalConfigPath ./config/global-config.jsonc -PlatformConfigFolder ./config/platform
$result.StoppedAtPhase
$result.PhasesWithErrors
```

`StoppedAtPhase` is empty when every phase ran. `PhasesWithErrors` lists each phase that reported
errors, including phases after which the run continued.

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

### -PlatformConfigFolder

Folder holding `resource-groups.jsonc`, `security-groups.jsonc` and `environments.jsonc`.
Defaults to `config/platform`.

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

You can pipe the global config path or platform config folder to this cmdlet by property name.

## OUTPUTS

### MSCKite.Azure.Platform.Models.PlatformDeploymentResult

Emitted once, when the run finishes or stops. Has `IsWhatIf`; `StoppedAtPhase`, the phase whose
errors stopped the run (`ResourceGroups` or `EnvironmentIdentities`), empty when every phase ran;
`PhasesWithErrors`, every phase that reported errors; and the results of each phase in
`ResourceGroups`, `SecurityGroups`, `EnvironmentIdentities` and `GitHubEnvironments`, as the phase
cmdlets return them. A phase that didn't run has an empty list.

## NOTES

The platform workflow templates keep running the four phase cmdlets as separate jobs, so phases 2
and 3 run side by side and each job is its own deployment gate. Use `Set-Platform` to run the
phases from your own computer.

Granting Microsoft Graph permissions (`Grant-PlatformGraphPermission`) and adding
`PLATFORM_GITHUB_TOKEN` to the platform environments stay manual steps.

## RELATED LINKS

- [Set-PlatformResourceGroup](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/Set-PlatformResourceGroup.md)
- [Set-PlatformSecurityGroup](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/Set-PlatformSecurityGroup.md)
- [Set-PlatformEnvironmentIdentity](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/Set-PlatformEnvironmentIdentity.md)
- [Set-PlatformGitHubEnvironment](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/Set-PlatformGitHubEnvironment.md)
