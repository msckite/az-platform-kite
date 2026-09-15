$ErrorActionPreference = 'Stop'

Describe 'New-RandomUniqueId' {
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

    It 'returns an 8-character alphanumeric string by default' {
        $result = New-RandomUniqueId

        $result | Should -BeOfType ([string])
        $result.Length | Should -Be 8
        $result | Should -Match '^[A-Za-z0-9]+$'
    }

    It 'honors valid custom lengths' -ForEach 4, 12 {
        $result = New-RandomUniqueId -Length $_

        $result.Length | Should -Be $_
        $result | Should -Match '^[A-Za-z0-9]+$'
    }

    It 'rejects lengths outside the supported range' -ForEach 3, 13 {
        { New-RandomUniqueId -Length $_ } | Should -Throw
    }

    It 'generates reasonably unique values for typical scripting use' {
        $results = 1..100 | ForEach-Object { New-RandomUniqueId }

        ($results | Select-Object -Unique).Count | Should -Be 100
    }

    It 'accepts Length from pipeline properties' {
        $results = [pscustomobject]@{ Length = 4 }, [pscustomobject]@{ Length = 12 } |
            New-RandomUniqueId

        $results[0].Length | Should -Be 4
        $results[1].Length | Should -Be 12
    }
}
