---
document type: cmdlet
external help file: MSCKite.Azure.Platform.dll-Help.xml
HelpUri: ''
Locale: en-NL
Module Name: MSCKite.Azure.Platform
ms.date: 09/08/2026
PlatyPS schema version: 2024-05-01
title: Get-PlatformContext
---

# Get-PlatformContext

## SYNOPSIS

Gets the combined sign-in status for Azure, Azure DevOps, and GitHub as a single JSON object.

## SYNTAX

### __AllParameterSets

```
Get-PlatformContext [-All] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Combines the Azure context, the Azure DevOps REST API auth status, and the local GitHub CLI auth
status into a single JSON object. By default, contexts that are not signed in are omitted.
Requires the Az.Accounts module version 5.5 or higher.

## EXAMPLES

### Example 1 - Get the status of all signed-in contexts

```powershell
Get-PlatformContext
```

### Example 2 - Get the status of every context, including those not signed in

```powershell
Get-PlatformContext -All
```

## PARAMETERS

### -All

Includes contexts that are not signed in, with the reason surfaced as a warning, instead of
omitting them.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

### System.String

A JSON string combining the Azure, Azure DevOps, and GitHub contexts. Nothing is written if no
context qualifies.

## NOTES

This cmdlet requires the Az.Accounts module version 5.5 or higher.

## RELATED LINKS
