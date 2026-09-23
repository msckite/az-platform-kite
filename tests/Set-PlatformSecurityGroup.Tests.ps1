$ErrorActionPreference = 'Stop'

Describe 'Set-PlatformSecurityGroup' {
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
    }

    AfterAll {
        Remove-Module MSCKite.Azure.Platform -ErrorAction SilentlyContinue
    }

    It 'throws when the global config file does not exist' {
        $global = Join-Path $TestDrive 'missing-global.jsonc'
        $platform = Join-Path $TestDrive 'platform.jsonc'
        '{"templateVersion":"1.0.0","resourceGroups":[],"securityGroups":[]}' | Set-Content -Path $platform

        { Set-PlatformSecurityGroup -GlobalConfigPath $global -PlatformConfigPath $platform } | Should -Throw '*Global config file not found*'
    }

    It 'throws when the platform config does not have a securityGroups array' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-noarray.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        '{"templateVersion":"1.0.0","resourceGroups":[]}' | Set-Content -Path $platform

        { Set-PlatformSecurityGroup -GlobalConfigPath $global -PlatformConfigPath $platform } | Should -Throw '*must contain a "securityGroups" array*'
    }

    It 'throws when a security group is missing a mailNickname' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-nomail.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        '{"templateVersion":"1.0.0","resourceGroups":[],"securityGroups":[{"displayName":"Devs","roleAssignments":[{"role":"Contributor","resourceGroupId":"dev"}]}]}' | Set-Content -Path $platform

        { Set-PlatformSecurityGroup -GlobalConfigPath $global -PlatformConfigPath $platform } | Should -Throw '*must have a non-empty "mailNickname"*'
    }

    It 'throws when a security group has no roleAssignments' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-noroles.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        '{"templateVersion":"1.0.0","resourceGroups":[],"securityGroups":[{"displayName":"Devs","mailNickname":"sg-devs","roleAssignments":[]}]}' | Set-Content -Path $platform

        { Set-PlatformSecurityGroup -GlobalConfigPath $global -PlatformConfigPath $platform } | Should -Throw '*must have a non-empty "roleAssignments" array*'
    }

    It 'reports an unresolved placeholder in a security group displayName' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-badplaceholder.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        '{"templateVersion":"1.0.0","resourceGroups":[],"securityGroups":[{"displayName":"SG ${bogus}","mailNickname":"sg-devs","roleAssignments":[{"role":"Contributor","resourceGroupId":"dev"}]}]}' | Set-Content -Path $platform

        { Set-PlatformSecurityGroup -GlobalConfigPath $global -PlatformConfigPath $platform -ErrorAction Stop } | Should -Throw '*Unresolved placeholder*bogus*'
    }

    It 'reports a role assignment referencing a resource group that does not exist' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-missing-rg.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        @'
{
  "templateVersion": "1.0.0",
  "resourceGroups": [
    { "id": "dev", "name": "rg-does-not-exist-msckite-tests-zzz", "location": "westeurope" }
  ],
  "securityGroups": [
    { "displayName": "SG Devs", "mailNickname": "sg-msckite-tests-devs", "roleAssignments": [ { "role": "Contributor", "resourceGroupId": "dev" } ] }
  ]
}
'@ | Set-Content -Path $platform

        { Set-PlatformSecurityGroup -GlobalConfigPath $global -PlatformConfigPath $platform -ErrorAction Stop } | Should -Throw '*does not exist. Run Set-PlatformResourceGroup first*'
    }

    It 'throws when the active Azure tenant differs from global config' {
        $global = Join-Path $TestDrive 'global-tenant-mismatch.jsonc'
        $platform = Join-Path $TestDrive 'platform.jsonc'
        ($script:ValidGlobalConfig -replace '"tenantId": "[^"]+"', '"tenantId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"') | Set-Content -Path $global
        '{"templateVersion":"1.0.0","resourceGroups":[],"securityGroups":[]}' | Set-Content -Path $platform

        { Set-PlatformSecurityGroup -GlobalConfigPath $global -PlatformConfigPath $platform } | Should -Throw '*active Azure tenant*does not match*'
    }

    It 'throws when the active Azure subscription differs from global config' {
        $global = Join-Path $TestDrive 'global-subscription-mismatch.jsonc'
        $platform = Join-Path $TestDrive 'platform.jsonc'
        ($script:ValidGlobalConfig -replace '"subscriptionId": "[^"]+"', '"subscriptionId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"') | Set-Content -Path $global
        '{"templateVersion":"1.0.0","resourceGroups":[],"securityGroups":[]}' | Set-Content -Path $platform

        { Set-PlatformSecurityGroup -GlobalConfigPath $global -PlatformConfigPath $platform } | Should -Throw '*active Azure subscription*does not match*'
    }
}
