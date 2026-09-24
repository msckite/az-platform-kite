using System;
using System.Linq;
using System.Management.Automation;

namespace MSCKite.Azure.Platform.Internal.Azure
{
    // Well-known Microsoft Graph service principal appId; identical in every tenant
    internal static class MicrosoftGraphAppRoleHelper
    {
        private const string MicrosoftGraphAppId = "00000003-0000-0000-c000-000000000000";

        // Resolves the Microsoft Graph service principal's own object id, so app role assignments can point their ResourceId at it
        private const string GetGraphServicePrincipalScript = @"
(Get-MgServicePrincipal -Filter ""appId eq '" + MicrosoftGraphAppId + @"'"" -ErrorAction Stop).Id
";

        // App roles are looked up by value (e.g. 'Group.Read.All') rather than by id, since role ids aren't stable to hand-author
        private const string GetAppRoleIdScript = @"
param($GraphServicePrincipalId, $PermissionValue)
$graphSp = Get-MgServicePrincipal -ServicePrincipalId $GraphServicePrincipalId -ErrorAction Stop
$appRole = $graphSp.AppRoles | Where-Object { $_.Value -eq $PermissionValue -and $_.AllowedMemberTypes -contains 'Application' } | Select-Object -First 1
if (-not $appRole) {
    throw ""Microsoft Graph has no application permission named '$PermissionValue'.""
}
$appRole.Id
";

        private const string GetExistingAssignmentScript = @"
param($PrincipalId, $AppRoleId, $GraphServicePrincipalId)
Get-MgServicePrincipalAppRoleAssignment -ServicePrincipalId $PrincipalId -ErrorAction Stop |
    Where-Object { $_.AppRoleId -eq $AppRoleId -and $_.ResourceId -eq $GraphServicePrincipalId } |
    Select-Object -First 1
";

        private const string CreateAssignmentScript = @"
param($PrincipalId, $AppRoleId, $GraphServicePrincipalId)
New-MgServicePrincipalAppRoleAssignment -ServicePrincipalId $PrincipalId -PrincipalId $PrincipalId -ResourceId $GraphServicePrincipalId -AppRoleId $AppRoleId -ErrorAction Stop | Out-Null
";

        internal static string GetGraphServicePrincipalId(PSCmdlet cmdlet)
        {
            var result = cmdlet.InvokeCommand.InvokeScript(GetGraphServicePrincipalScript).FirstOrDefault();
            if (result == null)
            {
                throw new InvalidOperationException("Could not resolve the Microsoft Graph service principal in this tenant.");
            }

            return result.BaseObject as string;
        }

        internal static string GetAppRoleId(PSCmdlet cmdlet, string graphServicePrincipalId, string permissionValue)
        {
            var result = cmdlet.InvokeCommand.InvokeScript(GetAppRoleIdScript, graphServicePrincipalId, permissionValue).FirstOrDefault();
            return result?.BaseObject as string;
        }

        // True if the principal already holds this exact app role on the Microsoft Graph resource
        internal static bool HasAssignment(PSCmdlet cmdlet, string principalId, string appRoleId, string graphServicePrincipalId)
        {
            return cmdlet.InvokeCommand.InvokeScript(GetExistingAssignmentScript, principalId, appRoleId, graphServicePrincipalId).FirstOrDefault() != null;
        }

        internal static void CreateAssignment(PSCmdlet cmdlet, string principalId, string appRoleId, string graphServicePrincipalId)
        {
            cmdlet.InvokeCommand.InvokeScript(CreateAssignmentScript, principalId, appRoleId, graphServicePrincipalId);
        }
    }
}
