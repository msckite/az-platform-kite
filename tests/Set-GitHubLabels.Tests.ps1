$ErrorActionPreference = 'Stop'

Describe 'Set-GitHubLabels' {
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

    It 'requires a LabelFilePath' {
        (Get-Command Set-GitHubLabels).Parameters['LabelFilePath'].Attributes.Mandatory | Should -Contain $true
    }

    It 'throws when Owner and Repository are not provided and no defaults are set' {
        Set-GitHubDefault -Owner $null -Repository $null | Out-Null

        $path = Join-Path $TestDrive 'labels.jsonc'
        '{"labels":[]}' | Set-Content -Path $path

        { Set-GitHubLabels -LabelFilePath $path } | Should -Throw '*Owner and Repository are required*'
    }

    It 'throws when the labels file does not exist' {
        $path = Join-Path $TestDrive 'missing.jsonc'

        { Set-GitHubLabels -LabelFilePath $path -Owner 'o' -Repository 'r' } | Should -Throw '*not found*'
    }

    It 'throws when the file is neither an array nor an object with a labels array' {
        $path = Join-Path $TestDrive 'bad-shape.jsonc'
        '{"foo": []}' | Set-Content -Path $path

        { Set-GitHubLabels -LabelFilePath $path -Owner 'o' -Repository 'r' } | Should -Throw '*must be a JSON array*'
    }

    It 'throws when a label has an invalid color' {
        $path = Join-Path $TestDrive 'bad-color.jsonc'
        '[{"name":"bug","color":"red"}]' | Set-Content -Path $path

        { Set-GitHubLabels -LabelFilePath $path -Owner 'o' -Repository 'r' } | Should -Throw '*invalid color*'
    }

    It 'throws on duplicate label names' {
        $path = Join-Path $TestDrive 'dup.jsonc'
        '[{"name":"bug","color":"#ffffff"},{"name":"bug","color":"#000000"}]' | Set-Content -Path $path

        { Set-GitHubLabels -LabelFilePath $path -Owner 'o' -Repository 'r' } | Should -Throw '*Duplicate label name*'
    }

    It 'parses jsonc comments and a top-level "labels" object before contacting GitHub' {
        $path = Join-Path $TestDrive 'valid.jsonc'
        @'
// comment
{
  "labels": [
    { "name": "bug", "color": "#d73a4a", "description": "Something isn't working" }
  ]
}
'@ | Set-Content -Path $path

        # File parsing succeeds, so the failure comes from trying to list labels on a repo that doesn't exist (or gh being unavailable)
        { Set-GitHubLabels -LabelFilePath $path -Owner 'msckite' -Repository 'this-repo-does-not-exist' } | Should -Throw '*Failed to list labels*'
    }
}
