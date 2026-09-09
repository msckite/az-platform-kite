<!-- omit from toc -->
# Azure Platform Kite

[![github-latest](https://img.shields.io/github/v/release/msckite/az-platform-kite?include_prereleases&color=blue&logo=github&label=release)](https://github.com/msckite/az-platform-kite/releases)
[![ps-gallery-downloads](https://img.shields.io/powershellgallery/dt/MSCKite.Azure.Platform.svg)](https://www.powershellgallery.com/packages/MSCKite.Azure.Platform)
[![github-issues](https://img.shields.io/github/issues/msckite/az-platform-kite?logo=github)](https://github.com/msckite/az-platform-kite/issues)

</br>

<!-- markdownlint-disable-next-line MD033 -->
<p><img src=".assets/msckite-logo-s.png" alt="Logo" width="auto" height="80"></p>

By **MSCKite™**  
_Lighter Work. Higher Impact._

</br>

> [!IMPORTANT]
> This module is intended primarily for personal use and is under active development.

<!-- omit from toc -->
## Table of Contents

- [What's this repo about?](#whats-this-repo-about)
- [What do you get?](#what-do-you-get)
- [Why use Kite?](#why-use-kite)
- [How to use it?](#how-to-use-it)
- [License](#license)

## What's this repo about?

**Azure Platform Kite** is a lightweight automation toolkit designed to simplify Azure platform engineering through reusable PowerShell automation.

Inspired by the agility and freedom of a kite riding the wind, the toolkit helps platform engineers and cloud architects build, manage, and govern Azure environments with less effort and greater consistency. It brings together common operational tasks into a single, automation-first experience, enabling teams to focus on delivering value instead of managing repetitive platform activities.

## What do you get?

Built on Infrastructure as Code and platform engineering principles, Azure Platform Kite provides reusable capabilities for resource provisioning, repository management, documentation generation, developer onboarding, and environment standardization. By codifying platform operations, it helps reduce manual effort, improve governance, and accelerate the delivery of secure and compliant developer platforms.

<!-- omit from toc -->
### Key Capabilities

- Generate documentation for Bicep and PowerShell projects
- Provision Azure and Microsoft Entra resources from configuration files
- Create and configure Azure DevOps and GitHub repositories
- Automate developer onboarding and workspace provisioning
- Standardize platform operations through reusable workflows
- Support Internal Developer Platform (IDP) and self-service initiatives
- Promote consistency, governance, and automation across Azure environments

> [!NOTE]
> Although this toolkit is designed with Azure DevOps integration in mind, the first release focuses on Azure with GitHub integration. Azure DevOps commands are planned for a later major release. Until then, you can still use the easy `Get-*` and `Remove-PlatformContext` commands to inspect and clear your platform context.

## Why use Kite?

> _Just as a kite harnesses the power of the wind to move efficiently with minimal effort, Azure Platform Kite helps engineers harness the power of the cloud through automation, standardization, and reusable building blocks._

Whether you are deploying a new platform environment, creating project repositories, generating documentation, or enabling self-service capabilities for development teams, the toolkit provides a consistent and repeatable approach to platform operations.

<!-- omit from toc -->
### Built with AI, Designed for Automation

Azure Platform Kite is built and maintained with AI assistance, but designed to reduce dependency on AI for routine operational tasks. Knowledge and workflows are captured in reusable automation so that common platform activities can be executed consistently, reliably, and without repeatedly spending time or tokens on the same questions.

<!-- ## The Kite Philosophy

AI is an excellent co-pilot, but repetitive operational work should not require a conversation every time.

Azure Platform Kite transforms knowledge, experience, and AI-assisted engineering into reusable automation that can be executed on demand. The goal is simple: use AI to create better tools, then use those tools to work faster, more consistently, and with less friction. -->

## How to use it?

<!-- omit from toc -->
### Quickstart

<!-- omit from toc -->
#### 1. Install from the PowerShell Gallery

```powershell
Install-Module -Name 'MSCKite.Azure.Platform' -Scope CurrentUser
```

<!-- omit from toc -->
#### 2. Log in to each platform you'll be working with

```powershell
Connect-AzAccount       # Azure
Connect-AdoOrganization # Azure DevOps
gh auth login           # GitHub
```

<!-- omit from toc -->
#### 3. Verify you're connected to the right platforms

```powershell
Get-PlatformContext
```

`Get-PlatformContext` combines your _Azure_, _Azure DevOps_, and _GitHub_ connectivity state into a single object, so you can confirm everything lines up before running deployments across platforms.

<!-- omit from toc -->
#### Sample json output

```json
{
  "Azure": {
    "Account": "user@contoso.com",
    "Tenant": "00000000-0000-0000-0000-000000000000",
    "SubscriptionId": "00000000-0000-0000-0000-000000000000",
    "SubscriptionName": "sub-contoso-workloads",
    "Environment": "AzureCloud",
    "IsSignedIn": true
  },
  "DevOps": {
    "Account": "user@contoso.com",
    "Organization": "contoso",
    "CollectionUri": "https://dev.azure.com/contoso",
    "IsSignedIn": true
  },
  "GitHub": {
    "Account": "github-user",
    "Host": "github.com",
    "Protocol": "https",
    "TokenScopes": "'gist', 'read:org', 'repo', 'workflow'",
    "IsSignedIn": true
  }
}
```

<!-- omit from toc -->
#### 4. (Optional) Set the default GitHub _Owner_ and _Repository_

If you're about to run several GitHub commands, set a default owner/repository so you don't have to pass them on every call.

```powershell
Set-GitHubDefault -Owner 'msckite' -Repository 'az-platform-kite'
```

Use `Get-GitHubDefault` at any time to see what's currently stored:

```powershell
Get-GitHubDefault
```

```text
Owner   Repository       RepositoryUri
-----   ----------       -------------
msckite az-platform-kite https://github.com/msckite/az-platform-kite
```

Reset the defaults by passing `$null`:

```powershell
Set-GitHubDefault $null
```

> [!TIP]
> The same pattern applies to Azure DevOps, using `Set-AdoDefault` and `Get-AdoDefault` to manage your default organization/project instead.

## License

Copyright (c) 2026 Martin Swinkels

All Rights Reserved

<!-- omit from toc -->
## Disclaimer

This repository is provided "**as is**" and is subject to **limited support**. While reasonable efforts
have been made to ensure its usefulness, there are **no warranties or guarantees** regarding accuracy,
reliability, security, or ongoing maintenance. By using this code, you acknowledge and agree that you
do so at your own risk. It is your responsibility to validate, test, and ensure suitability for your specific
use case, particularly in production environments. We welcome community contributions and feedback to improve
the project; however, official support will limited.

<!-- omit from toc -->
## Liability

Under no circumstances shall the authors, contributors, or affiliated organizations be held liable for
any direct, indirect, incidental, or consequential damages arising from the use of this repository, including
but not limited to loss of data, business interruption, or system failures.
Use of this code implies acceptance of these terms.

<!-- omit from toc -->
## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft
trademarks or logos is subject to and must follow
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft
sponsorship. Any use of third-party trademarks or logos are subject to those third-party's policies.

</br>

<!-- markdownlint-disable-next-line MD033 -->
<p><img src=".assets/msckite-line.png" alt="Logo" width="auto" height="6"></p>

**MSCKite™**  
_Lighter Work. Higher Impact._
