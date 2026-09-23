$ErrorActionPreference = 'Stop'

Describe 'Set-PlatformResourceGroup' {
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

        # Minimal, fully valid global-config.jsonc content shared by every test that needs to get past config loading
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
        '{"templateVersion":"1.0.0","resourceGroups":[]}' | Set-Content -Path $platform

        { Set-PlatformResourceGroup -GlobalConfigPath $global -PlatformConfigPath $platform } | Should -Throw '*Global config file not found*'
    }

    It 'throws when the platform config does not have a resourceGroups array' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-noarray.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        '{"templateVersion":"1.0.0","foo":[]}' | Set-Content -Path $platform

        { Set-PlatformResourceGroup -GlobalConfigPath $global -PlatformConfigPath $platform } | Should -Throw '*must contain a "resourceGroups" array*'
    }

    It 'throws when the platform config templateVersion is missing' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-noversion.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        '{"resourceGroups":[]}' | Set-Content -Path $platform

        { Set-PlatformResourceGroup -GlobalConfigPath $global -PlatformConfigPath $platform } | Should -Throw '*templateVersion*'
    }

    It 'throws on a duplicate resource group id' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-dup.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        @'
{
  "templateVersion": "1.0.0",
  "resourceGroups": [
    { "id": "dev", "name": "rg-dev", "location": "westeurope" },
    { "id": "dev", "name": "rg-dev2", "location": "westeurope" }
  ]
}
'@ | Set-Content -Path $platform

        { Set-PlatformResourceGroup -GlobalConfigPath $global -PlatformConfigPath $platform } | Should -Throw '*Duplicate resource group id*'
    }

    It 'throws when a resource group is missing a name' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-noname.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        '{"templateVersion":"1.0.0","resourceGroups":[{"id":"dev","location":"westeurope"}]}' | Set-Content -Path $platform

        { Set-PlatformResourceGroup -GlobalConfigPath $global -PlatformConfigPath $platform } | Should -Throw '*must have a non-empty "name"*'
    }

    It 'reports an unresolved placeholder in a resource group name' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-badplaceholder.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        '{"templateVersion":"1.0.0","resourceGroups":[{"id":"dev","name":"rg-${bogus}","location":"westeurope"}]}' | Set-Content -Path $platform

        { Set-PlatformResourceGroup -GlobalConfigPath $global -PlatformConfigPath $platform -ErrorAction Stop } | Should -Throw '*Unresolved placeholder*bogus*'
    }

    It 'reports an unresolved placeholder in a resource group tag value' {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-badtag.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        @'
{
  "templateVersion": "1.0.0",
  "resourceGroups": [
    { "id": "dev", "name": "rg-${uniqueId}${serviceShort}-dev", "location": "westeurope", "tags": { "owner": "${bogus}" } }
  ]
}
'@ | Set-Content -Path $platform

        { Set-PlatformResourceGroup -GlobalConfigPath $global -PlatformConfigPath $platform -ErrorAction Stop } | Should -Throw '*Unresolved placeholder*bogus*'
    }

    It 'resolves a fully valid config without error under -WhatIf' -Skip {
        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-whatif.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        '{"templateVersion":"1.0.0","resourceGroups":[{"id":"dev","name":"rg-${uniqueId}${serviceShort}-dev","location":"${location}","tags":{"service":"${serviceShort}"}}]}' | Set-Content -Path $platform

        { Set-PlatformResourceGroup -GlobalConfigPath $global -PlatformConfigPath $platform -WhatIf } | Should -Not -Throw
    }

    It 'throws when not signed in to Azure' {
        # Locally shadows the Describe-wide Get-AzContext fake for this test only, simulating no Azure sign-in
        function Get-AzContext { }

        $global = Join-Path $TestDrive 'global.jsonc'
        $platform = Join-Path $TestDrive 'platform-notsignedin.jsonc'
        $script:ValidGlobalConfig | Set-Content -Path $global
        '{"templateVersion":"1.0.0","resourceGroups":[{"id":"dev","name":"rg-dev","location":"westeurope"}]}' | Set-Content -Path $platform

        { Set-PlatformResourceGroup -GlobalConfigPath $global -PlatformConfigPath $platform } | Should -Throw '*Not signed in to Azure*'
    }
}
