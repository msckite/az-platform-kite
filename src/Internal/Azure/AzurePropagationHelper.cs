using System;
using System.Threading;

namespace MSCKite.Azure.Platform.Internal.Azure
{
    // Azure control-plane reads can lag just-created resources; poll until the resource is readable instead of assuming immediate consistency
    internal static class AzurePropagationHelper
    {
        // Retries `fetch` until it returns a non-null result, or `maxAttempts` is reached; returns the last (possibly null) result
        internal static T WaitUntilReadable<T>(Func<T> fetch, Action<string> logVerbose = null, int maxAttempts = 5, int delayMilliseconds = 2000) where T : class
        {
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var result = fetch();
                if (result != null)
                {
                    return result;
                }

                if (attempt < maxAttempts)
                {
                    logVerbose?.Invoke($"Not yet readable (attempt {attempt} of {maxAttempts}); waiting {delayMilliseconds}ms before retrying.");
                    Thread.Sleep(delayMilliseconds);
                }
            }

            return null;
        }
    }
}
