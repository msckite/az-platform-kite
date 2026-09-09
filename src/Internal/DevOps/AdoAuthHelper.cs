using System;
using System.Linq;
using System.Management.Automation;
using System.Net.Http.Headers;
using System.Text;

namespace MSCKite.Azure.Platform.Internal.DevOps
{
    // Builds the auth header for Azure DevOps REST calls: a PAT if supplied, otherwise an Az PowerShell access token
    internal static class AdoAuthHelper
    {
        // Well-known first-party application ID for Azure DevOps, used to scope Get-AzAccessToken
        private const string DevOpsResourceId = "499b84ac-1321-427f-aa17-267ca6975798";

        internal static AuthenticationHeaderValue GetAuthHeader(PSCmdlet cmdlet, string pat)
        {
            if (!string.IsNullOrEmpty(pat))
            {
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(":" + pat));
                return new AuthenticationHeaderValue("Basic", credentials);
            }

            var script = $"(Get-AzAccessToken -ResourceUrl '{DevOpsResourceId}' -AsSecureString).Token | ConvertFrom-SecureString -AsPlainText";
            var token = cmdlet.InvokeCommand.InvokeScript(script).FirstOrDefault();

            if (token == null)
            {
                throw new InvalidOperationException("Please sign in to Azure PowerShell first (Connect-AzAccount).");
            }

            return new AuthenticationHeaderValue("Bearer", token.ToString());
        }
    }
}
