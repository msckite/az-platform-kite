using System;
using System.Linq;
using System.Management.Automation;
using System.Net.Http;
using System.Text.RegularExpressions;
using MSCKite.Azure.Platform.Internal.Azure;
using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Internal.DevOps
{
    internal static class AdoApiHelper
    {
        private const string OrganizationUrlVariable = "AZURE_DEVOPS_ORG_URL";
        private const string PersonalAccessTokenVariable = "AZURE_DEVOPS_PAT";

        // Validates auth against the profile endpoint and, on success, marks the session as connected; throws on any failure so Connect-AdoOrganization can surface it
        internal static DevOpsContext Connect(PSCmdlet cmdlet, string organization, string project)
        {
            var (resolvedOrganization, resolvedCollectionUri) = ResolveOrganization(organization);
            if (string.IsNullOrEmpty(resolvedCollectionUri))
            {
                throw new InvalidOperationException(
                    $"No organization configured. Pass -Organization, set '{OrganizationUrlVariable}', or run Set-AdoDefault -Organization <org> first.");
            }

            var pat = Environment.GetEnvironmentVariable(PersonalAccessTokenVariable);
            var result = AdoRestClient.Invoke(cmdlet, AdoRestClient.ProfileUri, "7.1", HttpMethod.Get, pat);

            if (!result.IsSuccessStatusCode)
            {
                var detailMatch = Regex.Match(result.Body, "\"message\"\\s*:\\s*\"([^\"]+)\"");
                throw new InvalidOperationException(detailMatch.Success
                    ? $"Azure DevOps API returned {result.StatusDescription}: {detailMatch.Groups[1].Value}"
                    : $"Azure DevOps API returned {result.StatusDescription}. {result.Body}".Trim());
            }

            var context = new DevOpsContext
            {
                Account = null,
                Organization = resolvedOrganization,
                CollectionUri = resolvedCollectionUri,
                IsSignedIn = true
            };

            var nameMatch = Regex.Match(result.Body, "\"emailAddress\"\\s*:\\s*\"([^\"]+)\"");
            if (nameMatch.Success)
            {
                context.Account = nameMatch.Groups[1].Value;
            }

            AdoSessionState.SetConnected(context, project ?? AdoConfigStore.Load().Project, usesAzToken: string.IsNullOrEmpty(pat));
            return context;
        }

        // Reports the current in-memory connection state; never calls the Azure DevOps API, so Get-PlatformContext stays a cheap read
        internal static DevOpsContext GetAuthStatus(PSCmdlet cmdlet, out string message)
        {
            message = null;

            if (!AdoSessionState.IsConnected)
            {
                message = "Not signed in to Azure DevOps. Run Connect-AdoOrganization.";
                var (organization, collectionUri) = ResolveOrganization(null);
                return new DevOpsContext { IsSignedIn = false, Organization = organization, CollectionUri = collectionUri };
            }

            // A connection built from an Az-derived token is only as good as the underlying Az sign-in
            if (AdoSessionState.UsesAzToken && !AzureContextHelper.GetContext(cmdlet, out _).IsSignedIn)
            {
                var organization = AdoSessionState.Current.Organization;
                var collectionUri = AdoSessionState.Current.CollectionUri;
                AdoSessionState.Clear();

                message = "Not signed in to Azure DevOps. Azure sign-in expired; run Connect-AzAccount, then Connect-AdoOrganization again.";
                return new DevOpsContext { IsSignedIn = false, Organization = organization, CollectionUri = collectionUri };
            }

            return AdoSessionState.Current;
        }

        // Returns the bare organization name alongside its full collection URI, preferring an explicit -Organization,
        // then the AZURE_DEVOPS_ORG_URL env var, then the Set-AdoDefault config
        private static (string Organization, string CollectionUri) ResolveOrganization(string organization)
        {
            if (!string.IsNullOrEmpty(organization))
            {
                return (organization, $"https://dev.azure.com/{organization}");
            }

            var envCollectionUri = Environment.GetEnvironmentVariable(OrganizationUrlVariable);
            if (!string.IsNullOrEmpty(envCollectionUri))
            {
                return (ExtractOrganizationFromUri(envCollectionUri), envCollectionUri);
            }

            var defaults = AdoConfigStore.Load();
            return (defaults.Organization, defaults.CollectionUri);
        }

        // Handles both the https://dev.azure.com/{organization} and legacy https://{organization}.visualstudio.com formats
        internal static string ExtractOrganizationFromUri(string collectionUri)
        {
            if (!Uri.TryCreate(collectionUri, UriKind.Absolute, out var uri))
            {
                return null;
            }

            var pathSegment = uri.AbsolutePath.Trim('/').Split('/').FirstOrDefault(segment => !string.IsNullOrEmpty(segment));
            if (!string.IsNullOrEmpty(pathSegment))
            {
                return pathSegment;
            }

            var dotIndex = uri.Host.IndexOf('.');
            return dotIndex > 0 ? uri.Host.Substring(0, dotIndex) : uri.Host;
        }
    }
}
