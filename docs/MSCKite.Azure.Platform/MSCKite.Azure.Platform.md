---
document type: module
Help Version: 1.0.0.0
HelpInfoUri: ''
Locale: en-NL
Module Guid: 3d8f6c2e-9b4a-4c1d-8e2f-6a7b5c9d0e1f
Module Name: MSCKite.Azure.Platform
ms.date: 09/15/2026
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

### [Get-PlatformTemplate](Get-PlatformTemplate.md)

Downloads one or more folders (with their subfolders and files) from the Azure Platform Kite repository into a local folder.

### [New-PlatformConfigStructure](New-PlatformConfigStructure.md)

Scaffolds the platform configuration folder, creating a `global-config.jsonc` file either from a default template or copied from an existing input folder.

### [New-PlatformWorkflow](New-PlatformWorkflow.md)

Copies the GitHub Actions workflow templates for the configured branch strategy into the repository.

### [New-RandomPassword](New-RandomPassword.md)

Generates cryptographically secure random passwords with configurable complexity, length, and output formats.

### [New-RandomUniqueId](New-RandomUniqueId.md)

Generates a simple random alphanumeric identifier for non-security use cases.

### [Set-AdoDefault](Set-AdoDefault.md)

Sets the default Azure DevOps organization and project used by other Azure DevOps commands.

### [Set-GitHubDefault](Set-GitHubDefault.md)

Sets the default GitHub owner and repository used by other GitHub commands.

### [Set-GitHubLabels](Set-GitHubLabels.md)

Creates, updates, and removes GitHub repository labels to match a labels definition file.

### [Set-PlatformEnvironmentIdentity](Set-PlatformEnvironmentIdentity.md)

Creates the federated user-assigned managed identity for each environment declared in platform-config.jsonc, and assigns its RBAC roles against the resource groups from phase 1.

### [Set-PlatformGitHubEnvironment](Set-PlatformGitHubEnvironment.md)

Creates or updates each environment's GitHub deployment environment declared in platform-config.jsonc, including its protection rules, secrets, and variables.

### [Set-PlatformResourceGroup](Set-PlatformResourceGroup.md)

Creates or updates the Azure resource groups declared in platform-config.jsonc.

### [Set-PlatformSecurityGroup](Set-PlatformSecurityGroup.md)

Creates or updates the Microsoft Entra security groups declared in platform-config.jsonc, and assigns their RBAC roles against the resource groups from phase 1.

### [Test-PlatformTemplate](Test-PlatformTemplate.md)

Reports template and schema version compatibility and whether a newer template is available.

