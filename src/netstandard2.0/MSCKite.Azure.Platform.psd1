@{
    RootModule        = 'MSCKite.Azure.Platform.dll'
    ModuleVersion     = '0.1.0'
    GUID              = '3d8f6c2e-9b4a-4c1d-8e2f-6a7b5c9d0e1f'
    Author            = 'Martin Swinkels'
    CompanyName       = 'MSCKite™'
    Copyright         = '(C) Martin Swinkels. All rights reserved.'
    Description       = 'Lightweight automation for Azure platform engineering, developer enablement, and cloud operations.'
    PowerShellVersion = '7.0'
    RequiredModules   = @(
        @{ ModuleName = 'Az.Accounts'; ModuleVersion = '5.5' },
        @{ ModuleName = 'Az.Resources'; ModuleVersion = '10.1' }
    )
    CmdletsToExport   = @('Connect-AdoOrganization', 'Disconnect-AdoOrganization', 'Disconnect-PlatformContext', 'Get-AdoDefault', 'Get-GitHubDefault', 'Get-PlatformContext', 'Set-AdoDefault', 'Set-GitHubDefault')
    PrivateData       = @{
        PSData = @{
            Tags       = @('Az', 'Azure', 'Platform', 'Engineering', 'DevOps', 'GitHub', 'AzPlatformKite', 'MSC')
            ProjectUri = 'https://github.com/msckite/az-platform-kit'
        }
    }
}
