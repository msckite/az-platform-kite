---
document type: cmdlet
external help file: MSCKite.Azure.Platform.dll-Help.xml
HelpUri: ''
Locale: en-NL
Module Name: MSCKite.Azure.Platform
ms.date: 09/08/2026
PlatyPS schema version: 2024-05-01
title: Set-AdoDefault
---

# Set-AdoDefault

## SYNOPSIS

Sets the default Azure DevOps organization and project used by other Azure DevOps commands.

## SYNTAX

### __AllParameterSets

```
Set-AdoDefault [[-Organization] <string>] [[-Project] <string>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Persists the Organization and Project as defaults for later Azure DevOps commands. Pass `$null`
(or omit a parameter) to clear that value.

## EXAMPLES

### Example 1 - Set the default organization and project

```powershell
Set-AdoDefault -Organization "myorg" -Project "My Project"
```

## PARAMETERS

### -Organization

The name of the Azure DevOps organization to use as the default. Pass `$null` to clear it.
The name of the Azure DevOps organization to use as the default.
Pass `$null` to clear it.

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

### -Project

The name of the Azure DevOps project to use as the default. Pass `$null` to clear it.
The name of the Azure DevOps project to use as the default.
Pass `$null` to clear it.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

You can pipe the organization or project name to this cmdlet by property name.

## OUTPUTS

### MSCKite.Azure.Platform.Models.AdoDefaults

The Organization, Project, and CollectionUri that were persisted.

## NOTES

## RELATED LINKS
