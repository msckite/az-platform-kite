<!-- omit from toc -->
# 0001: Templates are starter samples

- Status: Accepted
- Date: 2026-09-28

## Context

Everything under `templates/` (GitHub workflows including the shared reusable workflows, the setup
action, the Bicep sample, `deploy.ps1` and the configuration files) is copied into a consumer
repository by `Get-PlatformTemplate` and `New-PlatformWorkflow`. A refactor was considered that moved
the shared workflows into this repository's `.github/` so consumers would reference them with
`uses: msckite/az-platform-kite/...@vX` instead of owning a copy.

## Decision

The templates stay starter samples. A consumer copies them, adapts them to their own use case and
owns them from then on. The only shared, versioned component is the PowerShell module, which a
consumer pins with `kiteVersion` in `global-config.jsonc`. Templates and schemas are downloaded from
the release tag that matches the module version.

## Consequences

- Consumers are free to change their pipelines; Kite does not force one behavior on everyone.
- A fix in a template does not reach consumers automatically. Improvements are opt-in: a consumer
  takes them by comparing with, or re-copying, the template of a newer release.
- Do not replace copied workflows with cross-repository `uses:` references.
- Tooling that helps consumers adopt template changes (provenance headers, diff or compare
  commands) is welcome as long as it stays opt-in.
