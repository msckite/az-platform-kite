---
document type: cmdlet
external help file: MSCKite.Azure.Platform.dll-Help.xml
HelpUri: ''
Locale: en-NL
Module Name: MSCKite.Azure.Platform
ms.date: 09/08/2026
PlatyPS schema version: 2024-05-01
title: Disconnect-PlatformContext
---

# Disconnect-PlatformContext

## SYNOPSIS

Signs out of every currently signed-in context: Azure, Azure DevOps, and GitHub.

## SYNTAX

### __AllParameterSets

```
Disconnect-PlatformContext [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Checks the sign-in status of Azure, Azure DevOps, and GitHub, and signs out of each context that
is currently signed in. Contexts that are already signed out are left untouched. Requires the
Az.Accounts module version 5.5 or higher.

## EXAMPLES

### Example 1 - Sign out of all connected platform contexts

```powershell
Disconnect-PlatformContext
```

## PARAMETERS

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

### System.String

A JSON string summarizing which contexts were signed out, or a message stating everything was
already signed out.

## NOTES

This cmdlet requires the Az.Accounts module version 5.5 or higher.

## RELATED LINKS
