---
document type: cmdlet
external help file: MSCKite.Azure.Platform.dll-Help.xml
HelpUri: ''
Locale: en-NL
Module Name: MSCKite.Azure.Platform
ms.date: 09/08/2026
PlatyPS schema version: 2024-05-01
title: Get-GitHubDefault
---

# Get-GitHubDefault

## SYNOPSIS

Gets the default GitHub owner and repository previously stored with Set-GitHubDefault.

## SYNTAX

### __AllParameterSets

```
Get-GitHubDefault [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Returns the Owner, Repository, and RepositoryUri currently stored for use by other GitHub
commands. If no defaults have been set, the returned values are empty.

## EXAMPLES

### Example 1 - Get the current GitHub defaults

```powershell
Get-GitHubDefault
```

## PARAMETERS

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

### MSCKite.Azure.Platform.Models.GitHubDefaults

The Owner, Repository, and RepositoryUri currently stored via Set-GitHubDefault.

## NOTES

## RELATED LINKS
