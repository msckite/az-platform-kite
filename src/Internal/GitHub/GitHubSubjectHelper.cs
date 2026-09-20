using System;

namespace MSCKite.Azure.Platform.Internal.GitHub
{
    // Computes the immutable GitHub Actions OIDC subject claim for a federated credential
    // https://docs.github.com/en/actions/reference/security/oidc#immutable-subject-claims
    internal static class GitHubSubjectHelper
    {
        internal static string ResolveSubject(string subjectType, GitHubRepositoryInfo repository, string environmentName)
        {
            if (repository == null || string.IsNullOrEmpty(repository.OwnerId) || string.IsNullOrEmpty(repository.Id))
            {
                throw new InvalidOperationException("The live GitHub repository's owner id and repository id are required to compute an immutable subject, but could not be read.");
            }

            // repo:OWNER@OWNER-ID/REPO@REPO-ID:... - immutable, so a repo rename/transfer or a recycled owner/repo name can't hijack the trust
            var repo = $"repo:{repository.OwnerLogin}@{repository.OwnerId}/{repository.Name}@{repository.Id}";

            switch (subjectType)
            {
                case "environment":
                    if (string.IsNullOrWhiteSpace(environmentName))
                    {
                        throw new InvalidOperationException("A GitHub environment name is required to compute an 'environment' subject, but none was configured.");
                    }

                    return $"{repo}:environment:{EscapeColon(environmentName)}";

                case "pull_request":
                    return $"{repo}:pull_request";

                case "branch":
                    throw new NotSupportedException(
                        "subjectType 'branch' is not yet supported: platform-config.jsonc has no field for the branch name the subject should bind to.");

                default:
                    throw new NotSupportedException($"Unsupported federated credential subjectType '{subjectType}'.");
            }
        }

        private static string EscapeColon(string value)
        {
            return value.Replace(":", "%3A");
        }
    }
}
