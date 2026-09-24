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
  "platform": {
    "shared": [
      { "source": "github/actions/setup-platform-kite/action.yml", "destination": ".github/actions/setup-platform-kite/action.yml" },
      { "source": "github/workflows/shared/platform-provision.yml", "destination": ".github/workflows/platform-provision.yml" }
    ],
    "files": [
      { "source": "github/workflows/platform-flow/platform-ci.yml", "destination": ".github/workflows/platform-ci.yml" },
      { "source": "github/workflows/platform-flow/platform-cd.yml", "destination": ".github/workflows/platform-cd.yml" }
    ]
  },
  "project": {
    "shared": [
      { "source": "github/actions/setup-platform-kite/action.yml", "destination": ".github/actions/setup-platform-kite/action.yml" },
      { "source": "github/workflows/shared/project-validate.yml", "destination": ".github/workflows/project-validate.yml" },
      { "source": "github/workflows/shared/project-provision.yml", "destination": ".github/workflows/project-provision.yml" }
    ],
    "strategies": {
      "github": {
        "environments": [ "dev", "prd" ],
        "files": [
          { "source": "github/workflows/github-flow/project-ci.yml", "destination": ".github/workflows/project-flow-ci.yml" }
        ]
      },
      "release": {
        "environments": [ "dev", "stg", "prd" ],
        "files": [
          { "source": "github/workflows/release-flow/project-ci.yml", "destination": ".github/workflows/project-flow-ci.yml" }
        ]
      }
    }
  }
}
'@
      }

      $files = @(
        'github/actions/setup-platform-kite/action.yml'
        'github/workflows/shared/platform-provision.yml'
        'github/workflows/platform-flow/platform-ci.yml'
        'github/workflows/platform-flow/platform-cd.yml'
        'github/workflows/shared/project-validate.yml'
        'github/workflows/shared/project-provision.yml'
        'github/workflows/github-flow/project-ci.yml'
        'github/workflows/release-flow/project-ci.yml'
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

  It 'copies fixed platform files without a branch strategy' {
    $result = New-PlatformWorkflow -InputFolder $script:templateRoot -OutputFolder $script:repoRoot -WorkflowType platform

    $result.Success | Should -BeTrue
    $result.BranchStrategy | Should -BeNullOrEmpty
    $result.CopiedFiles.Count | Should -Be 4

    Test-Path (Join-Path $script:repoRoot '.github/actions/setup-platform-kite/action.yml') | Should -BeTrue
    Test-Path (Join-Path $script:repoRoot '.github/workflows/platform-provision.yml') | Should -BeTrue
    Test-Path (Join-Path $script:repoRoot '.github/workflows/platform-cd.yml') | Should -BeTrue
    Test-Path (Join-Path $script:repoRoot '.github/workflows/platform-ci.yml') | Should -BeTrue
  }

  It 'copies only the requested workflow type' {
    $result = New-PlatformWorkflow -InputFolder $script:templateRoot -OutputFolder $script:repoRoot -BranchStrategy 'github' -WorkflowType project

    $result.WorkflowType | Should -Be 'project'
    Test-Path (Join-Path $script:repoRoot '.github/workflows/project-validate.yml') | Should -BeTrue
    Test-Path (Join-Path $script:repoRoot '.github/workflows/project-flow-ci.yml') | Should -BeTrue
    Test-Path (Join-Path $script:repoRoot '.github/workflows/platform-provision.yml') | Should -BeFalse
  }

  It 'resolves the branch strategy from global-config.jsonc when not specified' {
    $globalConfigPath = Join-Path $script:repoRoot 'config/global-config.jsonc'
    New-TestGlobalConfig -Path $globalConfigPath -BranchStrategy 'release'

    $result = New-PlatformWorkflow -InputFolder $script:templateRoot -OutputFolder $script:repoRoot -GlobalConfigPath $globalConfigPath -WorkflowType project

    $result.BranchStrategy | Should -Be 'release'
    Test-Path (Join-Path $script:repoRoot '.github/workflows/project-flow-ci.yml') | Should -BeTrue
    Test-Path (Join-Path $script:repoRoot '.github/workflows/platform-cd.yml') | Should -BeFalse
  }

  It 'skips existing files unless -Force is specified' {
    New-PlatformWorkflow -InputFolder $script:templateRoot -OutputFolder $script:repoRoot -WorkflowType platform | Out-Null

    $destination = Join-Path $script:repoRoot '.github/workflows/platform-ci.yml'
    'local change' | Set-Content -Path $destination

    $result = New-PlatformWorkflow -InputFolder $script:templateRoot -OutputFolder $script:repoRoot -WorkflowType platform -WarningAction SilentlyContinue

    $result.CopiedFiles.Count | Should -Be 0
    $result.SkippedFiles.Count | Should -Be 4
    Get-Content -Path $destination -Raw | Should -Match 'local change'

    New-PlatformWorkflow -InputFolder $script:templateRoot -OutputFolder $script:repoRoot -WorkflowType platform -Force | Out-Null
    Get-Content -Path $destination -Raw | Should -Not -Match 'local change'
  }

  It 'does not copy anything when -WhatIf is specified' -Skip {
    New-PlatformWorkflow -InputFolder $script:templateRoot -OutputFolder $script:repoRoot -WorkflowType platform -WhatIf | Out-Null

    Test-Path (Join-Path $script:repoRoot '.github') | Should -BeFalse
  }

  It 'fails for a strategy that is not declared in the manifest' {
    { New-PlatformWorkflow -InputFolder $script:templateRoot -OutputFolder $script:repoRoot -WorkflowType project -BranchStrategy 'trunk' } |
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
  "platform": {
    "files": [
      { "source": "github/workflows/platform-ci.yml", "destination": "../outside/platform-ci.yml" }
    ]
  }
}
'@
    New-TestTemplateFolder -Root $script:templateRoot -Manifest $manifest

    { New-PlatformWorkflow -InputFolder $script:templateRoot -OutputFolder $script:repoRoot -WorkflowType platform } |
      Should -Throw "*relative path without '..' segments*"
  }
}
