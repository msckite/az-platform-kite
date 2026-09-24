---
document type: cmdlet
external help file: MSCKite.Azure.Platform.dll-Help.xml
HelpUri: ''
Locale: en-NL
Module Name: MSCKite.Azure.Platform
ms.date: 09/23/2026
PlatyPS schema version: 2024-05-01
title: Get-AdoDefault
---

# Get-AdoDefault

## SYNOPSIS

Gets the default Azure DevOps organization and project previously stored with Set-AdoDefault.

## SYNTAX

### __AllParameterSets

```
Get-AdoDefault [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Returns the Organization, Project, and CollectionUri currently stored for use by other Azure
DevOps commands. If no defaults have been set, the returned values are empty.

## EXAMPLES

### Example 1 - Get the current Azure DevOps defaults

```powershell
Get-AdoDefault
```

## PARAMETERS

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

### MSCKite.Azure.Platform.Models.AdoDefaults

The Organization, Project, and CollectionUri currently stored via Set-AdoDefault.

## NOTES

## RELATED LINKS

- [Set-AdoDefault](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/Set-AdoDefault.md)
- [Connect-AdoOrganization](https://github.com/msckite/az-platform-kite/blob/main/docs/MSCKite.Azure.Platform/Connect-AdoOrganization.md)
