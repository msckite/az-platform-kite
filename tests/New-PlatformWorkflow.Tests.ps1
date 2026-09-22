$ErrorActionPreference = 'Stop'

Describe 'New-PlatformWorkflow' {
    BeforeAll {
        $modulePath = if ($env:MODULE_UNDER_TEST) {
            Convert-Path $env:MODULE_UNDER_TEST
        } else {
            Convert-Path (Join-Path (Get-Location) 'src\bin\Debug\netstandard2.0\MSCKite.Azure.Platform.dll')
        }

        Import-Module $modulePath -Force

        function New-TestTemplateFolder {
            param(
                [Parameter(Mandatory)][string] $Root,
                [string] $Manifest
            )

            if (-not $Manifest) {
                $Manifest = @'
{
  "templateVersion": "1.0.0",
  "shared": [
    { "source": "github/actions/setup-platform-kite/action.yml", "destination": ".github/actions/setup-platform-kite/action.yml" },
    { "source": "github/workflows/shared/platform-provision.yml", "destination": ".github/workflows/platform-provision.yml" }
  ],
  "strategies": {
    "github": {
      "environments": [ "dev", "prd" ],
      "files": [
        { "source": "github/workflows/github/platform-cd.yml", "destination": ".github/workflows/platform-cd.yml" }
      ]
    },
    "release": {
      "environments": [ "dev", "stg", "prd" ],
      "files": [
        { "source": "github/workflows/release/platform-release.yml", "destination": ".github/workflows/platform-release.yml" }
      ]
    }
  }
}
'@
            }

            $files = @(
                'github/actions/setup-platform-kite/action.yml'
                'github/workflows/shared/platform-provision.yml'
                'github/workflows/github/platform-cd.yml'
                'github/workflows/release/platform-release.yml'
            )

            foreach ($file in $files) {
                $path = Join-Path $Root $file
                New-Item -Path (Split-Path -Parent $path) -ItemType Directory -Force | Out-Null
                "name: $([System.IO.Path]::GetFileNameWithoutExtension($file))" | Set-Content -Path $path
            }

            $manifestPath = Join-Path $Root 'github/workflows/manifest.jsonc'
            $Manifest | Set-Content -Path $manifestPath
        }

        function New-TestGlobalConfig {
            param(
                [Parameter(Mandatory)][string] $Path,
                [Parameter(Mandatory)][string] $BranchStrategy
            )

            New-Item -Path (Split-Path -Parent $Path) -ItemType Directory -Force | Out-Null
            @"
{
  "templateVersion": "1.0.0",
  "tenantId": "00000000-0000-0000-0000-000000000000",
  "subscriptionId": "00000000-0000-0000-0000-000000000000",
  "uniqueId": "abc1234",
  "serviceShort": "kite",
  "displayName": "Kite",
  "location": "westeurope",
  "regionCode": "weu",
  "sourceControl": { "tool": "github", "owner": "msckite", "repository": "sandbox", "branchStrategy": "$BranchStrategy" }
}
"@ | Set-Content -Path $Path
        }
    }

    AfterAll {
        Remove-Module MSCKite.Azure.Platform -ErrorAction SilentlyContinue
    }

    BeforeEach {
        $script:templateRoot = Join-Path $TestDrive ('templates-' + [guid]::NewGuid().ToString('N'))
        $script:repoRoot = Join-Path $TestDrive ('repo-' + [guid]::NewGuid().ToString('N'))
        New-TestTemplateFolder -Root $script:templateRoot
        New-Item -Path $script:repoRoot -ItemType Directory -Force | Out-Null
    }

    It 'copies the shared and strategy files for the requested branch strategy' {
        $result = New-PlatformWorkflow -InputFolder $script:templateRoot -OutputFolder $script:repoRoot -BranchStrategy 'github'

        $result.Success | Should -BeTrue
        $result.BranchStrategy | Should -Be 'github'
        $result.Environments | Should -Be @('dev', 'prd')
        $result.CopiedFiles.Count | Should -Be 3

        Test-Path (Join-Path $script:repoRoot '.github/actions/setup-platform-kite/action.yml') | Should -BeTrue
        Test-Path (Join-Path $script:repoRoot '.github/workflows/platform-provision.yml') | Should -BeTrue
        Test-Path (Join-Path $script:repoRoot '.github/workflows/platform-cd.yml') | Should -BeTrue
        Test-Path (Join-Path $script:repoRoot '.github/workflows/platform-release.yml') | Should -BeFalse
    }

    It 'resolves the branch strategy from global-config.jsonc when not specified' {
        $globalConfigPath = Join-Path $script:repoRoot 'config/global-config.jsonc'
        New-TestGlobalConfig -Path $globalConfigPath -BranchStrategy 'release'

        $result = New-PlatformWorkflow -InputFolder $script:templateRoot -OutputFolder $script:repoRoot -GlobalConfigPath $globalConfigPath

        $result.BranchStrategy | Should -Be 'release'
        Test-Path (Join-Path $script:repoRoot '.github/workflows/platform-release.yml') | Should -BeTrue
        Test-Path (Join-Path $script:repoRoot '.github/workflows/platform-cd.yml') | Should -BeFalse
    }

    It 'skips existing files unless -Force is specified' {
        New-PlatformWorkflow -InputFolder $script:templateRoot -OutputFolder $script:repoRoot -BranchStrategy 'github' | Out-Null

        $destination = Join-Path $script:repoRoot '.github/workflows/platform-cd.yml'
        'local change' | Set-Content -Path $destination

        $result = New-PlatformWorkflow -InputFolder $script:templateRoot -OutputFolder $script:repoRoot -BranchStrategy 'github' -WarningAction SilentlyContinue

        $result.CopiedFiles.Count | Should -Be 0
        $result.SkippedFiles.Count | Should -Be 3
        Get-Content -Path $destination -Raw | Should -Match 'local change'

        New-PlatformWorkflow -InputFolder $script:templateRoot -OutputFolder $script:repoRoot -BranchStrategy 'github' -Force | Out-Null
        Get-Content -Path $destination -Raw | Should -Not -Match 'local change'
    }

    It 'does not copy anything when -WhatIf is specified' -Skip {
        New-PlatformWorkflow -InputFolder $script:templateRoot -OutputFolder $script:repoRoot -BranchStrategy 'github' -WhatIf | Out-Null

        Test-Path (Join-Path $script:repoRoot '.github') | Should -BeFalse
    }

    It 'fails for a strategy that is not declared in the manifest' {
        { New-PlatformWorkflow -InputFolder $script:templateRoot -OutputFolder $script:repoRoot -BranchStrategy 'trunk' } |
            Should -Throw '*Available strategies*'
    }

    It 'fails when the manifest is missing' {
        $emptyRoot = Join-Path $TestDrive ('empty-' + [guid]::NewGuid().ToString('N'))
        New-Item -Path $emptyRoot -ItemType Directory -Force | Out-Null

        { New-PlatformWorkflow -InputFolder $emptyRoot -OutputFolder $script:repoRoot -BranchStrategy 'github' } |
            Should -Throw '*Workflow manifest not found*'
    }

    It 'rejects a manifest destination that escapes the output folder' {
        $manifest = @'
{
  "templateVersion": "1.0.0",
  "shared": [],
  "strategies": {
    "github": {
      "environments": [ "dev" ],
      "files": [
        { "source": "github/workflows/github/platform-cd.yml", "destination": "../outside/platform-cd.yml" }
      ]
    }
  }
}
'@
        New-TestTemplateFolder -Root $script:templateRoot -Manifest $manifest

        { New-PlatformWorkflow -InputFolder $script:templateRoot -OutputFolder $script:repoRoot -BranchStrategy 'github' } |
            Should -Throw "*relative path without '..' segments*"
    }
}
