---
document type: cmdlet
external help file: MSCKite.Azure.Platform.dll-Help.xml
HelpUri: ''
Locale: en-NL
Module Name: MSCKite.Azure.Platform
ms.date: 11/25/2026
PlatyPS schema version: 2024-05-01
title: Grant-PlatformGraphPermission
---

# Grant-PlatformGraphPermission

## SYNOPSIS

Grants a pipeline identity the Microsoft Graph application permissions it needs to read or manage
Entra groups.

## SYNTAX

### __AllParameterSets

```
Grant-PlatformGraphPermission [-PrincipalId] <string> [[-Permission] <string[]>] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

A one-time setup step for the platform workflows. When `Set-PlatformSecurityGroup` runs in GitHub
Actions, it signs in as the identity of the `platform` or `platform-plan` environment and reads
Entra groups through Microsoft Graph. That identity needs the Microsoft Graph application permission
`Group.Read.All`, and also `Group.ReadWrite.All` if the pipeline should create and change groups.
This cmdlet grants those permissions to the identity's service principal.

Run it by hand, after `Set-PlatformEnvironmentIdentity` (or `Set-Platform`) has created the
identity. Granting Microsoft Graph application permissions takes the Application Administrator,
Privileged Role Administrator, or Global Administrator directory role, and a pipeline identity
should never hold that role. That is why the phase cmdlets never call this cmdlet.

The cmdlet connects to Microsoft Graph with `Connect-MgGraph` and the scopes
`AppRoleAssignment.ReadWrite.All` and `Application.Read.All`, which opens a sign-in for an account
with one of the directory roles above. For each permission it looks up the app role on the
Microsoft Graph service principal and grants it, unless the identity already has it. Running it
again is safe.

This cmdlet has a high confirmation impact, so it asks for confirmation before each grant unless
you pass `-Confirm:$false`. Under `-WhatIf`, or when you decline the prompt, a permission that would
be granted is reported as `WouldGrant` and nothing is changed.

## EXAMPLES

### Example 1 - Let the platform identity read Entra groups

```powershell
Grant-PlatformGraphPermission -PrincipalId '00000000-0000-0000-0000-000000000000'
```

Grants the default permission, `Group.Read.All`, to the service principal with that object id. Use
the `PrincipalId` that `Set-PlatformEnvironmentIdentity` reports for the `platform` environment.

### Example 2 - Let the platform identity also create and change groups

```powershell
Grant-PlatformGraphPermission -PrincipalId '00000000-0000-0000-0000-000000000000' -Permission 'Group.Read.All', 'Group.ReadWrite.All'
```

Add `Group.ReadWrite.All` only when the pipeline should create and change groups. Give the
`platform-plan` identity `Group.Read.All` only.

### Example 3 - Grant the permission to the identities straight from Set-Platform

```powershell
(Set-Platform -WhatIf).EnvironmentIdentities |
    Where-Object EnvironmentCode -EQ 'platform' |
    Grant-PlatformGraphPermission
```

`Set-Platform -WhatIf` changes nothing and reports every existing identity with its `PrincipalId`,
which is piped to this cmdlet by property name. Both identities of the `platform` environment, for
`platform` and `platform-plan`, get `Group.Read.All`.

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

### -Permission

The Microsoft Graph application permissions to grant, by name, for example `Group.Read.All`.
Defaults to `Group.Read.All`. Add `Group.ReadWrite.All` only when the pipeline should create and
change groups.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -PrincipalId

The object id of the identity's service principal in Entra ID: the `PrincipalId` that
`Set-PlatformEnvironmentIdentity` and `Set-Platform` report for the identity. It is not the client
id.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
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

You can pipe an object with a `PrincipalId` property to this cmdlet, such as the identity results of
`Set-PlatformEnvironmentIdentity` or `Set-Platform`.

## OUTPUTS

### MSCKite.Azure.Platform.Models.PlatformGraphPermissionGrantResult

Emitted once per principal. Has the `PrincipalId` and a `Permissions` list with the `Permission`
and `Action` of each permission: `Granted`; `Unchanged` when the identity already had it; or
`WouldGrant` under `-WhatIf` or when you declined the confirmation prompt. A permission that could
not be resolved or granted is reported as an error and left out of the list.

## NOTES

This cmdlet requires the `Microsoft.Graph.Applications` module and an account with the
Application Administrator, Privileged Role Administrator, or Global Administrator directory role.
It does not use your Azure (`Connect-AzAccount`) sign-in.

See the one-time setup in
[Platform flow](https://github.com/msckite/az-platform-kite/blob/main/docs/consumers/workflows/platform-flow.md#one-time-setup)
for where this step fits.

## RELATED LINKS

- [Set-PlatformEnvironmentIdentity](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/Set-PlatformEnvironmentIdentity.md)
- [Set-PlatformSecurityGroup](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/Set-PlatformSecurityGroup.md)
- [Set-Platform](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/Set-Platform.md)
