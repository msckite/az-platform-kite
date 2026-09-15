$ErrorActionPreference = 'Stop'

Describe 'Test-PlatformTemplate' {
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

    It 'reports matching template and schema versions as compatible' {
        $templatePath = Join-Path $TestDrive 'template.jsonc'
        $schemaPath = Join-Path $TestDrive 'schema.json'
        '{ "templateVersion": "1.2.3" }' | Set-Content -Path $templatePath
        '{ "schemaVersion": "1.2.3" }' | Set-Content -Path $schemaPath

        $result = Test-PlatformTemplate -TemplatePath $templatePath -SchemaPath $schemaPath

        $result.IsCompatible | Should -BeTrue
        $result.IsUpdateAvailable | Should -BeFalse
        $result.TemplateVersion | Should -Be '1.2.3'
    }

    It 'reports an incompatible template and schema version pair' {
        $templatePath = Join-Path $TestDrive 'template.jsonc'
        $schemaPath = Join-Path $TestDrive 'schema.json'
        '{ "templateVersion": "1.2.3" }' | Set-Content -Path $templatePath
        '{ "schemaVersion": "2.0.0" }' | Set-Content -Path $schemaPath

        $result = Test-PlatformTemplate -TemplatePath $templatePath -SchemaPath $schemaPath

        $result.IsCompatible | Should -BeFalse
        $result.CompatibilityMessage | Should -Match 'differ'
    }

    It 'reports when a newer template is available' {
        $templatePath = Join-Path $TestDrive 'template.jsonc'
        $schemaPath = Join-Path $TestDrive 'schema.json'
        $latestTemplatePath = Join-Path $TestDrive 'latest-template.jsonc'
        '{ "templateVersion": "1.2.3" }' | Set-Content -Path $templatePath
        '{ "schemaVersion": "1.2.3" }' | Set-Content -Path $schemaPath
        '{ "templateVersion": "1.3.0" }' | Set-Content -Path $latestTemplatePath

        $result = Test-PlatformTemplate -TemplatePath $templatePath -SchemaPath $schemaPath -LatestTemplatePath $latestTemplatePath

        $result.IsUpdateAvailable | Should -BeTrue
        $result.LatestTemplateVersion | Should -Be '1.3.0'
    }

    It 'rejects a missing or invalid template version' {
        $templatePath = Join-Path $TestDrive 'template.jsonc'
        $schemaPath = Join-Path $TestDrive 'schema.json'
        '{ "templateVersion": "1.2" }' | Set-Content -Path $templatePath
        '{ "schemaVersion": "1.2.3" }' | Set-Content -Path $schemaPath

        { Test-PlatformTemplate -TemplatePath $templatePath -SchemaPath $schemaPath } | Should -Throw '*Expected Major.Minor.Patch SemVer*'
    }
}
