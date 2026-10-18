<!-- markdownlint-disable-file MD041 -->
<!--
The pull request title is the page heading, so the description starts at level 2 (hence MD041 is off).

Pull request title: it becomes the commit title when the pull request is squash merged (GitHub
appends " (#<number>)"), so write it as a Conventional Commit, like the repository history:

  <type>: <Summary in the imperative, starting with a capital letter>

  feat      new or extended cmdlet, parameter, template, or workflow capability
  fix       bug fix, also for urgent fixes (no "hotfix" type)
  refactor  restructuring without a change in behavior
  docs      documentation only
  test      tests only
  style     formatting only, no code change
  chore     maintenance: settings, tooling, dependencies
  ci        this repository's own workflows in .github/workflows

  Add "!" after the type for a breaking change, e.g. "feat!: Rename the -Branch parameter of Get-PlatformTemplate".

The squash commit message lists the commits of this branch, so give those meaningful messages too.
Text in this template, including these comments, does not end up in the commit.
-->

## Summary

<!-- What does this pull request change, and why? One or two sentences. -->

## Changes

<!-- The notable changes, one per bullet. -->

-

## Related issues

<!--
"Closes #123" links the issue: issues.yaml marks it "in progress" when this pull request opens and
"completed" when it is merged. Use "Refs #123" to link an issue without closing it. Remove this
section when there is no related issue.
-->

Closes #

## Impact on consumers

<!-- Tick everything that applies and explain it below the list. -->

- [ ] None: internal change only (tooling, tests, or this repository's own workflows)
- [ ] Cmdlets: new or changed cmdlet, parameter, or output
- [ ] Templates or schemas: `templateVersion` and the matching `schemaVersion` are raised together (see `templates/README.md`)
- [ ] Workflow or IaC samples: consumers own their copies and adopt the change by re-copying with `New-PlatformWorkflow`
- [ ] Breaking change: what consumers must change is described below

## Checks

<!-- Tick what you ran. Explain anything you could not run, or that does not apply, below the list. -->

- [ ] Build and tests pass: `Invoke-psake ./build.ps1 -taskList Test`
- [ ] Help is current: `./platyps.ps1 -Mode Test`, and `-Mode Publish` when cmdlet help changed (review the `-Mode Update` diff, see `docs/contributors/help-authoring.md`)
- [ ] Script analysis is clean: `Invoke-ScriptAnalyzer -Path . -Recurse -Settings ./scripts/PSScriptAnalyzerSettings.psd1`
- [ ] Markdown lint is clean: `npx markdownlint-cli "**/*.md"`
- [ ] Files follow `.vscode/settings.json`: UTF-8 without BOM, LF line endings (`docs/contributors/line-endings.md`), 4 spaces for PowerShell and C#, 2 spaces for JSON, JSONC, and YAML
- [ ] Documentation is updated where behavior changed: `README.md`, `docs/`
- [ ] Workflow or template changes were tested in a consuming repository, e.g. `az-platform-kite-sandbox`

## Notes for reviewers

<!-- What deserves a close look, follow-ups, or what was deliberately left out. Remove when empty. -->
