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
so run the **build** task (see [CONTRIBUTING.md](../CONTRIBUTING.md#building-and-testing-locally)),
`Import-Module` the resulting DLL, and execute `platyps.ps1` inside that same session.

## Recurring workflow

Follow this sequence whenever a cmdlet is added or changed:

```powershell
dotnet build src/MSCKite.Azure.Platform.csproj                       # 1. Build the module
Import-Module ./src/bin/Debug/netstandard2.0/MSCKite.Azure.Platform.dll -Force  # 2. Load it
./platyps.ps1 -Mode New                    # 3. Only for brand-new cmdlets
./platyps.ps1 -Mode Update                 # 4. Refresh Markdown for changed cmdlets
# 5. Ask Copilot (or edit manually) to fill in any {{ }} placeholders flagged by step 4
./platyps.ps1 -Mode Test                   # 6. Validate before publishing
./platyps.ps1 -Mode Publish                # 7. Generate en-US\MSCKite.Azure.Platform.dll-Help.xml
dotnet build src/MSCKite.Azure.Platform.csproj                       # 8. Copy the published help into the build output
```

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
