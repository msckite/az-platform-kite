$ErrorActionPreference = 'Stop'

Describe 'Set-PlatformEnvironmentIdentity' {
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

        # A second identity/environment pair, used to test environments with an infra and a workload pipeline
        $script:ValidWorkloadIdentity = @'
"userAssignedIdentity": {
  "name": "id-${uniqueId}${serviceShort}-${tool}-${environmentCode}-workload",
  "federatedCredential": {
    "name": "fic-${uniqueId}${serviceShort}-${tool}-${environmentCode}-workload",
    "issuer": "https://token.actions.githubusercontent.com",
    "subjectType": "environment",
    "audiences": ["api://AzureADTokenExchange"]
  },
  "roleAssignments": [ { "role": "Contributor", "resourceGroupId": "dev" } ]
}
'@

        $script:ValidWorkloadGitHubEnvironment = @'
"githubEnvironment": {
  "name": "${environmentCode}-workload",
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

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*Global config file not found*'
    }

    It 'reads each phase from its own config file and names the file that is wrong' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $resourceGroups = Join-Path $TestDrive 'split-resource-groups.jsonc'
        $environments = Join-Path $TestDrive 'split-environments.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        '{"templateVersion":"2.0.0","resourceGroups":[]}' | Set-Content -Path $resourceGroups
        '{"templateVersion":"2.0.0"}' | Set-Content -Path $environments

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -ResourceGroupsConfigPath $resourceGroups -EnvironmentsConfigPath $environments } | Should -Throw '*split-environments.jsonc*environments*'
    }

    It 'throws when the platform config does not have an environments array' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-noarray.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        '{"templateVersion":"1.0.0","resourceGroups":[]}' | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*must contain an "environments" array*'
    }

    It 'throws when a pipeline is missing userAssignedIdentity' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-noidentity.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        "{`"templateVersion`":`"1.0.0`",`"resourceGroups`":[],`"environments`":[{`"environmentCode`":`"dev`",`"resourceGroupId`":`"dev`",`"pipelines`":[{`"purpose`":`"infra`",$script:ValidGitHubEnvironment}]}]}" | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*must have a "userAssignedIdentity" object*'
    }

    It 'throws when a pipeline is missing githubEnvironment' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-nogithub.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        "{`"templateVersion`":`"1.0.0`",`"resourceGroups`":[],`"environments`":[{`"environmentCode`":`"dev`",`"resourceGroupId`":`"dev`",`"pipelines`":[{`"purpose`":`"infra`",$script:ValidIdentity}]}]}" | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*must have a "githubEnvironment" object*'
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
      "pipelines": [ { "purpose": "infra",
      "userAssignedIdentity": { "name": "id-dev", "roleAssignments": [ { "role": "Contributor", "resourceGroupId": "dev" } ] },
      $script:ValidGitHubEnvironment
      } ]
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*must have a "federatedCredential" object*'
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
      "pipelines": [ { "purpose": "infra",
      "userAssignedIdentity": {
        "name": "id-dev",
        "federatedCredential": { "name": "fic-dev", "issuer": "https://token.actions.githubusercontent.com", "subjectType": "environment", "audiences": [] },
        "roleAssignments": [ { "role": "Contributor", "resourceGroupId": "dev" } ]
      },
      $script:ValidGitHubEnvironment
      } ]
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*must have a non-empty "audiences" array*'
    }

    It 'throws when a federatedCredential leaves out its issuer and there are no defaults' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-nodefaults.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        @"
{
  "templateVersion": "2.0.0",
  "resourceGroups": [],
  "environments": [
    {
      "environmentCode": "dev",
      "resourceGroupId": "dev",
      "pipelines": [ { "purpose": "infra",
      "userAssignedIdentity": {
        "name": "id-dev",
        "federatedCredential": { "name": "fic-dev" },
        "roleAssignments": [ { "role": "Contributor", "resourceGroupId": "dev" } ]
      },
      $script:ValidGitHubEnvironment
      } ]
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*non-empty "name", "issuer", and "subjectType"*defaults.federatedCredential*'
    }

    It 'fills a federatedCredential from the defaults, including workload identities' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-defaults.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        @"
{
  "templateVersion": "2.0.0",
  "defaults": {
    "federatedCredential": {
      "issuer": "https://token.actions.githubusercontent.com",
      "subjectType": "environment",
      "audiences": ["api://AzureADTokenExchange"]
    }
  },
  "resourceGroups": [
    { "id": "dev", "name": "rg-does-not-exist-msckite-tests-zzz", "location": "westeurope" }
  ],
  "environments": [
    {
      "environmentCode": "dev",
      "resourceGroupId": "dev",
      "pipelines": [ { "purpose": "infra",
      "userAssignedIdentity": {
        "name": "id-dev",
        "federatedCredential": { "name": "fic-dev" },
        "roleAssignments": [ { "role": "Contributor", "resourceGroupId": "dev" } ]
      },
      $script:ValidGitHubEnvironment
      },
      { "purpose": "workload",
      "userAssignedIdentity": {
        "name": "id-dev-workload",
        "federatedCredential": { "name": "fic-dev-workload" },
        "roleAssignments": [ { "role": "Contributor", "resourceGroupId": "dev" } ]
      },
      "githubEnvironment": { "name": "dev-workload" }
      } ]
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform -ErrorAction Stop } | Should -Throw '*does not exist. Run Set-PlatformResourceGroup first*'
    }

    It 'lets a federatedCredential override a default' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-defaults-override.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        @"
{
  "templateVersion": "2.0.0",
  "defaults": { "federatedCredential": { "issuer": "https://token.actions.githubusercontent.com", "subjectType": "environment", "audiences": ["api://AzureADTokenExchange"] } },
  "resourceGroups": [],
  "environments": [
    {
      "environmentCode": "dev",
      "resourceGroupId": "dev",
      "pipelines": [ { "purpose": "infra",
      "userAssignedIdentity": {
        "name": "id-dev",
        "federatedCredential": { "name": "fic-dev", "audiences": [] },
        "roleAssignments": [ { "role": "Contributor", "resourceGroupId": "dev" } ]
      },
      $script:ValidGitHubEnvironment
      } ]
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*must have a non-empty "audiences" array*'
    }

    It 'throws when defaults is not an object' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-baddefaults.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        '{"templateVersion":"2.0.0","defaults":[],"resourceGroups":[],"environments":[]}' | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*"defaults" property that must be an object*'
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
      "pipelines": [ { "purpose": "infra",
      "userAssignedIdentity": {
        "name": "id-dev",
        "federatedCredential": { "name": "fic-dev", "issuer": "https://token.actions.githubusercontent.com", "subjectType": "environment", "audiences": ["api://AzureADTokenExchange"] },
        "roleAssignments": []
      },
      $script:ValidGitHubEnvironment
      } ]
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*userAssignedIdentity must have a non-empty "roleAssignments" array*'
    }

    It 'throws when a role assignment sets neither resourceGroupId nor scope' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-noscope.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        @"
{
  "templateVersion": "1.0.0",
  "resourceGroups": [],
  "environments": [
    {
      "environmentCode": "dev",
      "resourceGroupId": "dev",
      "pipelines": [ { "purpose": "infra",
      "userAssignedIdentity": {
        "name": "id-dev",
        "federatedCredential": { "name": "fic-dev", "issuer": "https://token.actions.githubusercontent.com", "subjectType": "environment", "audiences": ["api://AzureADTokenExchange"] },
        "roleAssignments": [ { "role": "Contributor" } ]
      },
      $script:ValidGitHubEnvironment
      } ]
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*must set exactly one of "resourceGroupId" or "scope"*'
    }

    It 'throws when a role assignment scope is not ''subscription''' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-badscope.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        @"
{
  "templateVersion": "1.0.0",
  "resourceGroups": [],
  "environments": [
    {
      "environmentCode": "dev",
      "resourceGroupId": "dev",
      "pipelines": [ { "purpose": "infra",
      "userAssignedIdentity": {
        "name": "id-dev",
        "federatedCredential": { "name": "fic-dev", "issuer": "https://token.actions.githubusercontent.com", "subjectType": "environment", "audiences": ["api://AzureADTokenExchange"] },
        "roleAssignments": [ { "role": "Contributor", "scope": "managementGroup" } ]
      },
      $script:ValidGitHubEnvironment
      } ]
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*unsupported "scope" value*'
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
      "pipelines": [ { "purpose": "infra",
      $script:ValidIdentity,
      $script:ValidGitHubEnvironment
      } ]
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform -ErrorAction Stop } | Should -Throw '*does not exist. Run Set-PlatformResourceGroup first*'
    }

    It 'throws when the active Azure tenant differs from global config' {
        $global = Join-Path $TestDrive 'global-tenant-mismatch.jsonc'
        $platform = Join-Path $TestDrive 'platform.jsonc'
        ($script:ValidGlobalConfig -replace '"tenantId": "[^"]+"', '"tenantId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"') | Set-Content -Path $global
        '{"templateVersion":"1.0.0","resourceGroups":[],"environments":[]}' | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*active Azure tenant*does not match*'
    }

    It 'throws when the active Azure subscription differs from global config' {
        $global = Join-Path $TestDrive 'global-subscription-mismatch.jsonc'
        $platform = Join-Path $TestDrive 'platform.jsonc'
        ($script:ValidGlobalConfig -replace '"subscriptionId": "[^"]+"', '"subscriptionId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"') | Set-Content -Path $global
        '{"templateVersion":"1.0.0","resourceGroups":[],"environments":[]}' | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*active Azure subscription*does not match*'
    }

    It 'reports both pipelines when an environment declares an infra and a workload pipeline' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-two-pipelines-missing-rg.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        @"
{
  "templateVersion": "2.0.0",
  "resourceGroups": [{ "id": "dev", "name": "rg-does-not-exist-msckite-tests-zzz", "location": "westeurope" }],
  "environments": [
    {
      "environmentCode": "dev",
      "resourceGroupId": "dev",
      "pipelines": [
      { "purpose": "infra", $script:ValidIdentity, $script:ValidGitHubEnvironment },
      { "purpose": "workload", $script:ValidWorkloadIdentity, $script:ValidWorkloadGitHubEnvironment }
      ]
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform -ErrorAction Stop } | Should -Throw '*does not exist. Run Set-PlatformResourceGroup first*'
    }

    It 'throws when two pipelines of an environment share a purpose' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-duplicate-purpose.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        @"
{
  "templateVersion": "2.0.0",
  "resourceGroups": [],
  "environments": [
    {
      "environmentCode": "dev",
      "resourceGroupId": "dev",
      "pipelines": [
      { "purpose": "infra", $script:ValidIdentity, $script:ValidGitHubEnvironment },
      { "purpose": "infra", $script:ValidWorkloadIdentity, $script:ValidWorkloadGitHubEnvironment }
      ]
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*more than one pipeline with purpose ''infra''*'
    }

    It 'throws when two pipelines of an environment share an identity name' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-duplicate-identity.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        @"
{
  "templateVersion": "2.0.0",
  "resourceGroups": [],
  "environments": [
    {
      "environmentCode": "dev",
      "resourceGroupId": "dev",
      "pipelines": [
      { "purpose": "infra", $script:ValidIdentity, $script:ValidGitHubEnvironment },
      { "purpose": "workload", $script:ValidIdentity, $script:ValidWorkloadGitHubEnvironment }
      ]
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*must not share the same userAssignedIdentity name*'
    }

    It 'throws when two pipelines of an environment share a GitHub environment name' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-duplicate-github.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        @"
{
  "templateVersion": "2.0.0",
  "resourceGroups": [],
  "environments": [
    {
      "environmentCode": "dev",
      "resourceGroupId": "dev",
      "pipelines": [
      { "purpose": "infra", $script:ValidIdentity, $script:ValidGitHubEnvironment },
      { "purpose": "workload", $script:ValidWorkloadIdentity, $script:ValidGitHubEnvironment }
      ]
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*must not share the same githubEnvironment name*'
    }

    It 'throws when a pipeline has no purpose' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-no-purpose.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        @"
{
  "templateVersion": "2.0.0",
  "resourceGroups": [],
  "environments": [
    {
      "environmentCode": "dev",
      "resourceGroupId": "dev",
      "pipelines": [
      { $script:ValidIdentity, $script:ValidGitHubEnvironment }
      ]
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformEnvironmentIdentity -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*pipeline without a non-empty "purpose"*'
    }
}
