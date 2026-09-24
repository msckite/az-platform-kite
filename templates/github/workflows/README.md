<!-- omit from toc -->
# Platform and project workflow templates

Ready-to-copy GitHub Actions workflows for fixed platform reconciliation and project CI/CD
placeholders. The platform flow is independent of `sourceControl.branchStrategy`; only project
workflows use the `github` or `release` strategy from `config/global-config.jsonc`.

<!-- omit from toc -->
## Table of Contents

- [Layout](#layout)
- [Installing the templates](#installing-the-templates)
- [Provisioning phases and dependencies](#provisioning-phases-and-dependencies)
- [Platform flow](#platform-flow)
- [GitHub Flow project strategy (`github`)](#github-flow-project-strategy-github)
- [Release Flow project strategy (`release`)](#release-flow-project-strategy-release)
- [Prerequisites](#prerequisites)
- [Why the environment input is a gate, not a scope](#why-the-environment-input-is-a-gate-not-a-scope)

## Layout

```text
templates/github/
  actions/
    setup-platform-kite/action.yml   Composite action: installs the module and signs in to Azure (OIDC)
  workflows/
    manifest.jsonc                   Source/destination map used by the install command
    shared/
      platform-validate.yml          Reusable: template and schema version validation
      platform-provision.yml         Reusable: the four provisioning phases, correctly chained
      project-validate.yml           Reusable: source validation, build and tests
      project-provision.yml          Reusable: project plan/deployment placeholder
    platform-flow/
      platform-ci.yml                 Fixed platform PR validation and WhatIf plan
      platform-cd.yml                 Fixed platform main deployment
    github-flow/
      project-ci.yml                 GitHub Flow project trigger
      project-cd.yml                 GitHub Flow project trigger
    release-flow/
      project-ci.yml                 Release Flow project trigger
      project-cd.yml                 Release Flow project trigger
      project-release.yml            Release Flow project trigger
```

The platform workflows have one fixed lifecycle: pull requests validate and run `-WhatIf`, while
pushes to `main` deploy without `-WhatIf`. Project strategy folders contain thin trigger workflows.
The shared project workflows provide validation/testing and provisioning capabilities. The
strategy workflows call them and contain no project implementation logic.

## Installing the templates

`New-PlatformWorkflow` copies either the platform bundle, the project bundle, or both. Platform
installation never reads `sourceControl.branchStrategy`:

| Source                                             | Destination                                         |
| -------------------------------------------------- | --------------------------------------------------- |
| `github/actions/setup-platform-kite/action.yml`     | `.github/actions/setup-platform-kite/action.yml`     |
| `github/workflows/shared/platform-validate.yml`     | `.github/workflows/platform-validate.yml`            |
| `github/workflows/shared/platform-provision.yml`    | `.github/workflows/platform-provision.yml`           |
| `github/workflows/platform-flow/platform-ci.yml`    | `.github/workflows/platform-ci.yml`                 |
| `github/workflows/platform-flow/platform-cd.yml`    | `.github/workflows/platform-cd.yml`                 |

The project bundle copies the shared reusable workflows and the `project-*.yml` trigger files for
the selected project strategy. The shared workflows use the setup action. Project CI validates,
builds and tests source code, then calls `project-provision.yml` with `what-if: true`. Project CD
calls the same reusable workflow with `what-if: false` for the actual deployment. Each project
operation remains a PowerShell placeholder for the developer to replace.

Reusable workflows referenced with `./.github/workflows/...` must live directly in
`.github/workflows`, which is why the `shared` and `<strategy>-flow` folders flatten on copy.

## Provisioning phases and dependencies

`platform-provision.yml` runs the four phases as separate jobs, chained with `needs`:

```mermaid
flowchart LR
  P1["Phase 1<br/>Set-PlatformResourceGroup"] --> P2["Phase 2<br/>Set-PlatformSecurityGroup"]
  P1 --> P3["Phase 3<br/>Set-PlatformEnvironmentIdentity"]
  P3 --> P4["Phase 4<br/>Set-PlatformGitHubEnvironment"]
```

- Phase 2 and phase 3 both need the resource groups from phase 1 for their role assignments, and
  are independent of each other, so they run in parallel.
- Phase 4 resolves `${clientId}` from the identity created in phase 3, so it must wait for it.

Set the `what-if` input to `true` to run every phase with `-WhatIf`, which is what the CI workflows do.

## Platform flow

Pull requests run validation and a `-WhatIf` platform reconciliation. A push to `main` runs the
same validation and reconciles the complete platform configuration once.

```mermaid
flowchart LR
  PR["pull request"] --> CI["platform-ci<br/>validate + -WhatIf"]
  CI --> M["merge to main"]
  M --> CD["platform-cd"]
  CD --> D["reconcile platform without -WhatIf"]
```

Make `platform-ci` a required status check on `main`. Both workflows sign in as the dedicated
`platform` environment declared in `platform-config.jsonc`, not one of the project's `dev`/`stg`/
`prd` environments. Its identity holds subscription-level RBAC, so it can see and reconcile every
resource group declared in `resourceGroups`, not just the one it lives in.

## GitHub Flow project strategy (`github`)

Project CI runs on feature/fix branches and pull requests. Project CD runs after a push to `main`.
Both ignore the platform-only paths (`config/global-config.jsonc`, `config/platform-config.jsonc`,
`.github/workflows/platform-*.yml`, `.github/actions/setup-platform-kite/**`), which the platform
flow already covers.

## Release Flow project strategy (`release`)

Project CI also includes `release/**` branches. Project CD promotes through dev and stg, while the
project release trigger handles production after a published release. Project CI and CD ignore the
same platform-only paths as the GitHub Flow strategy.

## Prerequisites

1. Run the four phases locally once (`Set-PlatformResourceGroup`, `Set-PlatformSecurityGroup`,
   `Set-PlatformEnvironmentIdentity`, `Set-PlatformGitHubEnvironment`). The workflows authenticate
   with the federated identity that phase 3 creates and the environment secrets that phase 4 sets,
   so the very first run has to happen from a workstation.
2. Grant the `platform` environment's identity the Microsoft Graph application permission it needs
   to read (and, if you want CI to manage groups outside `-WhatIf`, create) Entra security groups
   in phase 2, since CI now runs that phase as the `platform` identity, not a project environment's
   identity. This is a one-time, manual step, not something the phases do automatically: assigning
   Microsoft Graph app roles requires the Application Administrator, Privileged Role Administrator,
   or Global Administrator directory role, and the automated identity must never hold that role
   permanently. Using the `PrincipalId` from phase 3's output for the `platform` environment:

   ```powershell
   Grant-PlatformGraphPermission -PrincipalId '<principalId-from-phase-3-platform-environment>' -Permission 'Group.Read.All'
   ```

   Add `'Group.ReadWrite.All'` to `-Permission` if CI is ever expected to create or update groups
   outside `-WhatIf`. Without this grant, `Set-PlatformSecurityGroup` fails, or in CI's
   `-ErrorAction SilentlyContinue` lookup path, misreports existing groups as missing.
3. Confirm every environment in `platform-config.jsonc` has a matching GitHub environment holding
   `AZURE_CLIENT_ID`, `AZURE_TENANT_ID` and `AZURE_SUBSCRIPTION_ID`. Phase 4 writes these.
4. Add a repository or environment secret named `PLATFORM_GITHUB_TOKEN`, a fine-grained personal
   access token or GitHub App token with administration, environment, secret and variable write
   access on the repository. The built-in `GITHUB_TOKEN` cannot manage environment secrets, so
   phase 4 fails without it.
5. The federated credential uses `subjectType: environment`, so every job that signs in to Azure
   declares `environment:`. Keep it that way, otherwise the OIDC subject claim no longer matches.

## Why the environment input is a gate, not a scope

Each `Set-Platform*` cmdlet reconciles the complete configuration file, not one environment. The
`environment` input on `platform-provision.yml` therefore selects the existing project GitHub
environment whose federated credential signs in to Azure and whose protection rules apply before
the single reconciliation starts. It is an approval and identity gate, not a configuration scope.
