<!-- omit from toc -->
# Workflows

Kite comes with ready-to-copy GitHub Actions workflows. You install them into your own repository
with one command, and from then on they are yours: change them as you like. They are a starting
point, so the build, test and deploy steps for your application and infrastructure are placeholders
for you to fill in.

<!-- omit from toc -->
## Table of Contents

- [What you get](#what-you-get)
- [Choose a branch strategy](#choose-a-branch-strategy)
- [Install the workflows](#install-the-workflows)
- [Before the workflows can run](#before-the-workflows-can-run)
- [Where to read next](#where-to-read-next)

## What you get

The workflows come in three bundles. You can install any combination.

| Bundle | What it does | Runs when |
| --- | --- | --- |
| Platform | Keeps your platform (resource groups, security groups, identities and GitHub environments) in line with the files in `config/platform/`. | Config files change |
| Workload | Builds, tests and deploys your application code. | `src/` changes |
| Infra | Validates and deploys your Bicep code as a deployment stack. | `iac/` changes |

Workload and infra react to different folders, so a code change never redeploys your infrastructure
and an infrastructure change never redeploys your code. They also sign in with different identities
(see [Identities and environments](./identities-and-environments.md)).

## Choose a branch strategy

Workload and infra follow one of two ways of getting changes to production. You choose it with
`sourceControl.branchStrategy` in `config/global-config.jsonc`. Both bundles use the same strategy.
The platform bundle ignores it.

| Strategy | Value | How a change reaches production |
| --- | --- | --- |
| [GitHub Flow](./github-flow.md) | `github` | You merge a pull request to `main` and it deploys to `prd`. |
| [Release Flow](./release-flow.md) | `release` | A merge to `main` deploys to `stg`. A change reaches `prd` when you merge its release pull request. |

Pick GitHub Flow when you want the shortest path to production. Pick Release Flow when you want
every change to run in `stg` first and to decide yourself when it goes live.

## Install the workflows

Download the templates, then copy the bundles you need:

```powershell
Get-PlatformTemplate -IncludedFolders 'templates' -OutputFolder ./.tmp
New-PlatformWorkflow -WorkflowType all
```

`-WorkflowType` selects the bundles:

| Value | Installs |
| --- | --- |
| `platform` | Platform |
| `workload` | Workload |
| `infra` | Infra, plus a starter Bicep sample in `iac/res/sample/` |
| `both` (default) | Platform and workload |
| `all` | Platform, workload and infra |

The command reads your branch strategy from `config/global-config.jsonc`, or you can pass
`-BranchStrategy`. It never overwrites a file you already have unless you add `-Force`, and
`-WhatIf` shows what it would copy. The
[cmdlet help](../../MSCKite.Azure.Platform/New-PlatformWorkflow.md) lists every option.

The workflows sign in to Azure with a short-lived token instead of a stored password. Keep the
`environment:` line in every job that signs in, otherwise the sign-in stops working.

## Before the workflows can run

Run the four platform phases once from your own computer: `Set-PlatformResourceGroup`,
`Set-PlatformSecurityGroup`, `Set-PlatformEnvironmentIdentity` and `Set-PlatformGitHubEnvironment`.
The workflows sign in with identities and GitHub environments that these phases create, so the very
first run cannot happen in a pipeline.

After that, check that every environment in `config/platform/environments.jsonc` has a matching
GitHub environment holding `AZURE_CLIENT_ID`, `AZURE_TENANT_ID` and `AZURE_SUBSCRIPTION_ID`. The
last phase writes these for you.

The platform workflows need a few more one-time steps. They are listed on the
[platform flow](./platform-flow.md) page.

## Where to read next

| Page | Read it to |
| --- | --- |
| [Identities and environments](./identities-and-environments.md) | Understand who signs in to what, and why. |
| [Platform flow](./platform-flow.md) | Decide whether to run the platform through GitHub Actions, and set it up. |
| [GitHub Flow](./github-flow.md) | See how workload and infra deploy with GitHub Flow. |
| [Release Flow](./release-flow.md) | See how workload and infra deploy with Release Flow, including releases. |
| [Pinning the Kite version](./pinning-kite-version.md) | Control when your pipelines move to a new Kite release. |
| [Manual runs and concurrency](./manual-runs-and-concurrency.md) | Know what happens when you start a workflow by hand or two runs overlap. |
