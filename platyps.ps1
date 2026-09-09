#Requires -Version 7.0
<#
.SYNOPSIS
    Manages the Markdown-based PowerShell help authoring workflow for Azure Platform Kite using
    Microsoft.PowerShell.PlatyPS.

.DESCRIPTION
    Wraps the four PlatyPS stages described at
    https://learn.microsoft.com/en-us/powershell/utility-modules/platyps/overview into a single,
    repeatable entry point so the workflow can be re-run every time cmdlets are added or changed:

    - New     Generates Markdown help for cmdlets that don't have a file yet in ./docs. Pass
              -Command to scope it to specific new cmdlets; otherwise every cmdlet without
              existing Markdown is picked up and the module page is (re)created.
    - Update  Refreshes existing Markdown help files to reflect parameter/output changes made to
              already-documented cmdlets, then updates the module page. PlatyPS only ever adds
              content here (using "{{ }}" placeholders); it never rewrites existing prose.
    - Test    Validates every Markdown file and fails if PlatyPS reports diagnostics or any
              unresolved "{{ }}" placeholder remains. Run this before Publish.
    - Publish Converts the Markdown files to MAML and writes
              MSCKite.Azure.Platform.dll-Help.xml to ./en-US, which the csproj copies into the
              build output on the next `dotnet build` / `./build.ps1`.

    New and Update require the module to be loaded in the current session first (see
    ./build.ps1 -Import), since PlatyPS reflects over the live module to discover
    cmdlets/parameters/output types.

.PARAMETER Mode
    Which stage of the PlatyPS workflow to run: New, Update, Test, or Publish.

.PARAMETER Command
    Only used with -Mode New. Scopes markdown generation to these specific cmdlet names instead
    of every cmdlet that doesn't have a Markdown file yet.

.EXAMPLE
    ./build.ps1 -Import
    ./platyps.ps1 -Mode New -Command Get-NewThing

.EXAMPLE
    ./platyps.ps1 -Mode Update

.EXAMPLE
    ./platyps.ps1 -Mode Test

.EXAMPLE
    ./platyps.ps1 -Mode Publish
#>
[CmdletBinding()]
param(
    [ValidateSet('New', 'Update', 'Test', 'Publish')]
    [string]$Mode = 'Update',

    [string[]]$Command
)

$ErrorActionPreference = 'Stop'
$moduleName = 'MSCKite.Azure.Platform'
$docsFolder = Join-Path $PSScriptRoot "docs\$moduleName"
$moduleMarkdownPath = Join-Path $docsFolder "$moduleName.md"
$placeholderPattern = '\{\{.*\}\}'

# Translates a PlatyPS diagnostic Source/Identifier pair into a human-readable description
function Format-PlatyPSDiagnostic {
    param($Source, $Identifier)

    switch ("$Source/$Identifier") {
        'Links/GetRelatedLinks' { 'RELATED LINKS section is empty'; break }
        'Notes/GetNotes' { 'NOTES section is empty'; break }
        default { "[$Source] $Identifier" }
    }
}

if ($Mode -in 'New', 'Update' -and -not (Get-Module -Name $moduleName)) {
    throw "Module '$moduleName' isn't loaded. Run './build.ps1 -Import' first, then re-run this script inside that session."
}

