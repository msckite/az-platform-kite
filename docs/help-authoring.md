<!-- omit from toc -->
# Help Authoring Workflow

Azure Platform Kite is a **binary** PowerShell module, so `Get-Help` never reads the C# XML doc
comments on cmdlet classes, those only help IntelliSense and readers of the source. Instead,
`Get-Help` reads a compiled MAML file (`MSCKite.Azure.Platform.dll-Help.xml`) that must be
generated from Markdown source and shipped in an `en-US` folder next to the module DLL.

That Markdown-to-MAML pipeline is provided by
[Microsoft.PowerShell.PlatyPS](https://learn.microsoft.com/en-us/powershell/utility-modules/platyps/overview)
and is wrapped by [`platyps.ps1`](../platyps.ps1) in this repository.

<!-- omit from toc -->
## Table of Contents
- [Modes](#modes)
- [Recurring workflow](#recurring-workflow)
- [Placeholders](#placeholders)
- [File layout](#file-layout)

## Modes

`platyps.ps1` runs one of four stages via `-Mode`:

| Mode | Requires module loaded | Purpose |
| --- | --- | --- |
| `New` | Yes | Generates Markdown for cmdlets that don't have a file yet under `./docs/MSCKite.Azure.Platform`. Pass `-Command <Name>` to scope it to specific new cmdlets. |
| `Update` | Yes | Refreshes existing Markdown files (and the module page) to reflect parameter/output changes on already-documented cmdlets. |
| `Test` | No | Validates the Markdown files and fails if PlatyPS reports diagnostics, or any `{{ }}` placeholder is still unresolved. |
| `Publish` | No | Converts the Markdown files to MAML and writes `en-US\MSCKite.Azure.Platform.dll-Help.xml`. |

`New` and `Update` reflect over the *loaded* module to discover cmdlets/parameters/output types,
so build the module first, then run `platyps.ps1` in a PowerShell session that has the built DLL
imported. Never import it in the terminal you build in: that session keeps the DLL locked, and the
next build fails (see [CONTRIBUTING.md](../CONTRIBUTING.md#building-and-testing-locally)). Use one of:

- **A separate `pwsh` process** that imports the DLL, runs the mode, and exits, which releases the
  lock straight away:

  ```powershell
  pwsh -NoProfile -Command 'Import-Module ./src/bin/Debug/netstandard2.0/MSCKite.Azure.Platform.dll; ./platyps.ps1 -Mode Update'
  ```

- **The debug terminal**: start the **PowerShell cmdlets: pwsh** launch configuration (`F5`), which
  builds the module and opens a terminal with it imported, run `./platyps.ps1 -Mode Update` there,
  and stop debugging (`Shift+F5`) when you're done.

`Test` and `Publish` read only the Markdown files, so run them in any terminal.

## Recurring workflow

Follow this sequence whenever a cmdlet is added or changed:

```powershell
# 1. Build the module (or in VS Code: Tasks: Run Build Task > Build)
Invoke-psake ./build.ps1 -taskList Build

# 2. Only for brand-new cmdlets: generate their Markdown in a separate process that loads the module
pwsh -NoProfile -Command 'Import-Module ./src/bin/Debug/netstandard2.0/MSCKite.Azure.Platform.dll; ./platyps.ps1 -Mode New'

# 3. Refresh Markdown for changed cmdlets, the same way
pwsh -NoProfile -Command 'Import-Module ./src/bin/Debug/netstandard2.0/MSCKite.Azure.Platform.dll; ./platyps.ps1 -Mode Update'

# 4. Ask Copilot (or edit manually) to fill in any {{ }} placeholders flagged by step 3

# 5. Validate before publishing
./platyps.ps1 -Mode Test

# 6. Generate en-US\MSCKite.Azure.Platform.dll-Help.xml
./platyps.ps1 -Mode Publish

# 7. Copy the published help into the build output
Invoke-psake ./build.ps1 -taskList Build
```

Every step runs in the same terminal: the module is only ever imported by the short-lived `pwsh`
processes in steps 2 and 3, so the build in step 7 never finds the DLL locked.

## Placeholders

`Update-MarkdownCommandHelp` and `Update-MarkdownModuleFile` only ever *add* new content; they
never rewrite or remove existing prose. New content is inserted as a `{{ Fill in ... }}`
placeholder. `-Mode Update` reports any placeholders it finds, and `-Mode Test` fails the script
if one is still present, so nothing incomplete reaches `-Mode Publish`.

## File layout

- `docs/MSCKite.Azure.Platform/*.md`: source of truth, hand-edited Markdown help per cmdlet plus
  the module page. Follows the structure described in
  [Edit Markdown help files](https://learn.microsoft.com/en-us/powershell/utility-modules/platyps/step-2-edit-markdown-help).
- `src/en-US/MSCKite.Azure.Platform.dll-Help.xml`: generated MAML output of `-Mode Publish`. Copied
  into the build output by an `ItemGroup` in
  [`MSCKite.Azure.Platform.csproj`](../src/MSCKite.Azure.Platform.csproj).
