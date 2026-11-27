$ErrorActionPreference = 'Stop'

# The sample's resource names must follow the repository that copies it. The path in loadJsonContent only resolves
# in that repository, so these tests read the file instead of building it.
Describe 'Sample Bicep parameters' {
    It 'takes the shared values from config/global-config.jsonc' {
        $path = Join-Path (Split-Path $PSScriptRoot -Parent) 'templates/iac/res/sample/params/base.bicepparam'
        $content = Get-Content -Path $path -Raw

        $content | Should -Match "var globalConfig = loadJsonContent\('\.\./\.\./\.\./\.\./config/global-config\.jsonc'\)"

        foreach ($name in 'uniqueId', 'serviceShort', 'location') {
            $content | Should -Match "(?m)^param $name = globalConfig\.$name\r?$"
        }
    }
}
