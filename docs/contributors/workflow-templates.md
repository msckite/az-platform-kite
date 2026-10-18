<!-- omit from toc -->
# Workflow templates

This page is for people who change the workflow templates under `templates/github/`. It describes
how the templates are laid out, how `New-PlatformWorkflow` installs them and why some of them work
the way they do. For how to use the workflows in your own repository, read the
[workflow templates README](../../templates/github/workflows/README.md).

<!-- omit from toc -->
## Table of Contents

- [Layout](#layout)
- [How the install command copies files](#how-the-install-command-copies-files)
- [Staging verification internals](#staging-verification-internals)
- [Why the environment input is a gate, not a scope](#why-the-environment-input-is-a-gate-not-a-scope)

## Layout

```text
templates/github/
  actions/
    setup-platform-kite/action.yml   Composite action: installs the module and signs in to Azure (OIDC)
  scripts/
    stg-verification.sh              Release Flow: records and verifies content deployed to stg
  release-please/
    config.json                      Release Flow: deliverables and tag format for release-please
    manifest.json                    Release Flow: current version per deliverable
  workflows/
    manifest.jsonc                   Source/destination map used by the install command
    shared/
      platform-validate.yml          Reusable: template and schema version validation
      platform-provision.yml         Reusable: the four provisioning phases, correctly chained
      workload-validate.yml          Reusable: validation, build and tests; uploads the workload package
      workload-provision.yml         Reusable: plans/deploys the workload package (placeholder)
      infra-validate.yml             Reusable: Bicep lint/build placeholder
      infra-provision.yml            Reusable: infra deployment stack plan/deployment placeholder
    platform-flow/
      platform-ci.yml                 Fixed platform PR validation and WhatIf plan
      platform-cd.yml                 Fixed platform main deployment
    github-flow/
      workload-ci.yml                GitHub Flow workload trigger
      workload-cd.yml                GitHub Flow workload trigger
      infra-ci.yml                   GitHub Flow infra trigger
      infra-cd.yml                   GitHub Flow infra trigger
    release-flow/
      workload-ci.yml                Release Flow workload trigger
      workload-cd.yml                Release Flow workload trigger
      workload-release.yml           Release Flow workload trigger
      infra-ci.yml                   Release Flow infra trigger
      infra-cd.yml                   Release Flow infra trigger
      infra-release.yml              Release Flow infra trigger
      release-please.yml             Release Flow: release pull requests per deliverable, starts prd deployments
```

## How the install command copies files

`manifest.jsonc` maps every template to its destination. `-WorkflowType` selects a bundle:
`platform`, `workload`, `infra`, `both` (platform + workload, the default) or `all`. The platform
bundle copies these files and never reads `sourceControl.branchStrategy`:

| Source                                             | Destination                                         |
| -------------------------------------------------- | --------------------------------------------------- |
| `github/actions/setup-platform-kite/action.yml`    | `.github/actions/setup-platform-kite/action.yml`    |
| `github/workflows/shared/platform-validate.yml`    | `.github/workflows/platform-validate.yml`           |
| `github/workflows/shared/platform-provision.yml`   | `.github/workflows/platform-provision.yml`          |
| `github/workflows/platform-flow/platform-ci.yml`   | `.github/workflows/platform-ci.yml`                 |
| `github/workflows/platform-flow/platform-cd.yml`   | `.github/workflows/platform-cd.yml`                 |

Reusable workflows referenced with `./.github/workflows/...` must live directly in `.github/workflows`,
which is why the `shared` and `<strategy>-flow` folders flatten on copy.

## Staging verification internals

The check compares **content**, not the position of a commit in history:

- A release cut from a later commit that did not change the deliverable (for example a workload-only
  change, or a release commit that only updates `CHANGELOG.md` or `version.txt`) still passes, since
  its content is the one `stg` ran. Both files are left out of the fingerprint for that reason.
- Content whose `stg` deployment failed never passes, even after a later commit deployed
  successfully.

`verify` also reports which commit and run deployed that content to `stg`, as the `stg-commit` and
`stg-run-id` step outputs, taken from the link that `mark` stores on the commit status. The workload
release uses the run id to deploy the package `stg` ran.

## Why the environment input is a gate, not a scope

Each `Set-Platform*` cmdlet reconciles the complete configuration file, not one environment. The
`environment` input on `platform-provision.yml` therefore selects the existing workload GitHub
environment whose federated credential signs in to Azure and whose protection rules apply before
the single reconciliation starts. It is an approval and identity gate, not a configuration scope.
