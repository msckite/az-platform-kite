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

        $script:ValidGitHubEnvironment = @'
"githubEnvironment": {
  "name": "${environmentCode}-infra",
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

        { Set-PlatformGitHubEnvironment -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*Global config file not found*'
    }

    It 'throws when the platform config does not have an environments array' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-noarray.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        '{"templateVersion":"1.0.0","resourceGroups":[]}' | Set-Content -Path $platform

        { Set-PlatformGitHubEnvironment -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*must contain an "environments" array*'
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
      "pipelines": [ { "purpose": "infra",
      $script:ValidIdentity,
      "githubEnvironment": { "secrets": [], "variables": [] }
      } ]
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformGitHubEnvironment -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*githubEnvironment must have a non-empty "name"*'
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
      "pipelines": [ { "purpose": "infra",
      $script:ValidIdentity,
      "githubEnvironment": { "name": "dev", "secrets": [], "variables": [] }
      } ]
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformGitHubEnvironment -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*must have a "sourceControl" object*'
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
      "githubEnvironment": { "name": "dev", "secrets": [], "variables": [] }
      } ]
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformGitHubEnvironment -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform -ErrorAction Stop } | Should -Throw '*does not exist. Run Set-PlatformResourceGroup first*'
    }

    It 'applies githubEnvironment defaults to an environment that sets only its name' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-gh-defaults.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        @"
{
  "templateVersion": "2.0.0",
  "defaults": { "githubEnvironment": { "protectionRules": { "deploymentBranchPolicy": {} } } },
  "resourceGroups": [],
  "environments": [
    {
      "environmentCode": "dev",
      "resourceGroupId": "dev",
      "pipelines": [ { "purpose": "infra",
      $script:ValidIdentity,
      "githubEnvironment": { "name": "dev" }
      } ]
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformGitHubEnvironment -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*deploymentBranchPolicy must list at least one branch or tag pattern*'
    }

    It 'lets a githubEnvironment protection rule override the default' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-gh-defaults-override.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        @"
{
  "templateVersion": "2.0.0",
  "defaults": { "githubEnvironment": { "protectionRules": { "deploymentBranchPolicy": {} } } },
  "resourceGroups": [
    { "id": "dev", "name": "rg-does-not-exist-msckite-tests-zzz", "location": "westeurope" }
  ],
  "environments": [
    {
      "environmentCode": "dev",
      "resourceGroupId": "dev",
      "pipelines": [ { "purpose": "infra",
      $script:ValidIdentity,
      "githubEnvironment": { "name": "dev", "protectionRules": { "deploymentBranchPolicy": { "branches": ["main"] } } }
      } ]
    }
  ]
}
"@ | Set-Content -Path $platform

        { Set-PlatformGitHubEnvironment -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform -ErrorAction Stop } | Should -Throw '*does not exist. Run Set-PlatformResourceGroup first*'
    }

    It 'throws when the active Azure tenant differs from global config' {
        $global = Join-Path $TestDrive 'global-tenant-mismatch.jsonc'
        $platform = Join-Path $TestDrive 'platform.jsonc'
        ($script:ValidGlobalConfig -replace '"tenantId": "[^"]+"', '"tenantId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"') | Set-Content -Path $global
        '{"templateVersion":"1.0.0","resourceGroups":[],"environments":[]}' | Set-Content -Path $platform

        { Set-PlatformGitHubEnvironment -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*active Azure tenant*does not match*'
    }

    It 'throws when the active Azure subscription differs from global config' {
        $global = Join-Path $TestDrive 'global-subscription-mismatch.jsonc'
        $platform = Join-Path $TestDrive 'platform.jsonc'
        ($script:ValidGlobalConfig -replace '"subscriptionId": "[^"]+"', '"subscriptionId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"') | Set-Content -Path $global
        '{"templateVersion":"1.0.0","resourceGroups":[],"environments":[]}' | Set-Content -Path $platform

        { Set-PlatformGitHubEnvironment -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*active Azure subscription*does not match*'
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

        { Set-PlatformGitHubEnvironment -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform -ErrorAction Stop } | Should -Throw '*does not exist. Run Set-PlatformResourceGroup first*'
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

        { Set-PlatformGitHubEnvironment -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*more than one pipeline with purpose ''infra''*'
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

        { Set-PlatformGitHubEnvironment -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*must not share the same userAssignedIdentity name*'
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

        { Set-PlatformGitHubEnvironment -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*must not share the same githubEnvironment name*'
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

        { Set-PlatformGitHubEnvironment -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw '*pipeline without a non-empty "purpose"*'
    }

    Context 'deploymentBranchPolicy' {
        BeforeAll {
            # Builds a single-environment platform config whose githubEnvironment carries the given protectionRules JSON
            function New-TestPlatformConfig {
                [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = 'Test helper that only writes fixture files to the Pester TestDrive.')]
                param(
                    [Parameter(Mandatory)][string] $Path,
                    [Parameter(Mandatory)][string] $ProtectionRules
                )

                @"
{
  "templateVersion": "1.1.0",
  "resourceGroups": [
    { "id": "prd", "name": "rg-does-not-exist-msckite-tests-zzz", "location": "westeurope" }
  ],
  "environments": [
    {
      "environmentCode": "prd",
      "resourceGroupId": "prd",
      "pipelines": [ { "purpose": "infra",
      $script:ValidIdentity,
      "githubEnvironment": { "name": "prd-infra", "protectionRules": $ProtectionRules, "secrets": [], "variables": [] }
      } ]
    }
  ]
}
"@ | Set-Content -Path $Path
            }
        }

        It 'throws when <Case>' -ForEach @(
            @{ Case = 'the policy lists no patterns'; Rules = '{ "deploymentBranchPolicy": { "branches": [], "tags": [] } }'; Message = '*must list at least one branch or tag pattern*' }
            @{ Case = 'the policy is not an object'; Rules = '{ "deploymentBranchPolicy": "main" }'; Message = '*deploymentBranchPolicy must be an object*' }
            @{ Case = 'branches is not an array'; Rules = '{ "deploymentBranchPolicy": { "branches": "main" } }'; Message = '*deploymentBranchPolicy.branches must be an array*' }
            @{ Case = 'a tag pattern is empty'; Rules = '{ "deploymentBranchPolicy": { "tags": [ " " ] } }'; Message = '*deploymentBranchPolicy.tags must only contain non-empty name patterns*' }
        ) {
            $global = Join-Path $TestDrive 'global.jsonc'
            $platform = Join-Path $TestDrive 'platform-branch-policy-invalid.jsonc'
            $script:ValidGlobalConfig | Set-Content -Path $global
            New-TestPlatformConfig -Path $platform -ProtectionRules $Rules

            { Set-PlatformGitHubEnvironment -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform } | Should -Throw $Message
        }

        It 'accepts a policy with <Case>' -ForEach @(
            @{ Case = 'branches and tags'; Rules = '{ "requiredReviewers": [], "deploymentBranchPolicy": { "branches": [ "main" ], "tags": [ "infra/*" ] } }' }
            @{ Case = 'branches only'; Rules = '{ "deploymentBranchPolicy": { "branches": [ "main" ] } }' }
            @{ Case = 'tags only'; Rules = '{ "deploymentBranchPolicy": { "tags": [ "infra/*" ] } }' }
            @{ Case = 'an explicit null (unmanaged)'; Rules = '{ "deploymentBranchPolicy": null }' }
        ) {
            $global = Join-Path $TestDrive 'global.jsonc'
            $platform = Join-Path $TestDrive 'platform-branch-policy-valid.jsonc'
            $script:ValidGlobalConfig | Set-Content -Path $global
            New-TestPlatformConfig -Path $platform -ProtectionRules $Rules

            # The resource group intentionally does not exist, so a valid policy gets past config parsing and fails only at the resolver
            { Set-PlatformGitHubEnvironment -GlobalConfigPath $global -ResourceGroupsConfigPath $platform -EnvironmentsConfigPath $platform -ErrorAction Stop } | Should -Throw '*does not exist. Run Set-PlatformResourceGroup first*'
        }
    }
}
