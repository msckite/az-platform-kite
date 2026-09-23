$ErrorActionPreference = 'Stop'

Describe 'Set-PlatformGitHubEnvironment' {
    BeforeAll {
        $modulePath = if ($env:MODULE_UNDER_TEST) {
            Convert-Path $env:MODULE_UNDER_TEST
        } else {
            Convert-Path (Join-Path (Get-Location) 'src\bin\Debug\netstandard2.0\MSCKite.Azure.Platform.dll')
        }

        Import-Module $modulePath -Force

        # Shadows the real Az.Accounts cmdlet so the cmdlet's sign-in check passes without a real Azure login;
        # the underlying Az.Resources cmdlets still run for real and no-op gracefully when unauthenticated
        function Get-AzContext {
            [PSCustomObject]@{
                Account      = [PSCustomObject]@{ Id = 'tester@example.com' }
                Tenant       = [PSCustomObject]@{ Id = '11111111-1111-1111-1111-111111111111' }
                Subscription = [PSCustomObject]@{ Id = '22222222-2222-2222-2222-222222222222'; Name = 'Test Subscription' }
                Environment  = [PSCustomObject]@{ Name = 'AzureCloud' }
            }
        }

        $script:ValidGlobalConfig = @'
{
  "templateVersion": "1.0.0",
  "tenantId": "11111111-1111-1111-1111-111111111111",
  "subscriptionId": "22222222-2222-2222-2222-222222222222",
  "uniqueId": "testid",
  "serviceShort": "tst",
  "displayName": "Test Service",
  "location": "westeurope",
  "regionCode": "weu",
  "sourceControl": { "tool": "github", "owner": "msckite", "repository": "az-platform-kite", "branchStrategy": "github" }
}
'@

        # Same shape, but with no sourceControl at all, to exercise the missing owner/repository check
        $script:GlobalConfigNoSourceControl = @'
{
  "templateVersion": "1.0.0",
  "tenantId": "11111111-1111-1111-1111-111111111111",
  "subscriptionId": "22222222-2222-2222-2222-222222222222",
  "uniqueId": "testid",
  "serviceShort": "tst",
  "displayName": "Test Service",
  "location": "westeurope",
  "regionCode": "weu"
}
'@

        # A single fully valid environment entry, used as a base that individual tests strip fields from
        $script:ValidIdentity = @'
"userAssignedIdentity": {
  "name": "id-${uniqueId}${serviceShort}-${tool}-${environmentCode}",
  "federatedCredential": {
    "name": "fic-${uniqueId}${serviceShort}-${tool}-${environmentCode}",
    "issuer": "https://token.actions.githubusercontent.com",
    "subjectType": "environment",
    "audiences": ["api://AzureADTokenExchange"]
  },
  "roleAssignments": [ { "role": "Contributor", "resourceGroupId": "dev" } ]
}
'@
    }

    AfterAll {
        Remove-Module MSCKite.Azure.Platform -ErrorAction SilentlyContinue
    }

    It 'throws when the global config file does not exist' {
        $global = Join-Path $TestDrive 'missing-global.jsonc'
        $platform = Join-Path $TestDrive 'platform.jsonc'
        '{"templateVersion":"1.0.0","resourceGroups":[],"environments":[]}' | Set-Content -Path $platform

        { Set-PlatformGitHubEnvironment -GlobalConfigPath $global -PlatformConfigPath $platform } | Should -Throw '*Global config file not found*'
    }

    It 'throws when the platform config does not have an environments array' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-noarray.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        '{"templateVersion":"1.0.0","resourceGroups":[]}' | Set-Content -Path $platform

        { Set-PlatformGitHubEnvironment -GlobalConfigPath $global -PlatformConfigPath $platform } | Should -Throw '*must contain an "environments" array*'
    }

    It 'throws when githubEnvironment is missing a name' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-nogithubname.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        @"
{
  "templateVersion": "1.0.0",
  "resourceGroups": [],
  "environments": [
    {
      "environmentCode": "dev",
      "resourceGroupId": "dev",
      $script:ValidIdentity,
      "githubEnvironment": { "secrets": [], "variables": [] }
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformGitHubEnvironment -GlobalConfigPath $global -PlatformConfigPath $platform } | Should -Throw '*githubEnvironment must have a non-empty "name"*'
    }

    It 'throws when global config has no sourceControl owner/repository' {
        $global = Join-Path $TestDrive 'global-nosc.jsonc'
        $platform = Join-Path $TestDrive 'platform-full.jsonc'
        $script:GlobalConfigNoSourceControl | Set-Content -Path $global
        @"
{
  "templateVersion": "1.0.0",
  "resourceGroups": [],
  "environments": [
    {
      "environmentCode": "dev",
      "resourceGroupId": "dev",
      $script:ValidIdentity,
      "githubEnvironment": { "name": "dev", "secrets": [], "variables": [] }
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformGitHubEnvironment -GlobalConfigPath $global -PlatformConfigPath $platform } | Should -Throw '*sourceControl.owner*sourceControl.repository*'
    }

    It 'reports a role assignment referencing a resource group that does not exist' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-missing-rg.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        @"
{
  "templateVersion": "1.0.0",
  "resourceGroups": [
    { "id": "dev", "name": "rg-does-not-exist-msckite-tests-zzz", "location": "westeurope" }
  ],
  "environments": [
    {
      "environmentCode": "dev",
      "resourceGroupId": "dev",
      $script:ValidIdentity,
      "githubEnvironment": { "name": "dev", "secrets": [], "variables": [] }
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformGitHubEnvironment -GlobalConfigPath $global -PlatformConfigPath $platform -ErrorAction Stop } | Should -Throw '*does not exist. Run Set-PlatformResourceGroup first*'
    }
}
