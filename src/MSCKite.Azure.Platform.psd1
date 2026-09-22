@{
    RootModule        = 'MSCKite.Azure.Platform.dll'
    ModuleVersion     = '1.2.3'
    GUID              = '3d8f6c2e-9b4a-4c1d-8e2f-6a7b5c9d0e1f'
    Author            = 'Martin Swinkels'
    CompanyName       = 'MSCKite™'
    Copyright         = 'Copyright (C) 2026 Martin Swinkels. All rights reserved.'
    Description       = 'Lightweight automation for Azure platform engineering, developer enablement, and cloud operations.'
    PowerShellVersion = '7.0'
    RequiredModules   = @(
        @{ ModuleName = 'Az.Accounts'; ModuleVersion = '5.5' },
        @{ ModuleName = 'Az.Resources'; ModuleVersion = '10.1' },
        @{ ModuleName = 'Az.ManagedServiceIdentity'; ModuleVersion = '2.0' }
    )
    CmdletsToExport   = @('Connect-AdoOrganization', 'Disconnect-AdoOrganization', 'Disconnect-PlatformContext', 'Get-AdoDefault', 'Get-GitHubDefault', 'Get-PlatformContext', 'Get-PlatformTemplate', 'Grant-PlatformGraphPermission', 'New-PlatformConfigStructure', 'New-PlatformWorkflow', 'New-RandomPassword', 'New-RandomUniqueId', 'Set-AdoDefault', 'Set-GitHubDefault', 'Set-GitHubLabels', 'Set-PlatformEnvironmentIdentity', 'Set-PlatformGitHubEnvironment', 'Set-PlatformResourceGroup', 'Set-PlatformSecurityGroup', 'Test-PlatformTemplate')
    PrivateData       = @{
        PSData = @{
            Tags       = @('Az', 'Azure', 'Platform', 'Engineering', 'DevOps', 'GitHub', 'Kite', 'MSC')
            LicenseUri = 'https://github.com/msckite/az-platform-kite/blob/main/LICENSE.md'
            ProjectUri = 'https://github.com/msckite/az-platform-kite'
            IconUri    = 'https://raw.githubusercontent.com/msckite/az-platform-kite/refs/heads/main/.assets/msckite-icon.png'
            Prerelease = 'beta.1'
        }
    }
}
