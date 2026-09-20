$ErrorActionPreference = 'Stop'

Describe 'Set-PlatformEnvironmentIdentity' {
    BeforeAll {
        $modulePath = if ($env:MODULE_UNDER_TEST) {
            Convert-Path $env:MODULE_UNDER_TEST
        } else {
            Convert-Path (Join-Path (Get-Location) 'src\bin\Debug\netstandard2.0\MSCKite.Azure.Platform.dll')
        }

        Import-Module $modulePath -Force

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

        $script:ValidGitHubEnvironment = @'
"githubEnvironment": {
  "name": "${environmentCode}",
  "secrets": [],
  "variables": []
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

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -PlatformConfigPath $platform } | Should -Throw '*Global config file not found*'
    }

    It 'throws when the platform config does not have an environments array' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-noarray.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        '{"templateVersion":"1.0.0","resourceGroups":[]}' | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -PlatformConfigPath $platform } | Should -Throw '*must contain an "environments" array*'
    }

    It 'throws when an environment is missing userAssignedIdentity' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-noidentity.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        "{`"templateVersion`":`"1.0.0`",`"resourceGroups`":[],`"environments`":[{`"environmentCode`":`"dev`",`"resourceGroupId`":`"dev`",$script:ValidGitHubEnvironment}]}" | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -PlatformConfigPath $platform } | Should -Throw '*must have a "userAssignedIdentity" object*'
    }

    It 'throws when an environment is missing githubEnvironment' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-nogithub.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        "{`"templateVersion`":`"1.0.0`",`"resourceGroups`":[],`"environments`":[{`"environmentCode`":`"dev`",`"resourceGroupId`":`"dev`",$script:ValidIdentity}]}" | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -PlatformConfigPath $platform } | Should -Throw '*must have a "githubEnvironment" object*'
    }

    It 'throws when federatedCredential is missing' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-nofederated.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        @"
{
  "templateVersion": "1.0.0",
  "resourceGroups": [],
  "environments": [
    {
      "environmentCode": "dev",
      "resourceGroupId": "dev",
      "userAssignedIdentity": { "name": "id-dev", "roleAssignments": [ { "role": "Contributor", "resourceGroupId": "dev" } ] },
      $script:ValidGitHubEnvironment
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -PlatformConfigPath $platform } | Should -Throw '*must have a "federatedCredential" object*'
    }

    It 'throws when federatedCredential audiences is empty' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-noaudiences.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        @"
{
  "templateVersion": "1.0.0",
  "resourceGroups": [],
  "environments": [
    {
      "environmentCode": "dev",
      "resourceGroupId": "dev",
      "userAssignedIdentity": {
        "name": "id-dev",
        "federatedCredential": { "name": "fic-dev", "issuer": "https://token.actions.githubusercontent.com", "subjectType": "environment", "audiences": [] },
        "roleAssignments": [ { "role": "Contributor", "resourceGroupId": "dev" } ]
      },
      $script:ValidGitHubEnvironment
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -PlatformConfigPath $platform } | Should -Throw '*must have a non-empty "audiences" array*'
    }

    It 'throws when userAssignedIdentity has no roleAssignments' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-noroles.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        @"
{
  "templateVersion": "1.0.0",
  "resourceGroups": [],
  "environments": [
    {
      "environmentCode": "dev",
      "resourceGroupId": "dev",
      "userAssignedIdentity": {
        "name": "id-dev",
        "federatedCredential": { "name": "fic-dev", "issuer": "https://token.actions.githubusercontent.com", "subjectType": "environment", "audiences": ["api://AzureADTokenExchange"] },
        "roleAssignments": []
      },
      $script:ValidGitHubEnvironment
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -PlatformConfigPath $platform } | Should -Throw '*userAssignedIdentity must have a non-empty "roleAssignments" array*'
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
      $script:ValidGitHubEnvironment
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -PlatformConfigPath $platform -ErrorAction Stop } | Should -Throw '*does not exist. Run Set-PlatformResourceGroup first*'
    }
}
