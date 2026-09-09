using MSCKite.Azure.Platform.Models;

namespace MSCKite.Azure.Platform.Internal.DevOps
{
    // Tracks whether Connect-AdoOrganization has been run; restored from disk on first use each
    // process so it survives across sessions, same as Connect-AzAccount and gh auth login
    internal static class AdoSessionState
    {
        internal static bool IsConnected { get; private set; }
        internal static DevOpsContext Current { get; private set; }
        internal static string Project { get; private set; }
        internal static bool UsesAzToken { get; private set; }

        // Runs once per process; a stale restored connection is caught and cleared by the normal
        // AuthGetStatus revalidation (Az sign-in check) or by the first real REST call failing
        static AdoSessionState()
        {
            var saved = AdoSessionStore.Load();
            if (saved == null)
            {
                return;
            }

            IsConnected = true;
            Current = new DevOpsContext
            {
                IsSignedIn = true,
                Account = saved.Account,
                Organization = saved.Organization,
                CollectionUri = saved.CollectionUri
            };
            Project = saved.Project;
            UsesAzToken = saved.UsesAzToken;
        }

        internal static void SetConnected(DevOpsContext context, string project, bool usesAzToken)
        {
            IsConnected = true;
            Current = context;
            Project = project;
            UsesAzToken = usesAzToken;

            AdoSessionStore.Save(context.Account, context.Organization, context.CollectionUri, project, usesAzToken);
        }

        // Also drops the cached auth header so a stale token can't be silently reused after disconnecting
        internal static void Clear()
        {
            IsConnected = false;
            Current = null;
            Project = null;
            UsesAzToken = false;
            AdoRestClient.ClearCachedHeader();
            AdoSessionStore.Clear();
        }
    }
}
