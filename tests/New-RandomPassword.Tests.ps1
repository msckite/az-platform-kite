$ErrorActionPreference = 'Stop'

Describe 'New-RandomPassword' {
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

    It 'generates a 16-character complex password by default' {
        $result = New-RandomPassword

        $result.Password.Length | Should -Be 16
        $result.Password | Should -Match '[A-Z]'
        $result.Password | Should -Match '[a-z]'
        $result.Password | Should -Match '[0-9]'
        $result.Password | Should -Match '[^A-Za-z0-9]'
        $result.Complexity | Should -Be 'High'
    }

    It 'honors custom password length' {
        $result = New-RandomPassword -Length 32

        $result.Password.Length | Should -Be 32
        $result.Length | Should -Be 32
    }

    It 'excludes ambiguous characters when requested' {
        $results = New-RandomPassword -NoAmbiguousCharacters -Count 20

        ($results.Password -join '') | Should -Not -Match '[0Oo1lI5S8B]'
    }

    It 'returns SecureString output when requested' {
        $result = New-RandomPassword -AsSecureString

        $result | Should -BeOfType ([System.Security.SecureString])
    }

    It 'generates the requested number of unique passwords' {
        $results = New-RandomPassword -Count 25

        $results.Count | Should -Be 25
        ($results.Password | Select-Object -Unique).Count | Should -Be 25
    }

    It 'requires at least one enabled character set' {
        { New-RandomPassword -Uppercase:$false -Lowercase:$false -Numbers:$false -SpecialCharacters:$false } | Should -Throw
    }
}
