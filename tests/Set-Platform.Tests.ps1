$ErrorActionPreference = 'Stop'

Describe 'Set-Platform' {
    BeforeAll {
        $modulePath = if ($env:MODULE_UNDER_TEST) {
            Convert-Path $env:MODULE_UNDER_TEST
        } else {
            Convert-Path (Join-Path (Get-Location) 'src\bin\Debug\netstandard2.0\MSCKite.Azure.Platform.dll')
        }

        Import-Module $modulePath -Force

        # Shadows the real Az.Accounts cmdlet so the cmdlet's sign-in check passes without a real Azure login
        function Get-AzContext {
            [PSCustomObject]@{
                Account      = [PSCustomObject]@{ Id = 'tester@example.com' }
                Tenant       = [PSCustomObject]@{ Id = '11111111-1111-1111-1111-111111111111' }
                Subscription = [PSCustomObject]@{ Id = '22222222-2222-2222-2222-222222222222'; Name = 'Test Subscription' }
                Environment  = [PSCustomObject]@{ Name = 'AzureCloud' }
            }
        }

        # Shadow the Azure and Entra lookups so every resource reads as missing, like a fresh subscription, without a real login.
        # The repository lookup and the phase 4 reads still call the real GitHub CLI (read-only, against msckite/az-platform-kite).
        function Get-AzResourceGroup { }
        function Get-AzADGroup { }
        function Get-AzUserAssignedIdentity { }
        function Get-AzRoleAssignment { }

        $script:SubscriptionId = '22222222-2222-2222-2222-222222222222'

        $script:ValidGlobalConfig = @'
{
  "templateVersion": "2.0.0",
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

        # Writes a config set to its own folder and returns the paths; each part can be replaced to break one phase
        function Initialize-TestConfigSet {
            param(
                [string] $Name,
                [string] $GlobalConfig = $script:ValidGlobalConfig,
                [string] $ResourceGroupName = 'rg-${uniqueId}${serviceShort}-dev',
                [string] $SecurityGroupDisplayName = 'SG ${displayName} - Developers',
                [string] $IdentityName = 'id-${uniqueId}${serviceShort}-${tool}-${environmentCode}'
            )

            $root = Join-Path $TestDrive $Name
            $platform = Join-Path $root 'platform'
            New-Item -Path $platform -ItemType Directory -Force | Out-Null

            $GlobalConfig | Set-Content -Path (Join-Path $root 'global-config.jsonc')

            @"
{
  "templateVersion": "2.0.0",
  "resourceGroups": [{ "id": "dev", "name": "$ResourceGroupName", "location": "`${location}" }]
}
"@ | Set-Content -Path (Join-Path $platform 'resource-groups.jsonc')

            @"
{
  "templateVersion": "2.0.0",
  "securityGroups": [
    {
      "displayName": "$SecurityGroupDisplayName",
      "mailNickname": "sg-`${uniqueId}`${serviceShort}-devs",
      "description": "Developers",
      "roleAssignments": [{ "role": "Contributor", "resourceGroupId": "dev" }]
    }
  ]
}
"@ | Set-Content -Path (Join-Path $platform 'security-groups.jsonc')

            @"
{
  "templateVersion": "2.0.0",
  "environments": [
    {
      "environmentCode": "dev",
      "resourceGroupId": "dev",
      "pipelines": [
        {
          "purpose": "infra",
          "userAssignedIdentity": {
            "name": "$IdentityName",
            "federatedCredential": {
              "name": "fic-`${uniqueId}`${serviceShort}-`${tool}-`${environmentCode}",
              "issuer": "https://token.actions.githubusercontent.com",
              "subjectType": "environment",
              "audiences": ["api://AzureADTokenExchange"]
            },
            "roleAssignments": [{ "role": "Contributor", "resourceGroupId": "dev" }]
          },
          "githubEnvironment": {
            "name": "msckite-tests-zzz-`${environmentCode}",
            "secrets": [{ "name": "AZURE_CLIENT_ID", "value": "`${clientId}" }],
            "variables": []
          }
        }
      ]
    }
  ]
}
"@ | Set-Content -Path (Join-Path $platform 'environments.jsonc')

            [PSCustomObject]@{
                GlobalConfigPath     = Join-Path $root 'global-config.jsonc'
                PlatformConfigFolder = $platform
            }
        }

        # Runs a -WhatIf deployment and captures its errors and warnings instead of failing on them
        function Invoke-TestDeployment {
            param([Parameter(Mandatory)] $ConfigSet)

            $errors = $null
            $warnings = $null
            $result = Set-Platform -GlobalConfigPath $ConfigSet.GlobalConfigPath -PlatformConfigFolder $ConfigSet.PlatformConfigFolder -WhatIf `
                -ErrorAction SilentlyContinue -ErrorVariable errors -WarningAction SilentlyContinue -WarningVariable warnings

            [PSCustomObject]@{
                Result   = $result
                Errors   = @($errors)
                Warnings = @($warnings)
            }
        }
    }

    AfterAll {
        Remove-Module MSCKite.Azure.Platform -ErrorAction SilentlyContinue
    }

    It 'is exported and supports -WhatIf and -Confirm' {
        $command = Get-Command Set-Platform

        $command.Parameters.Keys | Should -Contain 'GlobalConfigPath'
        $command.Parameters.Keys | Should -Contain 'PlatformConfigFolder'
        $command.Parameters.Keys | Should -Contain 'WhatIf'
        $command.Parameters.Keys | Should -Contain 'Confirm'
    }

    Context 'before any phase runs' {
        It 'throws when the global config file does not exist' {
            $configSet = Initialize-TestConfigSet -Name 'missing-global'
            Remove-Item $configSet.GlobalConfigPath

            { Set-Platform -GlobalConfigPath $configSet.GlobalConfigPath -PlatformConfigFolder $configSet.PlatformConfigFolder } | Should -Throw '*Global config file not found*'
        }

        It 'names the platform config file that is missing' {
            $configSet = Initialize-TestConfigSet -Name 'missing-security-groups'
            Remove-Item (Join-Path $configSet.PlatformConfigFolder 'security-groups.jsonc')

            { Set-Platform -GlobalConfigPath $configSet.GlobalConfigPath -PlatformConfigFolder $configSet.PlatformConfigFolder } | Should -Throw '*Config file not found*security-groups.jsonc*'
        }

        It 'loads every config file before changing anything' {
            $configSet = Initialize-TestConfigSet -Name 'invalid-environments'
            '{"templateVersion":"2.0.0"}' | Set-Content -Path (Join-Path $configSet.PlatformConfigFolder 'environments.jsonc')

            { Set-Platform -GlobalConfigPath $configSet.GlobalConfigPath -PlatformConfigFolder $configSet.PlatformConfigFolder } | Should -Throw '*must contain an "environments" array*'
        }

        It 'throws when not signed in to Azure' {
            # Locally shadows the Describe-wide Get-AzContext fake for this test only, simulating no Azure sign-in
            function Get-AzContext { }
            $configSet = Initialize-TestConfigSet -Name 'not-signed-in'

            { Set-Platform -GlobalConfigPath $configSet.GlobalConfigPath -PlatformConfigFolder $configSet.PlatformConfigFolder } | Should -Throw '*Not signed in to Azure*'
        }

        It 'throws when the active Azure subscription differs from global config' {
            $configSet = Initialize-TestConfigSet -Name 'subscription-mismatch' `
                -GlobalConfig ($script:ValidGlobalConfig -replace '"subscriptionId": "[^"]+"', '"subscriptionId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"')

            { Set-Platform -GlobalConfigPath $configSet.GlobalConfigPath -PlatformConfigFolder $configSet.PlatformConfigFolder } | Should -Throw '*active Azure subscription*does not match*'
        }

        It 'throws when the GitHub repository cannot be read' {
            $configSet = Initialize-TestConfigSet -Name 'repository-missing' `
                -GlobalConfig ($script:ValidGlobalConfig -replace '"repository": "az-platform-kite"', '"repository": "does-not-exist-msckite-tests-zzz"')

            { Set-Platform -GlobalConfigPath $configSet.GlobalConfigPath -PlatformConfigFolder $configSet.PlatformConfigFolder } | Should -Throw '*Failed to read repository*does-not-exist-msckite-tests-zzz*'
        }
    }

    Context 'on a fresh subscription under -WhatIf' {
        BeforeAll {
            $script:Run = Invoke-TestDeployment -ConfigSet (Initialize-TestConfigSet -Name 'fresh')
            $script:ExpectedResourceId = "/subscriptions/$script:SubscriptionId/resourceGroups/rg-testidtst-dev"
        }

        It 'runs every phase without errors' {
            $script:Run.Errors | Should -BeNullOrEmpty
            $script:Run.Result.IsWhatIf | Should -BeTrue
            $script:Run.Result.StoppedAtPhase | Should -BeNullOrEmpty
            $script:Run.Result.PhasesWithErrors | Should -BeNullOrEmpty
        }

        It 'computes the ResourceId of a resource group it would create' {
            $resourceGroup = $script:Run.Result.ResourceGroups | Select-Object -First 1

            $resourceGroup.Action | Should -Be 'WouldCreate'
            $resourceGroup.ResourceId | Should -Be $script:ExpectedResourceId
        }

        It 'scopes security group roles to the planned resource group' {
            $securityGroup = $script:Run.Result.SecurityGroups | Select-Object -First 1

            $securityGroup.Action | Should -Be 'WouldCreate'
            $securityGroup.RoleAssignments[0].Scope | Should -Be $script:ExpectedResourceId
            $securityGroup.RoleAssignments[0].Action | Should -Be 'WouldAdd'
        }

        It 'plans the identity in the planned resource group' {
            $identity = $script:Run.Result.EnvironmentIdentities | Select-Object -First 1

            $identity.Action | Should -Be 'WouldCreate'
            $identity.ResourceGroupName | Should -Be 'rg-testidtst-dev'
            $identity.ClientId | Should -BeNullOrEmpty
            $identity.RoleAssignments[0].Scope | Should -Be $script:ExpectedResourceId
        }

        It 'plans the GitHub environment with the client id of an identity that does not exist yet' {
            $environment = $script:Run.Result.GitHubEnvironments | Select-Object -First 1

            $environment.Name | Should -Be 'msckite-tests-zzz-dev'
            $environment.Action | Should -Be 'WouldCreate'
            ($environment.Secrets | Where-Object Name -EQ 'AZURE_CLIENT_ID').Action | Should -Be 'WouldCreate'
        }
    }

    Context 'when a phase reports errors' {
        It 'stops after phase 1, because every later phase needs the resource groups' {
            $run = Invoke-TestDeployment -ConfigSet (Initialize-TestConfigSet -Name 'phase1-error' -ResourceGroupName 'rg-${bogus}')

            $run.Errors.FullyQualifiedErrorId | Should -Contain 'PlatformResourceGroupUnresolvedPlaceholder,MSCKite.Azure.Platform.Commands.Platform.SetPlatform'
            $run.Warnings | Should -BeLike '*Phase 1 (resource groups) reported errors*'
            $run.Result.StoppedAtPhase | Should -Be 'ResourceGroups'
            $run.Result.PhasesWithErrors | Should -Be @('ResourceGroups')
            $run.Result.SecurityGroups | Should -BeNullOrEmpty
            $run.Result.EnvironmentIdentities | Should -BeNullOrEmpty
            $run.Result.GitHubEnvironments | Should -BeNullOrEmpty
        }

        It 'continues after phase 2, because phases 3 and 4 do not need the security groups' {
            $run = Invoke-TestDeployment -ConfigSet (Initialize-TestConfigSet -Name 'phase2-error' -SecurityGroupDisplayName 'SG ${bogus}')

            $run.Errors.FullyQualifiedErrorId | Should -Contain 'PlatformSecurityGroupUnresolvedPlaceholder,MSCKite.Azure.Platform.Commands.Platform.SetPlatform'
            $run.Result.StoppedAtPhase | Should -BeNullOrEmpty
            $run.Result.PhasesWithErrors | Should -Be @('SecurityGroups')
            $run.Result.EnvironmentIdentities | Should -HaveCount 1
            $run.Result.GitHubEnvironments | Should -HaveCount 1
        }

        It 'stops after phase 3, because phase 4 needs the identities' {
            $run = Invoke-TestDeployment -ConfigSet (Initialize-TestConfigSet -Name 'phase3-error' -IdentityName 'id-${bogus}')

            $run.Errors.FullyQualifiedErrorId | Should -Contain 'PlatformEnvironmentIdentityUnresolvedPlaceholder,MSCKite.Azure.Platform.Commands.Platform.SetPlatform'
            $run.Warnings | Should -BeLike '*Phase 3 (environment identities) reported errors*'
            $run.Result.StoppedAtPhase | Should -Be 'EnvironmentIdentities'
            $run.Result.PhasesWithErrors | Should -Be @('EnvironmentIdentities')
            $run.Result.SecurityGroups | Should -HaveCount 1
            $run.Result.GitHubEnvironments | Should -BeNullOrEmpty
        }
    }
}
