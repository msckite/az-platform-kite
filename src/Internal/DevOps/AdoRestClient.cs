using System.Management.Automation;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace MSCKite.Azure.Platform.Internal.DevOps
{
    internal class AdoRestResult
    {
        public bool IsSuccessStatusCode { get; set; }
        public string StatusDescription { get; set; }
        public string Body { get; set; }
    }

    // Invokes the Azure DevOps REST API, reusing and refreshing a cached auth header across calls
    internal static class AdoRestClient
    {
        // Global "who am I" endpoint used both to validate a bearer header and to check auth status; org-independent
        internal const string ProfileUri = "https://app.vssps.visualstudio.com/_apis/profile/profiles/me";

        private static AuthenticationHeaderValue _header;

        // Drops the in-memory bearer header so a stale, still-valid Az-derived token can't keep being reused after Disconnect-AzAccount
        internal static void ClearCachedHeader()
        {
            _header = null;
        }

        internal static AdoRestResult Invoke(
            PSCmdlet cmdlet,
            string uri,
            string version,
            HttpMethod method,
            string pat = null,
            string queryParameters = null,
            string body = null,
            string contentType = "application/json")
        {
            EnsureValidHeader(cmdlet, pat);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = _header;

                var requestUri = string.IsNullOrEmpty(queryParameters)
                    ? $"{uri}?api-version={version}"
                    : $"{uri}?{queryParameters}&api-version={version}";

                using (var request = new HttpRequestMessage(method, requestUri))
                {
                    if (body != null)
                    {
                        request.Content = new StringContent(body, Encoding.UTF8, contentType);
                    }

                    var response = client.SendAsync(request).GetAwaiter().GetResult();
                    var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    return new AdoRestResult
                    {
                        IsSuccessStatusCode = response.IsSuccessStatusCode,
                        StatusDescription = $"{(int)response.StatusCode} {response.ReasonPhrase}",
                        Body = responseBody
                    };
                }
            }
        }

        // Re-validates a cached bearer header against the profile endpoint before reusing it, like the PS script's $script:header check
        private static void EnsureValidHeader(PSCmdlet cmdlet, string pat)
        {
            if (_header != null && _header.Scheme == "Bearer" && !IsHeaderStillValid())
            {
                _header = null;
            }

            if (_header == null)
            {
                _header = AdoAuthHelper.GetAuthHeader(cmdlet, pat);
            }
        }

        private static bool IsHeaderStillValid()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = _header;

                try
                {
                    var response = client.GetAsync($"{ProfileUri}?api-version=7.1").GetAwaiter().GetResult();
                    return response.IsSuccessStatusCode;
                }
                catch (HttpRequestException)
                {
                    return false;
                }
            }
        }
    }
}
