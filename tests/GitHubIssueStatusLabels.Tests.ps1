$ErrorActionPreference = 'Stop'

# The skill in templates/skills is the source. The installed skill and this repository's .github files are copies of it.
BeforeDiscovery {
    $script:skillPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'templates/skills/github-issue-status-labels'

    $script:skillFiles = @(Get-ChildItem -Path $script:skillPath -Recurse -File -Force | ForEach-Object {
            @{ File = [System.IO.Path]::GetRelativePath($script:skillPath, $_.FullName).Replace('\', '/') }
        })
}

Describe 'GitHub issue status labels copies' {
    BeforeAll {
        $script:root = Split-Path $PSScriptRoot -Parent
        $script:skillPath = Join-Path $script:root 'templates/skills/github-issue-status-labels'
        $script:installedPath = Join-Path $script:root '.claude/skills/github-issue-status-labels'

        function Get-NormalizedContent([string]$Path) {
            (Get-Content -Path $Path -Raw).Replace("`r`n", "`n")
        }

        # Only the status labels are shared; every repository chooses its other labels
        function Get-StatusLabel([string]$Path) {
            (Get-Content -Path $Path -Raw | ConvertFrom-Json).labels |
                Where-Object name -Like 'status: *' |
                Sort-Object name |
                ForEach-Object { '{0} | {1} | {2}' -f $_.name, $_.color, $_.description }
        }

        function Get-RelativeFile([string]$Path) {
            Get-ChildItem -Path $Path -Recurse -File -Force |
                ForEach-Object { [System.IO.Path]::GetRelativePath($Path, $_.FullName).Replace('\', '/') } |
                Sort-Object
        }
    }

    It 'installs the same set of files in .claude/skills as the skill has' {
        Get-RelativeFile $script:installedPath | Should -Be (Get-RelativeFile $script:skillPath)
    }

    It 'keeps the installed skill file <File> equal to the template' -ForEach $script:skillFiles {
        Get-NormalizedContent (Join-Path $script:installedPath $File) |
            Should -BeExactly (Get-NormalizedContent (Join-Path $script:skillPath $File)) -Because "templates/skills is the source of .claude/skills for $File"
    }

    It 'keeps the workflow of this repository equal to the skill' {
        Get-NormalizedContent (Join-Path $script:root '.github/workflows/issue-status-labels.yml') |
            Should -BeExactly (Get-NormalizedContent (Join-Path $script:skillPath 'assets/issue-status-labels.yml'))
    }

    It 'keeps the status labels of this repository equal to the skill' {
        $expected = @(Get-StatusLabel (Join-Path $script:skillPath 'assets/github-labels.jsonc'))
        $actual = @(Get-StatusLabel (Join-Path $script:root '.github/github-labels.jsonc'))

        $expected.Count | Should -Be 7 -Because 'GitHub has seven issue statuses'
        $actual -join "`n" | Should -BeExactly ($expected -join "`n")
    }
}
