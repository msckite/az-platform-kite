---
document type: cmdlet
external help file: MSCKite.Azure.Platform.dll-Help.xml
HelpUri: ''
Locale: en-NL
Module Name: MSCKite.Azure.Platform
ms.date: 09/08/2026
PlatyPS schema version: 2024-05-01
title: Disconnect-AdoOrganization
---

# Disconnect-AdoOrganization

## SYNOPSIS

Disconnects the current Azure DevOps session established by Connect-AdoOrganization.

## SYNTAX

### __AllParameterSets

```
Disconnect-AdoOrganization [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Clears the current Azure DevOps session state. If no session is connected, the cmdlet has no
effect.

## EXAMPLES

### Example 1 - Disconnect from Azure DevOps

```powershell
Disconnect-AdoOrganization
```

## PARAMETERS

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

### MSCKite.Azure.Platform.Models.DevOpsContext

The context that was disconnected, or `$null` if the session was already disconnected.

## NOTES

## RELATED LINKS
