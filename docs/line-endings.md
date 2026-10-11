<!-- omit from toc -->
# Line Endings

Every line in a text file ends with an invisible marker: Windows traditionally uses two characters,
**CRLF** (`\r\n`), Linux and macOS use one, **LF** (`\n`). This repository uses **LF everywhere**, in
the repository and in every working copy, on every machine. Two settings enforce it:

- [`.gitattributes`](../.gitattributes): `* text=auto eol=lf` makes git store **and** check out every
  text file with LF, whatever `core.autocrlf` is set to on the machine. Binary files (`*.png`) are
  never converted.
- [`.vscode/settings.json`](../.vscode/settings.json): `"files.eol": "\n"` makes VS Code create new
  files with LF.

<!-- omit from toc -->
## Table of Contents
- [What the settings do](#what-the-settings-do)
- [Why LF](#why-lf)
- [Checking and fixing line endings](#checking-and-fixing-line-endings)
- [Exceptions](#exceptions)

## What the settings do

The `eol` attribute only decides the working copy; the repository stores LF in every case:

| `.gitattributes` | Stored in the repository | Working copy on Windows | Working copy on Linux/macOS |
| --- | --- | --- | --- |
| `* text=auto` (no `eol`) | LF | depends on the machine's `core.autocrlf`; **CRLF** with the Git for Windows default | LF |
| `* text=auto eol=lf` (this repository) | LF | **LF** | LF |
| `* text=auto eol=crlf` | LF | CRLF | CRLF |

The Git for Windows installer sets `core.autocrlf=true` in the system configuration
(`C:\Program Files\Git\etc\gitconfig`) when you keep its default, *Checkout Windows-style, commit
Unix-style line endings*. `eol=lf` in `.gitattributes` overrides that for this repository only, so
nothing needs to change on your machine. Run `git config --show-origin --get core.autocrlf` to see
where your setting comes from.

`text=auto` lets git detect which files are text; files it detects as binary are never converted.

## Why LF

1. **The pipelines run on Linux.** The GitHub Actions runners (`ubuntu-latest`) execute the scripts.
   Bash fails on a CRLF script: it reads the `\r` as part of the command (`$'\r': command not found`),
   and a `#!/usr/bin/env bash` line ending in `\r` cannot find its interpreter. PowerShell and YAML
   accept both, but mixing them gains nothing.
2. **The templates travel to other repositories.** `Get-PlatformTemplate` clones this repository and
   `New-PlatformWorkflow` copies the templates into a consuming repository. This repository's
   `.gitattributes` applies during that clone, so a consumer on Windows gets the sample scripts, such
   as `stg-verification.sh`, with LF regardless of their own git settings.
3. **Identical bytes on every machine and for every tool.** With `core.autocrlf`, the working copy
   differs per machine, and any tool that writes LF (formatters, scripts, PlatyPS) makes files show up
   as modified although their content is unchanged. With `eol=lf`, editor, tools, git, and CI all see
   the same file, so there are no phantom changes and no diffs in which every line changed.
4. **No downside on Windows.** VS Code, Visual Studio, .NET, PowerShell 7, Windows PowerShell 5.1, and
   Notepad (since Windows 10, 2018) all handle LF correctly.

## Checking and fixing line endings

List the line ending of every tracked file, in the repository (`i/`) and in your working copy (`w/`):

```powershell
git ls-files --eol
```

Show which attributes apply to a file:

```powershell
git check-attr text eol -- templates/github/scripts/stg-verification.sh
```

A clone made before `eol=lf` was added keeps its CRLF working files until they are checked out
again. Re-clone, or convert the working copy in place; on a clean working tree (commit or stash your
changes first), this rewrites the tracked files only and leaves untracked and ignored files, such as
`config/`, untouched:

```powershell
git rm -r -q --cached .
git reset -q --hard
```

## Exceptions

Add a more specific rule **below** `* text=auto eol=lf` when a file type needs something else; later
rules override earlier ones:

- **New binary file types**: `text=auto` detects most binaries, but marking them is explicit and
  safe, for example `*.jpg binary` or `*.zip binary`.
- **Windows batch files** can misbehave with LF. The repository has none today; if you add one, keep
  it CRLF:

  ```gitattributes
  *.bat text eol=crlf
  *.cmd text eol=crlf
  ```
