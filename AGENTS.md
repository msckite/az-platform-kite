# Agent instructions

Azure Platform Kite is a PowerShell **binary** module (C#, `netstandard2.0`) plus starter templates
for GitHub workflows and platform configuration. This file is the entry point for coding agents;
human contributors should read [CONTRIBUTING.md](./CONTRIBUTING.md), which this file builds on.

## Repository map

- `src/` - the module: cmdlets in `src/Commands`, helpers in `src/Internal`, models in `src/Models`,
  compiled help in `src/en-US`.
- `tests/` - Pester tests.
- `docs/` - contributor documentation and generated cmdlet help (`docs/MSCKite.Azure.Platform`).
- `templates/` and `schemas/` - what consumers copy into their own repositories.
- `config/` - git-ignored local test configuration that mirrors the sandbox repository. It is not part of the repository; never commit it or treat it as a reference configuration.
- `build.ps1` (psake) and `platyps.ps1` (help generation) - the build entry points.

## Checks

Run these before opening a pull request:

```powershell
Invoke-psake ./build.ps1 -taskList Test        # expect 95 passed, 3 skipped
./platyps.ps1 -Mode Test
Invoke-ScriptAnalyzer -Path . -Recurse -Settings ./scripts/PSScriptAnalyzerSettings.psd1
npx markdownlint-cli "**/*.md"
```

> [!IMPORTANT]
> Never `Import-Module` the module DLL in the terminal you build in; the session keeps the DLL
> locked and the next build fails. Load it in a separate process instead, for example when
> regenerating help:
>
> ```powershell
> pwsh -NoProfile -Command 'Import-Module ./src/bin/Debug/netstandard2.0/MSCKite.Azure.Platform.dll; ./platyps.ps1 -Mode Update'
> ```

Help is generated from Markdown with PlatyPS, not from C# doc comments. After changing a cmdlet's
parameters or description, update the help as described in
[docs/help-authoring.md](./docs/help-authoring.md) and commit the regenerated files.

## Conventions

- File format and style follow `.vscode/settings.json`: UTF-8 without BOM, LF line endings (see
  [docs/line-endings.md](./docs/line-endings.md)), four spaces in PowerShell and C#, two spaces in
  JSON, JSONC and YAML. Naming rules are in [CONTRIBUTING.md](./CONTRIBUTING.md#formatting-guidelines).
- Windows with Git Bash has no Python. Use PowerShell or your editor tooling for scripted edits.
- Split work into logical commits. Commit and pull request titles are Conventional Commits:
  `<type>: <Capitalized summary>` (for example `fix: Make platyps Update idempotent`). Pull requests
  are squash-merged and the pull request title becomes the commit title on `main`.
- Fill in [.github/pull_request_template.md](./.github/pull_request_template.md).

## Rules that must not be broken

- The files in `templates/` are **starter samples**. Consumers copy them (`Get-PlatformTemplate`,
  `New-PlatformWorkflow`) and own their copies. Do not replace copies with cross-repo
  `uses: msckite/az-platform-kite/...@vX` references.
- Publishing a GitHub Release publishes the module to the PowerShell Gallery and cannot be undone.
  Do not create releases or tags without explicit approval from a maintainer.
- Do not push to `main`; open a pull request for a maintainer to review and merge.
