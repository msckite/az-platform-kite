using System;
using System.Linq;
using System.Management.Automation;
using System.Threading;

namespace MSCKite.Azure.Platform.Internal.Azure
{
    // Assigns and reads Azure RBAC role assignments via the Az.Resources module
    internal static class AzureRoleAssignmentHelper
    {
        private const string GetScript = @"
param($ObjectId, $Scope, $Role)
Get-AzRoleAssignment -ObjectId $ObjectId -Scope $Scope -RoleDefinitionName $Role -ErrorAction SilentlyContinue | Select-Object -First 1
";

        private const string CreateScript = @"
param($ObjectId, $Scope, $Role)
New-AzRoleAssignment -ObjectId $ObjectId -Scope $Scope -RoleDefinitionName $Role -ErrorAction Stop | Out-Null
";

        internal static bool Exists(PSCmdlet cmdlet, string objectId, string scope, string role)
        {
            return cmdlet.InvokeCommand.InvokeScript(GetScript, objectId, scope, role).FirstOrDefault() != null;
        }

        // Newly created principals can take a while to propagate through Entra ID before RBAC will accept them; retry rather than failing immediately
        internal static void Create(PSCmdlet cmdlet, string objectId, string scope, string role, int maxAttempts = 5, int delayMilliseconds = 3000)
        {
            Exception lastError = null;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    cmdlet.InvokeCommand.InvokeScript(CreateScript, objectId, scope, role);
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    if (attempt < maxAttempts)
                    {
                        cmdlet.WriteVerbose($"Role assignment failed (attempt {attempt} of {maxAttempts}): {ex.Message}. Retrying in {delayMilliseconds}ms.");
                        Thread.Sleep(delayMilliseconds);
                    }
                }
            }

            throw new InvalidOperationException(
                $"Failed to assign role '{role}' at scope '{scope}' to principal '{objectId}' after {maxAttempts} attempts: {lastError.Message}", lastError);
        }
    }
}
