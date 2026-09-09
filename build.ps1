[CmdletBinding()]
param()

#region Variables

# The tag name in the format v<version>[-<prerelease>], e.g. v1.0.0 or v1.0.0-beta
$tagName = $env:TAG_NAME -replace '^v', ''

# Determine if the tag name indicates a prerelease version
$isPrerelease = $tagName -match '-'

# Define the module name
$moduleName = 'MSCKite.Azure.Platform'
$sourcePath = "$PSScriptRoot\src"

#endregion

#region Update Module Manifest

# Select the appropriate template based on prerelease status
$sourceManifest = if ($isPrerelease) {
    "$sourcePath\$moduleName`_Prerelease.psd1"
} else {
    "$sourcePath\$moduleName`_Release.psd1"
}

$manifestPath = Join-Path -Path $sourcePath -ChildPath "$moduleName.psd1"

# Copy the selected template to create a fresh manifest file
Copy-Item -Path $sourceManifest -Destination $manifestPath -Force

# Read the manifest content
$manifestContent = Get-Content -Path $manifestPath -Raw

# Update ModuleVersion (preserving existing spacing around '=')
$moduleVersion = ($tagName -split '-')[0]
$manifestContent = $manifestContent -replace "(ModuleVersion\s*=\s*)'[^']*'", ('$1' + "'$moduleVersion'")

# Update Prerelease if applicable (preserving existing spacing around '=')
if ($isPrerelease) {
    $prereleaseVersion = ($tagName -split '-')[1]
    $manifestContent = $manifestContent -replace "(Prerelease\s*=\s*)'[^']*'", ('$1' + "'$prereleaseVersion'")
}

# Write the updated content back to the manifest
$manifestContent | Set-Content -Path $manifestPath -NoNewline

#endregion
