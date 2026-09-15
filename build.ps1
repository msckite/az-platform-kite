[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Detailed', 'Diagnostic', 'Minimal', 'None', 'Normal')]
    [string]$Output = 'Normal'
)

Properties {
    $script:Output = $Output
    $script:rootPath = (Get-Item $PSScriptRoot).FullName
    $script:sourcePath = Join-Path $script:rootPath 'src'
    $script:testsPath = Join-Path $script:rootPath 'tests'
    $script:projectFile = Join-Path $script:sourcePath 'MSCKite.Azure.Platform.csproj'
    $script:moduleName = 'MSCKite.Azure.Platform'
    $script:buildConfiguration = 'Debug'
    $script:tagName = $env:TAG_NAME
    $script:isPrerelease = $script:tagName -match '-'
    $script:manifestPath = Join-Path $script:sourcePath "$script:moduleName.psd1"
}

Task Default -Depends Build

Task UpdateManifest {
    if ([string]::IsNullOrWhiteSpace($script:tagName)) {
        return
    }

    $sourceManifest = if ($script:isPrerelease) {
        Join-Path $script:sourcePath "$script:moduleName`_Prerelease.psd1"
    } else {
        Join-Path $script:sourcePath "$script:moduleName`_Release.psd1"
    }

    Copy-Item -Path $sourceManifest -Destination $script:manifestPath -Force

    $manifestContent = Get-Content -Path $script:manifestPath -Raw
    $moduleVersion = ($script:tagName -split '-')[0].TrimStart('v')

    $manifestContent = $manifestContent -replace "(?m)^(\s*ModuleVersion\s*=\s*)'[^']*'", ('${1}' + "'$moduleVersion'")

    if ($script:isPrerelease) {
        $prereleaseVersion = ($script:tagName -split '-', 2)[1]
        $manifestContent = $manifestContent -replace "(Prerelease\s*=\s*)'[^']*'", ('${1}' + "'$prereleaseVersion'")
    }

    $manifestContent | Set-Content -Path $script:manifestPath -NoNewline
}

Task Clean {
    if (Get-Module -Name $script:moduleName -All) {
        Remove-Module -Name $script:moduleName -Force -ErrorAction SilentlyContinue
    }

    & dotnet clean $script:projectFile --configuration $script:buildConfiguration --nologo
    if ($LASTEXITCODE -ne 0) {
        throw 'dotnet clean failed.'
    }
}

Task Build -Depends Clean, UpdateManifest {
    if (Get-Module -Name $script:moduleName -All) {
        Remove-Module -Name $script:moduleName -Force -ErrorAction SilentlyContinue
    }

    & dotnet build $script:projectFile --configuration $script:buildConfiguration --nologo
    if ($LASTEXITCODE -ne 0) {
        throw 'dotnet build failed.'
    }
}

Task Test -Depends Build {
    if (-not (Get-Command Invoke-Pester -ErrorAction SilentlyContinue)) {
        throw 'Pester is not installed. Install-Module Pester -Scope CurrentUser -Force'
    }

    $config = New-PesterConfiguration
    $config.Run.Path = $script:testsPath
    $config.Run.PassThru = $false
    $config.Output.Verbosity = $script:Output
    Invoke-Pester -Configuration $config
}

