---
document type: module
Help Version: 1.0.0.0
HelpInfoUri: ''
Locale: en-NL
Module Guid: 3d8f6c2e-9b4a-4c1d-8e2f-6a7b5c9d0e1f
Module Name: MSCKite.Azure.Platform
ms.date: 09/08/2026
PlatyPS schema version: 2024-05-01
title: MSCKite.Azure.Platform Module
---

# MSCKite.Azure.Platform Module

## Description

Lightweight automation for Azure platform engineering, developer enablement, and cloud operations.

## MSCKite.Azure.Platform Cmdlets

### [Connect-AdoOrganization](Connect-AdoOrganization.md)

Connects to an Azure DevOps organization and optionally a project within it.

### [Disconnect-AdoOrganization](Disconnect-AdoOrganization.md)

Disconnects the current Azure DevOps session established by Connect-AdoOrganization.

### [Disconnect-PlatformContext](Disconnect-PlatformContext.md)

Signs out of every currently signed-in context: Azure, Azure DevOps, and GitHub.

### [Get-AdoDefault](Get-AdoDefault.md)

Gets the default Azure DevOps organization and project previously stored with Set-AdoDefault.

### [Get-GitHubDefault](Get-GitHubDefault.md)

Gets the default GitHub owner and repository previously stored with Set-GitHubDefault.

### [Get-PlatformContext](Get-PlatformContext.md)

Gets the combined sign-in status for Azure, Azure DevOps, and GitHub as a single JSON object.

### [Set-AdoDefault](Set-AdoDefault.md)

Sets the default Azure DevOps organization and project used by other Azure DevOps commands.

### [Set-GitHubDefault](Set-GitHubDefault.md)

Sets the default GitHub owner and repository used by other GitHub commands.

