using System;
using System.Management.Automation;
using MSCKite.Azure.Platform.Internal.Azure;
using MSCKite.Azure.Platform.Internal.Common;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Commands.Bootstrap
{
    // One-time bootstrap step, run manually by a directory admin after Set-PlatformEnvironmentIdentity creates the
    // federated identity. Grants that identity's service principal the Microsoft Graph application permissions
    // Set-PlatformSecurityGroup needs (Get-AzADGroup, New-AzADGroup) when it later runs unattended in CI.
    //
    // Never invoked by the Set-Platform* phases themselves: assigning Microsoft Graph app roles requires the
    // Application Administrator, Privileged Role Administrator, or Global Administrator directory role, and the
    // automated identity must not hold that role permanently.
    [Cmdlet(VerbsSecurity.Grant, "PlatformGraphPermission", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(PlatformGraphPermissionGrantResult))]
    public class GrantPlatformGraphPermission : PSCmdlet
    {
        private static readonly PowerShellModule[] RequiredModules = {
            new PowerShellModule("Microsoft.Graph.Applications", "2.0")
        };

        private static readonly string[] DefaultPermissions = { "Group.Read.All" };

        // Object (principal) id of the identity's service principal in Entra ID; this is the PrincipalId from Set-PlatformEnvironmentIdentity's output
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string PrincipalId { get; set; }

        // Microsoft Graph application permissions to grant. Add 'Group.ReadWrite.All' if CI is expected to create or update groups outside -WhatIf
        [Parameter(Position = 1)]
        [ValidateNotNullOrEmpty]
        public string[] Permission { get; set; } = DefaultPermissions;

        protected override void BeginProcessing()
        {
            PowerShellModuleLoader.Import(this, (PowerShellModule[])(object)RequiredModules);

            WriteVerbose("Connecting to Microsoft Graph. Sign in with an account that holds Application Administrator, Privileged Role Administrator, or Global Administrator.");
            try
            {
                InvokeCommand.InvokeScript("Connect-MgGraph -Scopes 'AppRoleAssignment.ReadWrite.All','Application.Read.All' -NoWelcome -ErrorAction Stop");
            }
            catch (Exception ex)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new InvalidOperationException($"Failed to connect to Microsoft Graph: {ex.Message}", ex),
                    "PlatformGraphPermissionConnectFailed", ErrorCategory.AuthenticationError, null));
            }
        }

        protected override void ProcessRecord()
        {
            string graphServicePrincipalId;
            try
            {
                graphServicePrincipalId = MicrosoftGraphAppRoleHelper.GetGraphServicePrincipalId(this);
            }
            catch (Exception ex)
            {
                ThrowTerminatingError(new ErrorRecord(ex, "PlatformGraphPermissionGraphSpNotFound", ErrorCategory.ObjectNotFound, null));
                return;
            }

            var result = new PlatformGraphPermissionGrantResult { PrincipalId = PrincipalId };

            foreach (var permission in Permission)
            {
                var actionResult = GrantAppRole(permission, graphServicePrincipalId);
                if (actionResult != null)
                {
                    result.Permissions.Add(actionResult);
                }
            }

            WriteObject(result);
        }

        private PlatformGraphPermissionActionResult GrantAppRole(string permission, string graphServicePrincipalId)
        {
            WriteVerbose($"Permission '{permission}': resolving Microsoft Graph app role.");
            string appRoleId;
            try
            {
                appRoleId = MicrosoftGraphAppRoleHelper.GetAppRoleId(this, graphServicePrincipalId, permission);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(
                    new InvalidOperationException($"Failed to resolve Microsoft Graph app role '{permission}': {ex.Message}", ex),
                    "PlatformGraphPermissionAppRoleNotFound", ErrorCategory.ObjectNotFound, permission));
                return null;
            }

            if (MicrosoftGraphAppRoleHelper.HasAssignment(this, PrincipalId, appRoleId, graphServicePrincipalId))
            {
                WriteVerbose($"Permission '{permission}' is already granted to principal '{PrincipalId}'; nothing to do.");
                return new PlatformGraphPermissionActionResult { Permission = permission, Action = "Unchanged" };
            }

            if (!ShouldProcess(PrincipalId, $"Grant Microsoft Graph application permission '{permission}'"))
            {
                return null;
            }

            try
            {
                MicrosoftGraphAppRoleHelper.CreateAssignment(this, PrincipalId, appRoleId, graphServicePrincipalId);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(
                    new InvalidOperationException($"Failed to grant permission '{permission}' to principal '{PrincipalId}': {ex.Message}", ex),
                    "PlatformGraphPermissionGrantFailed", ErrorCategory.WriteError, permission));
                return null;
            }

            WriteVerbose($"Permission '{permission}' granted to principal '{PrincipalId}'.");
            return new PlatformGraphPermissionActionResult { Permission = permission, Action = "Granted" };
        }
    }
}
