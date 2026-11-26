$ErrorActionPreference = 'Stop'

Describe 'Grant-PlatformGraphPermission' {
    BeforeAll {
        $modulePath = if ($env:MODULE_UNDER_TEST) {
            Convert-Path $env:MODULE_UNDER_TEST
        } else {
            Convert-Path (Join-Path (Get-Location) 'src\bin\Debug\netstandard2.0\MSCKite.Azure.Platform.dll')
        }

        Import-Module $modulePath -Force

        # Stands in for Microsoft.Graph.Applications: the cmdlet skips Import-Module for a module that is already loaded,
        # so its Connect-MgGraph and Microsoft Graph calls reach these fakes instead of a real tenant.
        # 'principal-with-read' already holds Group.Read.All; every grant is recorded instead of applied.
        New-Module -Name 'Microsoft.Graph.Applications' {
            $script:Grants = [System.Collections.Generic.List[object]]::new()

            function Connect-MgGraph { }

            function Get-MgServicePrincipal {
                [CmdletBinding()]
                param([string] $Filter, [string] $ServicePrincipalId)

                if ($Filter) {
                    return [PSCustomObject]@{ Id = 'graph-sp-id' }
                }

                [PSCustomObject]@{
                    Id       = $ServicePrincipalId
                    AppRoles = @(
                        [PSCustomObject]@{ Id = 'role-read'; Value = 'Group.Read.All'; AllowedMemberTypes = @('Application') }
                        [PSCustomObject]@{ Id = 'role-readwrite'; Value = 'Group.ReadWrite.All'; AllowedMemberTypes = @('Application') }
                    )
                }
            }

            function Get-MgServicePrincipalAppRoleAssignment {
                [CmdletBinding()]
                param([string] $ServicePrincipalId)

                if ($ServicePrincipalId -eq 'principal-with-read') {
                    [PSCustomObject]@{ AppRoleId = 'role-read'; ResourceId = 'graph-sp-id' }
                }
            }

            function New-MgServicePrincipalAppRoleAssignment {
                [CmdletBinding(SupportsShouldProcess)]
                param([string] $ServicePrincipalId, [string] $PrincipalId, [string] $ResourceId, [string] $AppRoleId)

                if ($PSCmdlet.ShouldProcess($PrincipalId, 'Record fake app role assignment')) {
                    $script:Grants.Add([PSCustomObject]@{
                            ServicePrincipalId = $ServicePrincipalId
                            PrincipalId        = $PrincipalId
                            ResourceId         = $ResourceId
                            AppRoleId          = $AppRoleId
                        })
                }
            }

            function Get-FakeGraphGrant { $script:Grants }

            function Clear-FakeGraphGrant { $script:Grants.Clear() }

            Export-ModuleMember -Function *
        } | Import-Module -Global
    }

    AfterAll {
        Remove-Module MSCKite.Azure.Platform -ErrorAction SilentlyContinue
        Remove-Module Microsoft.Graph.Applications -ErrorAction SilentlyContinue
    }

    BeforeEach {
        Clear-FakeGraphGrant
    }

    It 'reports a permission it would grant as WouldGrant under -WhatIf, without granting it' {
        $result = Grant-PlatformGraphPermission -PrincipalId 'principal-without' -Permission 'Group.Read.All', 'Group.ReadWrite.All' -WhatIf

        $result.PrincipalId | Should -Be 'principal-without'
        $result.Permissions | Should -HaveCount 2
        $result.Permissions.Permission | Should -Be @('Group.Read.All', 'Group.ReadWrite.All')
        $result.Permissions.Action | Should -Be @('WouldGrant', 'WouldGrant')
        Get-FakeGraphGrant | Should -BeNullOrEmpty
    }

    It 'reports a permission the identity already has as Unchanged' {
        $result = Grant-PlatformGraphPermission -PrincipalId 'principal-with-read' -Permission 'Group.Read.All', 'Group.ReadWrite.All' -WhatIf

        ($result.Permissions | Where-Object Permission -EQ 'Group.Read.All').Action | Should -Be 'Unchanged'
        ($result.Permissions | Where-Object Permission -EQ 'Group.ReadWrite.All').Action | Should -Be 'WouldGrant'
    }

    It 'grants a missing permission and reports it as Granted' {
        $result = Grant-PlatformGraphPermission -PrincipalId 'principal-without' -Confirm:$false

        $result.Permissions | Should -HaveCount 1
        $result.Permissions[0].Permission | Should -Be 'Group.Read.All'
        $result.Permissions[0].Action | Should -Be 'Granted'

        $grant = Get-FakeGraphGrant
        $grant | Should -HaveCount 1
        $grant.ServicePrincipalId | Should -Be 'principal-without'
        $grant.PrincipalId | Should -Be 'principal-without'
        $grant.ResourceId | Should -Be 'graph-sp-id'
        $grant.AppRoleId | Should -Be 'role-read'
    }

    It 'reports an unknown permission as an error and leaves it out of the result' {
        $errors = $null
        $result = Grant-PlatformGraphPermission -PrincipalId 'principal-without' -Permission 'Bogus.Permission', 'Group.Read.All' -WhatIf `
            -ErrorAction SilentlyContinue -ErrorVariable errors

        $errors.FullyQualifiedErrorId | Should -Contain 'PlatformGraphPermissionAppRoleNotFound,MSCKite.Azure.Platform.Commands.Bootstrap.GrantPlatformGraphPermission'
        $result.Permissions.Permission | Should -Be @('Group.Read.All')
    }

    It 'takes the PrincipalId from the pipeline by property name' {
        $result = [PSCustomObject]@{ PrincipalId = 'principal-without' } | Grant-PlatformGraphPermission -WhatIf

        $result.PrincipalId | Should -Be 'principal-without'
        $result.Permissions[0].Action | Should -Be 'WouldGrant'
    }
}
