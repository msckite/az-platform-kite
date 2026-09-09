using System;
using System.Management.Automation;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Internal.Common
{
    internal static class PowerShellModuleLoader
    {
        // Imports each required module into the cmdlet's session, terminating on the first failure
        internal static void Import(PSCmdlet cmdlet, params PowerShellModule[] modules)
        {
            foreach (var module in modules)
            {
                try
                {
                    cmdlet.InvokeCommand.InvokeScript(
                        $"if (-not (Get-Module -Name {module.ModuleName})) {{ Import-Module -Name {module.ModuleName} -MinimumVersion {module.MinimumVersion} -ErrorAction Stop }}");
                }
                catch (Exception ex)
                {
                    cmdlet.ThrowTerminatingError(new ErrorRecord(
                        new InvalidOperationException(
                            $"'{module.ModuleName}' (>= {module.MinimumVersion}) is required. Install it with: Install-Module {module.ModuleName} -MinimumVersion {module.MinimumVersion}", ex),
                        "AzModuleNotFound",
                        ErrorCategory.ObjectNotFound,
                        null));
                }
            }
        }
    }
}
