<!-- omit from toc -->
# Templates

## Workflow templates

`github/workflows` holds ready-to-copy GitHub Actions workflows that provision the files in `platform/`
in phase order, one set per `sourceControl.branchStrategy` value. See
[docs/consumers/workflows](../docs/consumers/workflows/README.md).

## Issue status labels

`skills/github-issue-status-labels` is an agent skill that sets up or migrates a repository so that
`status:` labels mirror the GitHub issue status. It holds the workflow, the label set and the
instructions. Issue statuses are a GitHub public preview and may not be available in your repository
yet.

To use it, download the templates and copy the skill folder into your repository, to
`.claude/skills/` for Claude Code, or `.github/skills/` for GitHub Copilot:

```powershell
Get-PlatformTemplate
New-Item -ItemType Directory .claude/skills -Force
Copy-Item .tmp/templates/skills/github-issue-status-labels .claude/skills -Recurse
```

Then ask your agent to set up issue status labels. Without an agent, follow the steps in its
`SKILL.md` yourself.

The label set,
[`github-labels.jsonc`](./skills/github-issue-status-labels/assets/github-labels.jsonc), also works
on its own with `Set-GitHubLabels`. Add `-KeepExistingLabels` to keep the labels that it does not
list.

## Template versioning

Every shipped JSON template has a required `templateVersion` and every matching JSON Schema has a `schemaVersion`. Both use `Major.Minor.Patch` SemVer and the versions must match exactly for a template/schema pair to be compatible. The schemas also require the matching template version, so standard JSON Schema validators reject an incompatible template.

Use `Test-PlatformTemplate` after downloading templates, supplying the corresponding local schema. Pass a newer downloaded template with `-LatestTemplatePath` to report whether an update is available without overwriting the configuration currently in use.

```powershell
Test-PlatformTemplate `
  -TemplatePath .\config\global-config.jsonc `
  -SchemaPath .\schemas\global-config.schema.json `
  -LatestTemplatePath .\.tmp\templates\global-config.jsonc
```

> [!NOTE]
> `-SchemaPath` always points at a local schema file, never the `$schema` URL embedded in the
template, so this check runs offline. It's primarily useful when maintaining templates and
schemas in this repo (keeping a pair's versions in sync as changes are made) and for anyone who has downloaded both a template and its matching schema and wants to confirm compatibility, or check for a newer template, before adopting it.

Increment the major version for breaking contract changes and increment minor or patch versions for compatible additions or corrections, updating the template and schema together. All template consumers require the versioned object format with `templateVersion`.

Template versions travel with Kite releases: a consuming repository adopts a new `templateVersion` by raising `kiteVersion` in its `global-config.jsonc` to the Kite release that ships it, since `Get-PlatformTemplate` and the `platform-validate` workflow read templates and schemas from that release's tag rather than `main`. Changing a template or schema on `main` therefore affects no consumer until it is released and adopted.
