$ErrorActionPreference = 'Stop'

Describe 'New-PlatformConfigStructure' {
  BeforeAll {
    $modulePath = if ($env:MODULE_UNDER_TEST) {
      Convert-Path $env:MODULE_UNDER_TEST
    } else {
      Convert-Path (Join-Path (Get-Location) 'src\bin\Debug\netstandard2.0\MSCKite.Azure.Platform.dll')
    }

    Import-Module $modulePath -Force

    # CI imports the stamped manifest (a real version), a local run may import the bare DLL (no version to pin)
    $module = Get-Module -Name 'MSCKite.Azure.Platform'
    $script:ExpectedKiteVersion = if ($module.PrivateData -is [hashtable] -and $module.Version.Major + $module.Version.Minor + [Math]::Max($module.Version.Build, 0) -gt 0) {
      $version = '{0}.{1}.{2}' -f $module.Version.Major, $module.Version.Minor, [Math]::Max($module.Version.Build, 0)
      if ($module.PrivateData.PSData.Prerelease) { "$version-$($module.PrivateData.PSData.Prerelease)" } else { $version }
    } else {
      ''
    }

    function Get-KiteVersion([string] $Path) {
      (Get-Content -Path $Path -Raw | ConvertFrom-Json).kiteVersion
    }
  }

  AfterAll {
    Remove-Module MSCKite.Azure.Platform -ErrorAction SilentlyContinue
  }

  It 'pins kiteVersion to the loaded module version in the built-in template' {
    $output = Join-Path $TestDrive 'config-builtin'

    New-PlatformConfigStructure -OutputFolder $output | Out-Null

    Get-KiteVersion (Join-Path $output 'global-config.jsonc') | Should -BeExactly $script:ExpectedKiteVersion
  }

  It 'pins an empty kiteVersion in a downloaded template' {
    $templates = Join-Path $TestDrive 'templates-empty'
    $output = Join-Path $TestDrive 'config-empty'
    New-Item -Path $templates -ItemType Directory | Out-Null
    "{`n  `"templateVersion`": `"1.1.0`",`n  `"kiteVersion`": `"`", // comment`n  `"tenantId`": `"`"`n}" | Set-Content -Path (Join-Path $templates 'global-config.jsonc')

    New-PlatformConfigStructure -InputFolder $templates -OutputFolder $output | Out-Null

    Get-KiteVersion (Join-Path $output 'global-config.jsonc') | Should -BeExactly $script:ExpectedKiteVersion
  }

  It 'points $schema at the release tag of the loaded module, or keeps main without a release version' {
    $output = Join-Path $TestDrive 'config-schema'

    New-PlatformConfigStructure -OutputFolder $output | Out-Null

    $expectedRef = if ($script:ExpectedKiteVersion) { "refs/tags/v$($script:ExpectedKiteVersion)" } else { 'refs/heads/main' }
    (Get-Content -Path (Join-Path $output 'global-config.jsonc') -Raw | ConvertFrom-Json).'$schema' |
      Should -BeExactly "https://raw.githubusercontent.com/msckite/az-platform-kite/$expectedRef/schemas/global-config.schema.json"
  }

  It 'keeps a kiteVersion the template already sets' {
    $templates = Join-Path $TestDrive 'templates-pinned'
    $output = Join-Path $TestDrive 'config-pinned'
    New-Item -Path $templates -ItemType Directory | Out-Null
    '{ "templateVersion": "1.1.0", "kiteVersion": "9.9.9" }' | Set-Content -Path (Join-Path $templates 'global-config.jsonc')

    New-PlatformConfigStructure -InputFolder $templates -OutputFolder $output | Out-Null

    Get-KiteVersion (Join-Path $output 'global-config.jsonc') | Should -BeExactly '9.9.9'
  }
}
