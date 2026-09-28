$ErrorActionPreference = 'Stop'

# These tests clone the public msckite/az-platform-kite repository, so they need network access and git on PATH
Describe 'Get-PlatformTemplate' {
  BeforeAll {
    $modulePath = if ($env:MODULE_UNDER_TEST) {
      Convert-Path $env:MODULE_UNDER_TEST
    } else {
      Convert-Path (Join-Path (Get-Location) 'src\bin\Debug\netstandard2.0\MSCKite.Azure.Platform.dll')
    }

    Import-Module $modulePath -Force
  }

  AfterAll {
    Remove-Module MSCKite.Azure.Platform -ErrorAction SilentlyContinue
  }

  It 'resolves -Branch latest to a stable release tag and pins schema references to it' {
    $output = Join-Path $TestDrive 'latest'

    $result = Get-PlatformTemplate -IncludedFolders 'templates', 'schemas' -OutputFolder $output -Branch latest

    $result.Branch | Should -Match '^v\d+\.\d+\.\d+$'
    $tagPrefix = "https://raw.githubusercontent.com/msckite/az-platform-kite/refs/tags/$($result.Branch)/"
    Get-Content -Path (Join-Path $output 'templates/global-config.jsonc') -Raw | Should -BeLike "*$tagPrefix*"
    Get-Content -Path (Join-Path $output 'schemas/global-config.schema.json') -Raw | Should -BeLike "*$tagPrefix*"
    Get-ChildItem -Path $output -Recurse -Include '*.json', '*.jsonc' | Select-String -Pattern 'refs/heads/main' | Should -BeNullOrEmpty
  }

  It 'leaves schema references untouched when downloading a branch' {
    $output = Join-Path $TestDrive 'main'

    $result = Get-PlatformTemplate -IncludedFolders 'templates' -OutputFolder $output -Branch main

    $result.Branch | Should -BeExactly 'main'
    Get-Content -Path (Join-Path $output 'templates/global-config.jsonc') -Raw | Should -BeLike '*refs/heads/main/schemas/global-config.schema.json*'
  }

  It 'reports a clear error for a release tag that does not exist' {
    { Get-PlatformTemplate -OutputFolder (Join-Path $TestDrive 'missing') -Branch 'v0.0.999' } | Should -Throw '*Failed to clone*v0.0.999*'
  }
}