switch ($Mode) {
    'New' {
        # https://learn.microsoft.com/en-us/powershell/utility-modules/platyps/step-1-create-new-markdown-help
        $newMarkdownCommandHelpSplat = @{
            ModuleInfo   = Get-Module -Name $moduleName
            OutputFolder = './docs'
        }
        if ($Command) {
            $newMarkdownCommandHelpSplat.Command = $Command
        } else {
            $newMarkdownCommandHelpSplat.WithModulePage = $true
        }
        New-MarkdownCommandHelp @newMarkdownCommandHelpSplat
    }

    'Update' {
        # https://learn.microsoft.com/en-us/powershell/utility-modules/platyps/step-1-update-markdown-help
        Measure-PlatyPSMarkdown -Path "$docsFolder\*.md" |
            Where-Object Filetype -Match 'CommandHelp' |
            Update-MarkdownCommandHelp -Path { $_.FilePath }

        Measure-PlatyPSMarkdown -Path "$docsFolder\*.md" |
            Where-Object Filetype -Match 'CommandHelp' |
            Import-MarkdownCommandHelp -Path { $_.FilePath } |
            Update-MarkdownModuleFile -Path $moduleMarkdownPath

        $placeholders = Select-String -Path "$docsFolder\*.md" -Pattern $placeholderPattern
        if ($placeholders) {
            Write-Warning "Found $($placeholders.Count) unresolved placeholder(s). Fill these in (or ask Copilot to) before running -Mode Test:"
            $placeholders | ForEach-Object { Write-Warning "  $($_.Path):$($_.LineNumber): $($_.Line.Trim())" }
        } else {
            Write-Host 'No placeholders found. Ready for -Mode Test.' -ForegroundColor Green
        }
    }

    'Test' {
        # https://learn.microsoft.com/en-us/powershell/utility-modules/platyps/step-3-test-markdown-help
        # Diagnostics.Messages includes routine "Information" entries for every section PlatyPS
        # recognized; only "Error" severity indicates something is actually broken. "Warning"
        # entries (e.g. an intentionally empty NOTES or RELATED LINKS section) are surfaced but
        # don't fail the build.
        $warningCount = 0
        $errorCount = 0

        Measure-PlatyPSMarkdown -Path "$docsFolder\*.md" |
            Where-Object Filetype -Match 'CommandHelp' |
            ForEach-Object {
                $fileName = Split-Path -Path $_.FilePath -Leaf
                $messages = (Import-MarkdownCommandHelp -Path $_.FilePath).Diagnostics.Messages

                foreach ($message in ($messages | Where-Object Severity -EQ 'Warning')) {
                    $warningCount++
                    Write-Host ('{0,-7}: {1}, {2}' -f 'WARNING', $fileName, (Format-PlatyPSDiagnostic $message.Source $message.Identifier)) -ForegroundColor Yellow
                }
                foreach ($message in ($messages | Where-Object Severity -EQ 'Error')) {
                    $errorCount++
                    Write-Host ('{0,-7}: {1}, {2}' -f 'ERROR', $fileName, (Format-PlatyPSDiagnostic $message.Source $message.Identifier)) -ForegroundColor Red
                }
            }

        $moduleFileName = Split-Path -Path $moduleMarkdownPath -Leaf
        $moduleMessages = (Import-MarkdownModuleFile -Path $moduleMarkdownPath).Diagnostics.Messages
        foreach ($message in ($moduleMessages | Where-Object Severity -EQ 'Warning')) {
            $warningCount++
            Write-Host ('{0,-7}: {1}, {2}' -f 'WARNING', $moduleFileName, (Format-PlatyPSDiagnostic $message.Source $message.Identifier)) -ForegroundColor Yellow
        }
        foreach ($message in ($moduleMessages | Where-Object Severity -EQ 'Error')) {
            $errorCount++
            Write-Host ('{0,-7}: {1}, {2}' -f 'ERROR', $moduleFileName, (Format-PlatyPSDiagnostic $message.Source $message.Identifier)) -ForegroundColor Red
        }

        $placeholders = Select-String -Path "$docsFolder\*.md" -Pattern $placeholderPattern
        foreach ($placeholder in $placeholders) {
            $errorCount++
            $placeholderFileName = Split-Path -Path $placeholder.Path -Leaf
            Write-Host ('{0,-7}: {1}, {2}, Unresolved placeholder - {3}' -f 'ERROR', $placeholderFileName, $placeholder.LineNumber, $placeholder.Line.Trim()) -ForegroundColor Red
        }

        Write-Host ''
        if ($errorCount -gt 0) {
            throw "Markdown help validation Failed: $errorCount Error(s), $warningCount Warning(s). Fix the errors above before running -Mode Publish."
        }

        Write-Host "Markdown help is valid: $warningCount Warning(s), 0 Errors. Ready for -Mode Publish." -ForegroundColor Green
    }

    'Publish' {
        # https://learn.microsoft.com/en-us/powershell/utility-modules/platyps/step-4-convert-publish-help
        $outputFolder = Join-Path $PSScriptRoot 'src\en-US'
        Measure-PlatyPSMarkdown -Path "$docsFolder\*.md" |
            Where-Object Filetype -Match 'CommandHelp' |
            Import-MarkdownCommandHelp -Path { $_.FilePath } |
            Export-MamlCommandHelp -OutputFolder $outputFolder -Force

        # Export-MamlCommandHelp nests the file under a module-named subfolder; flatten it so it sits directly in en-US
        $helpFileName = "$moduleName.dll-Help.xml"
        $nestedHelpFile = Join-Path $outputFolder "$moduleName\$helpFileName"
        if (Test-Path $nestedHelpFile) {
            Move-Item -Path $nestedHelpFile -Destination (Join-Path $outputFolder $helpFileName) -Force
            Remove-Item -Path (Join-Path $outputFolder $moduleName) -Recurse -Force
        }

        # .bak files only exist to diff -Mode Update's changes before Test/Publish; no longer needed once Publish succeeds
        $backupFiles = Get-ChildItem -Path "$docsFolder\*.bak" -ErrorAction SilentlyContinue
        if ($backupFiles) {
            $backupFiles | Remove-Item -Force
            Write-Host "Removed $($backupFiles.Count) .bak file(s) from '$docsFolder'." -ForegroundColor Green
        }

        Write-Host "Published MAML help to '$outputFolder'. Run './build.ps1' to copy it into the build output." -ForegroundColor Green
    }
}
