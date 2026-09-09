---
document type: cmdlet
external help file: MSCKite.Azure.Platform.dll-Help.xml
HelpUri: ''
Locale: en-NL
Module Name: MSCKite.Azure.Platform
ms.date: 09/08/2026
PlatyPS schema version: 2024-05-01
title: Connect-AdoOrganization
---

# Connect-AdoOrganization

## SYNOPSIS

Connects to an Azure DevOps organization and optionally a project within it.

## SYNTAX

### __AllParameterSets

```
Connect-AdoOrganization [[-Organization] <string>] [[-Project] <string>] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Validates authentication against Azure DevOps and marks the session as connected so that other
Azure DevOps commands in this module can use it. Requires the Az.Accounts module version 5.5 or
higher.

## EXAMPLES

### Example 1 - Connect to an organization

```powershell
Connect-AdoOrganization -Organization "https://dev.azure.com/myorg"
```

### Example 2 - Connect to an organization and project

```powershell
Connect-AdoOrganization -Organization "https://dev.azure.com/myorg" -Project "My Project"
```

## PARAMETERS

### -Organization

The name of the Azure DevOps organization to connect to.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases:
- O
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

The name of the Azure DevOps project to connect to within the specified organization.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases:
- P
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

### MSCKite.Azure.Platform.Models.DevOpsContext

Represents the connected Azure DevOps organization and project.

## NOTES

This cmdlet requires the Az.Accounts module version 5.5 or higher.

## RELATED LINKS
