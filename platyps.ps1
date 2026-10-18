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
              build output on the next build (the VS Code Build task, or
              Invoke-psake ./build.ps1 -taskList Build).

    New and Update require the built module to be loaded in the session that runs this script,
    since PlatyPS reflects over the live module to discover cmdlets/parameters/output types.
    Load it in a separate pwsh process that exits afterwards (see the examples), never in the
    terminal you build in: that session would keep the DLL locked and the next build fails.
    See docs/contributors/help-authoring.md.

.PARAMETER Mode
    Which stage of the PlatyPS workflow to run: New, Update, Test, or Publish.

.PARAMETER Command
    Only used with -Mode New. Scopes markdown generation to these specific cmdlet names instead
    of every cmdlet that doesn't have a Markdown file yet.

.EXAMPLE
    pwsh -NoProfile -Command 'Import-Module ./src/bin/Debug/netstandard2.0/MSCKite.Azure.Platform.dll; ./platyps.ps1 -Mode New -Command Get-NewThing'

    Builds Markdown for one new cmdlet, loading the module in a short-lived process.

.EXAMPLE
    pwsh -NoProfile -Command 'Import-Module ./src/bin/Debug/netstandard2.0/MSCKite.Azure.Platform.dll; ./platyps.ps1 -Mode Update'

    Refreshes the Markdown of changed cmdlets, loading the module in a short-lived process.

.EXAMPLE
    ./platyps.ps1 -Mode Test

.EXAMPLE
    ./platyps.ps1 -Mode Publish
#>
[CmdletBinding()]
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingWriteHost', '', Justification = 'Interactive developer tool: the colored status lines are meant for the console, not the pipeline.')]
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

# Update-MarkdownCommandHelp and Update-MarkdownModuleFile aren't idempotent: on every run they repeat each
# parameter description once more, re-add a BOM and CRLF line endings, and bump ms.date, even when nothing
# changed. This undoes that so a no-op Update leaves the files byte-for-byte untouched.
function Repair-PlatyPSMarkdown {
    param([string]$Path)

    $backupPath = "$Path.bak"
    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    $readNormalized = { param($file) [System.IO.File]::ReadAllText($file).TrimStart([char]0xFEFF).Replace("`r`n", "`n") }

    $lines = (& $readNormalized $Path) -split "`n"

    # Collapse a parameter description that repeats itself (whole copies, word for word) back to one copy
    $result = [System.Collections.Generic.List[string]]::new()
    $index = 0
    while ($index -lt $lines.Count) {
        $result.Add($lines[$index])
        if ($lines[$index] -notmatch '^### -') { $index++; continue }

        $bodyStart = $index + 1
        $bodyEnd = $bodyStart
        while ($bodyEnd -lt $lines.Count -and $lines[$bodyEnd] -ne '```yaml') { $bodyEnd++ }
        $body = @($lines[$bodyStart..($bodyEnd - 1)])

        $wordsPerLine = @($body | ForEach-Object { , @($_ -split '\s+' | Where-Object { $_ }) })
        $words = @($wordsPerLine | ForEach-Object { $_ })
        $keepUntil = $body.Count
        for ($period = 1; $period -le $words.Count / 2; $period++) {
            if ($words.Count % $period -ne 0) { continue }
            $repeats = $true
            for ($i = $period; $i -lt $words.Count; $i++) {
                if ($words[$i] -cne $words[$i % $period]) { $repeats = $false; break }
            }
            if (-not $repeats) { continue }

            # Only collapse when one copy ends exactly at the end of a line
            $wordsSoFar = 0
            for ($line = 0; $line -lt $body.Count; $line++) {
                $wordsSoFar += $wordsPerLine[$line].Count
                if ($wordsSoFar -eq $period) { $keepUntil = $line + 1; break }
            }
            break
        }

        if ($keepUntil -eq $body.Count) {
            $body | ForEach-Object { $result.Add($_) }
        } else {
            $body | Select-Object -First $keepUntil | ForEach-Object { $result.Add($_) }
            $result.Add('')
        }
        $index = $bodyEnd
    }

    $normalized = $result -join "`n"

    # No real change besides the churn above: put the original file back, including its ms.date
    if (Test-Path -LiteralPath $backupPath) {
        $original = & $readNormalized $backupPath
        $dateLine = '(?m)^ms\.date: .*$'
        if (($normalized -replace $dateLine) -ceq ($original -replace $dateLine)) {
            $normalized = $original
        }
    }

    [System.IO.File]::WriteAllText($Path, $normalized, $utf8NoBom)
}

if ($Mode -in 'New', 'Update' -and -not (Get-Module -Name $moduleName)) {
    throw "Module '$moduleName' isn't loaded. Build it, then run this mode in a separate process that loads it: pwsh -NoProfile -Command 'Import-Module ./src/bin/Debug/netstandard2.0/$moduleName.dll; ./platyps.ps1 -Mode $Mode'. See docs/contributors/help-authoring.md."
}

switch ($Mode) {
    'New' {
        # https://learn.microsoft.com/en-us/powershell/utility-modules/platyps/step-1-create-new-markdown-help
        $newMarkdownCommandHelpSplat = @{
            ModuleInfo   = Get-Module -Name $moduleName
            OutputFolder = './docs'
        }
        if ($Command) {
            # New-MarkdownCommandHelp takes -CommandInfo (CommandInfo[]), not command name strings
            $newMarkdownCommandHelpSplat.CommandInfo = Get-Command -Name $Command -Module $moduleName
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

        Get-ChildItem -Path "$docsFolder\*.md" | ForEach-Object { Repair-PlatyPSMarkdown -Path $_.FullName }

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

        # Export-MamlCommandHelp writes CRLF on Windows and omits the final newline; normalize to the LF with a final newline
        # that .gitattributes (docs/contributors/line-endings.md) and files.insertFinalNewline in .vscode/settings.json expect
        $helpFilePath = Join-Path $outputFolder $helpFileName
        $helpContent = [System.IO.File]::ReadAllText($helpFilePath).Replace("`r`n", "`n")
        if (-not $helpContent.EndsWith("`n")) {
            $helpContent += "`n"
        }
        [System.IO.File]::WriteAllText($helpFilePath, $helpContent, (New-Object System.Text.UTF8Encoding($false)))

        # .bak files only exist to diff -Mode Update's changes before Test/Publish; no longer needed once Publish succeeds
        $backupFiles = Get-ChildItem -Path "$docsFolder\*.bak" -ErrorAction SilentlyContinue
        if ($backupFiles) {
            $backupFiles | Remove-Item -Force
            Write-Host "Removed $($backupFiles.Count) .bak file(s) from '$docsFolder'." -ForegroundColor Green
        }

        Write-Host "Published MAML help to '$outputFolder'. Run the Build task (or 'Invoke-psake ./build.ps1 -taskList Build') to copy it into the build output." -ForegroundColor Green
    }
}
