using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using MSCKite.Azure.Platform.Internal.Common;

namespace MSCKite.Azure.Platform.Internal.Platform
{
    // Loads global-config.jsonc and platform-config.jsonc, and resolves ${placeholder} tokens shared across both
    internal static class PlatformConfigLoader
    {
        private static readonly Regex PlaceholderPattern = new Regex(@"\$\{(\w+)\}", RegexOptions.Compiled);

        private static readonly JsonDocumentOptions DocumentOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        internal static GlobalConfig LoadGlobalConfig(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Global config file not found: {path}", path);
            }

            TemplateVersionHelper.ReadVersion(path, "templateVersion");

            using (var document = JsonDocument.Parse(File.ReadAllText(path), DocumentOptions))
            {
                var root = document.RootElement;
                var config = new GlobalConfig
                {
                    TenantId = GetString(root, "tenantId"),
                    SubscriptionId = GetString(root, "subscriptionId"),
                    UniqueId = GetString(root, "uniqueId"),
                    ServiceShort = GetString(root, "serviceShort"),
                    DisplayName = GetString(root, "displayName"),
                    Location = GetString(root, "location"),
                    RegionCode = GetString(root, "regionCode")
                };

                if (root.TryGetProperty("sourceControl", out var sourceControlElement) &&
                    sourceControlElement.ValueKind == JsonValueKind.Object)
                {
                    config.SourceControl = new SourceControlConfig
                    {
                        Tool = GetString(sourceControlElement, "tool"),
                        Owner = GetString(sourceControlElement, "owner"),
                        Repository = GetString(sourceControlElement, "repository"),
                        BranchStrategy = GetString(sourceControlElement, "branchStrategy")
                    };
                }

                ValidateGlobalConfig(config);
                return config;
            }
        }

        internal static List<PlatformResourceGroupConfig> LoadResourceGroups(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Platform config file not found: {path}", path);
            }

            TemplateVersionHelper.ReadVersion(path, "templateVersion");

            using (var document = JsonDocument.Parse(File.ReadAllText(path), DocumentOptions))
            {
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object ||
                    !root.TryGetProperty("resourceGroups", out var arrayElement) ||
                    arrayElement.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException($"'{path}' must contain a \"resourceGroups\" array.");
                }

                var resourceGroups = new List<PlatformResourceGroupConfig>();
                var seenIds = new HashSet<string>(StringComparer.Ordinal);

                foreach (var element in arrayElement.EnumerateArray())
                {
                    var id = GetString(element, "id");
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        throw new InvalidOperationException("Each resource group must have a non-empty \"id\".");
                    }

                    if (!seenIds.Add(id))
                    {
                        throw new InvalidOperationException($"Duplicate resource group id in platform config: '{id}'.");
                    }

                    var resourceGroup = new PlatformResourceGroupConfig
                    {
                        Id = id,
                        Name = GetString(element, "name"),
                        Location = GetString(element, "location")
                    };

                    if (string.IsNullOrWhiteSpace(resourceGroup.Name))
                    {
                        throw new InvalidOperationException($"Resource group '{id}' must have a non-empty \"name\".");
                    }

                    if (string.IsNullOrWhiteSpace(resourceGroup.Location))
                    {
                        throw new InvalidOperationException($"Resource group '{id}' must have a non-empty \"location\".");
                    }

                    if (element.TryGetProperty("tags", out var tagsElement) && tagsElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var tag in tagsElement.EnumerateObject())
                        {
                            resourceGroup.Tags[tag.Name] = tag.Value.ValueKind == JsonValueKind.String ? tag.Value.GetString() : null;
                        }
                    }

                    resourceGroups.Add(resourceGroup);
                }

                return resourceGroups;
            }
        }

        internal static List<PlatformSecurityGroupConfig> LoadSecurityGroups(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Platform config file not found: {path}", path);
            }

            TemplateVersionHelper.ReadVersion(path, "templateVersion");

            using (var document = JsonDocument.Parse(File.ReadAllText(path), DocumentOptions))
            {
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object ||
                    !root.TryGetProperty("securityGroups", out var arrayElement) ||
                    arrayElement.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException($"'{path}' must contain a \"securityGroups\" array.");
                }

                var securityGroups = new List<PlatformSecurityGroupConfig>();

                foreach (var element in arrayElement.EnumerateArray())
                {
                    var displayName = GetString(element, "displayName");
                    var mailNickname = GetString(element, "mailNickname");

                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        throw new InvalidOperationException("Each security group must have a non-empty \"displayName\".");
                    }

                    if (string.IsNullOrWhiteSpace(mailNickname))
                    {
                        throw new InvalidOperationException($"Security group '{displayName}' must have a non-empty \"mailNickname\".");
                    }

                    var securityGroup = new PlatformSecurityGroupConfig
                    {
                        DisplayName = displayName,
                        MailNickname = mailNickname,
                        Description = GetString(element, "description") ?? string.Empty
                    };

                    if (!element.TryGetProperty("roleAssignments", out var roleAssignmentsElement) ||
                        roleAssignmentsElement.ValueKind != JsonValueKind.Array ||
                        roleAssignmentsElement.GetArrayLength() == 0)
                    {
                        throw new InvalidOperationException($"Security group '{displayName}' must have a non-empty \"roleAssignments\" array.");
                    }

                    foreach (var roleElement in roleAssignmentsElement.EnumerateArray())
                    {
                        var role = GetString(roleElement, "role");
                        if (string.IsNullOrWhiteSpace(role))
                        {
                            throw new InvalidOperationException($"Security group '{displayName}' has a role assignment missing \"role\".");
                        }

                        var (resourceGroupId, scope) = ParseRoleAssignmentScope(roleElement, $"Security group '{displayName}'");
                        securityGroup.RoleAssignments.Add(new PlatformRoleAssignmentConfig { Role = role, ResourceGroupId = resourceGroupId, Scope = scope });
                    }

                    securityGroups.Add(securityGroup);
                }

                return securityGroups;
            }
        }

        internal static List<PlatformEnvironmentConfig> LoadEnvironments(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Platform config file not found: {path}", path);
            }

            TemplateVersionHelper.ReadVersion(path, "templateVersion");

            using (var document = JsonDocument.Parse(File.ReadAllText(path), DocumentOptions))
            {
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object ||
                    !root.TryGetProperty("environments", out var arrayElement) ||
                    arrayElement.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException($"'{path}' must contain an \"environments\" array.");
                }

                var environments = new List<PlatformEnvironmentConfig>();

                foreach (var element in arrayElement.EnumerateArray())
                {
                    var environmentCode = GetString(element, "environmentCode");
                    if (string.IsNullOrWhiteSpace(environmentCode))
                    {
                        throw new InvalidOperationException("Each environment must have a non-empty \"environmentCode\".");
                    }

                    var resourceGroupId = GetString(element, "resourceGroupId");
                    if (string.IsNullOrWhiteSpace(resourceGroupId))
                    {
                        throw new InvalidOperationException($"Environment '{environmentCode}' must have a non-empty \"resourceGroupId\".");
                    }

                    if (!element.TryGetProperty("userAssignedIdentity", out var identityElement) || identityElement.ValueKind != JsonValueKind.Object)
                    {
                        throw new InvalidOperationException($"Environment '{environmentCode}' must have a \"userAssignedIdentity\" object.");
                    }

                    if (!element.TryGetProperty("githubEnvironment", out var githubElement) || githubElement.ValueKind != JsonValueKind.Object)
                    {
                        throw new InvalidOperationException($"Environment '{environmentCode}' must have a \"githubEnvironment\" object.");
                    }

                    // Optional narrower-scoped workload identity/environment; when absent, callers fall back to userAssignedIdentity/githubEnvironment.
                    // Both must be declared together: their GitHub environment names must differ from the infra one, or their secrets would collide.
                    PlatformUserAssignedIdentityConfig workloadIdentity = null;
                    if (element.TryGetProperty("workloadUserAssignedIdentity", out var workloadIdentityElement) && workloadIdentityElement.ValueKind == JsonValueKind.Object)
                    {
                        workloadIdentity = ParseUserAssignedIdentity(workloadIdentityElement, environmentCode);
                    }

                    PlatformGitHubEnvironmentConfig workloadGithubEnvironment = null;
                    if (element.TryGetProperty("workloadGithubEnvironment", out var workloadGithubElement) && workloadGithubElement.ValueKind == JsonValueKind.Object)
                    {
                        workloadGithubEnvironment = ParseGitHubEnvironment(workloadGithubElement, environmentCode);
                    }

                    if ((workloadIdentity == null) != (workloadGithubEnvironment == null))
                    {
                        throw new InvalidOperationException(
                            $"Environment '{environmentCode}' must declare both \"workloadUserAssignedIdentity\" and \"workloadGithubEnvironment\" together, or neither.");
                    }

                    environments.Add(new PlatformEnvironmentConfig
                    {
                        DisplayName = GetString(element, "displayName"),
                        EnvironmentCode = environmentCode,
                        ResourceGroupId = resourceGroupId,
                        UserAssignedIdentity = ParseUserAssignedIdentity(identityElement, environmentCode),
                        GitHubEnvironment = ParseGitHubEnvironment(githubElement, environmentCode),
                        WorkloadUserAssignedIdentity = workloadIdentity,
                        WorkloadGitHubEnvironment = workloadGithubEnvironment
                    });
                }

                return environments;
            }
        }

        private static void ValidateGlobalConfig(GlobalConfig config)
        {
            RequireGlobalConfigValue(config.TenantId, "tenantId");
            RequireGlobalConfigValue(config.SubscriptionId, "subscriptionId");
            RequireGlobalConfigValue(config.UniqueId, "uniqueId");
            RequireGlobalConfigValue(config.ServiceShort, "serviceShort");
            RequireGlobalConfigValue(config.DisplayName, "displayName");
            RequireGlobalConfigValue(config.Location, "location");
            RequireGlobalConfigValue(config.RegionCode, "regionCode");

            if (config.SourceControl == null)
            {
                throw new InvalidOperationException("global-config.jsonc must have a \"sourceControl\" object.");
            }

            RequireGlobalConfigValue(config.SourceControl.Tool, "sourceControl.tool");
            RequireGlobalConfigValue(config.SourceControl.Owner, "sourceControl.owner");
            RequireGlobalConfigValue(config.SourceControl.Repository, "sourceControl.repository");
            RequireGlobalConfigValue(config.SourceControl.BranchStrategy, "sourceControl.branchStrategy");
        }

        private static void RequireGlobalConfigValue(string value, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"global-config.jsonc must have a non-empty \"{propertyName}\".");
            }
        }

        private static PlatformGitHubEnvironmentConfig ParseGitHubEnvironment(JsonElement element, string environmentCode)
        {
            var name = GetString(element, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException($"Environment '{environmentCode}' githubEnvironment must have a non-empty \"name\".");
            }

            var githubEnvironment = new PlatformGitHubEnvironmentConfig { Name = name };

            if (element.TryGetProperty("protectionRules", out var protectionRulesElement) && protectionRulesElement.ValueKind == JsonValueKind.Object)
            {
                if (protectionRulesElement.TryGetProperty("requiredReviewers", out var reviewersElement) && reviewersElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var reviewerElement in reviewersElement.EnumerateArray())
                    {
                        if (reviewerElement.ValueKind == JsonValueKind.String)
                        {
                            githubEnvironment.RequiredReviewers.Add(reviewerElement.GetString());
                        }
                    }
                }

                if (protectionRulesElement.TryGetProperty("waitTimerMinutes", out var waitTimerElement) && waitTimerElement.ValueKind == JsonValueKind.Number)
                {
                    githubEnvironment.WaitTimerMinutes = waitTimerElement.GetInt32();
                }
            }

            if (element.TryGetProperty("secrets", out var secretsElement) && secretsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var secretElement in secretsElement.EnumerateArray())
                {
                    githubEnvironment.Secrets.Add(ParseKeyValue(secretElement, environmentCode, "secret"));
                }
            }

            if (element.TryGetProperty("variables", out var variablesElement) && variablesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var variableElement in variablesElement.EnumerateArray())
                {
                    githubEnvironment.Variables.Add(ParseKeyValue(variableElement, environmentCode, "variable"));
                }
            }

            return githubEnvironment;
        }

        private static PlatformKeyValueConfig ParseKeyValue(JsonElement element, string environmentCode, string kind)
        {
            var name = GetString(element, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException($"Environment '{environmentCode}' has a {kind} entry missing \"name\".");
            }

            return new PlatformKeyValueConfig { Name = name, Value = GetString(element, "value") ?? string.Empty };
        }

        private static PlatformUserAssignedIdentityConfig ParseUserAssignedIdentity(JsonElement element, string environmentCode)
        {
            var name = GetString(element, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException($"Environment '{environmentCode}' userAssignedIdentity must have a non-empty \"name\".");
            }

            if (!element.TryGetProperty("federatedCredential", out var federatedElement) || federatedElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException($"Environment '{environmentCode}' userAssignedIdentity must have a \"federatedCredential\" object.");
            }

            var identity = new PlatformUserAssignedIdentityConfig
            {
                Name = name,
                FederatedCredential = ParseFederatedCredential(federatedElement, environmentCode)
            };

            if (!element.TryGetProperty("roleAssignments", out var roleAssignmentsElement) ||
                roleAssignmentsElement.ValueKind != JsonValueKind.Array ||
                roleAssignmentsElement.GetArrayLength() == 0)
            {
                throw new InvalidOperationException($"Environment '{environmentCode}' userAssignedIdentity must have a non-empty \"roleAssignments\" array.");
            }

            foreach (var roleElement in roleAssignmentsElement.EnumerateArray())
            {
                var role = GetString(roleElement, "role");
                if (string.IsNullOrWhiteSpace(role))
                {
                    throw new InvalidOperationException($"Environment '{environmentCode}' has a role assignment missing \"role\".");
                }

                var (resourceGroupId, scope) = ParseRoleAssignmentScope(roleElement, $"Environment '{environmentCode}'");
                identity.RoleAssignments.Add(new PlatformRoleAssignmentConfig { Role = role, ResourceGroupId = resourceGroupId, Scope = scope });
            }

            return identity;
        }

        // A role assignment targets either a phase-1 resource group id, or the subscription scope; exactly one must be set
        private static (string ResourceGroupId, string Scope) ParseRoleAssignmentScope(JsonElement roleElement, string context)
        {
            var resourceGroupId = GetString(roleElement, "resourceGroupId");
            var scope = GetString(roleElement, "scope");

            var hasResourceGroupId = !string.IsNullOrWhiteSpace(resourceGroupId);
            var hasScope = !string.IsNullOrWhiteSpace(scope);

            if (hasResourceGroupId == hasScope)
            {
                throw new InvalidOperationException($"{context} has a role assignment that must set exactly one of \"resourceGroupId\" or \"scope\".");
            }

            if (hasScope && !string.Equals(scope, PlatformResourceGroupResolver.SubscriptionScope, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"{context} has a role assignment with unsupported \"scope\" value '{scope}'; only 'subscription' is supported.");
            }

            return (resourceGroupId, scope);
        }

        private static PlatformFederatedCredentialConfig ParseFederatedCredential(JsonElement element, string environmentCode)
        {
            var name = GetString(element, "name");
            var issuer = GetString(element, "issuer");
            var subjectType = GetString(element, "subjectType");

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subjectType))
            {
                throw new InvalidOperationException($"Environment '{environmentCode}' federatedCredential must have a non-empty \"name\", \"issuer\", and \"subjectType\".");
            }

            var federatedCredential = new PlatformFederatedCredentialConfig
            {
                Name = name,
                Issuer = issuer,
                SubjectType = subjectType
            };

            if (!element.TryGetProperty("audiences", out var audiencesElement) ||
                audiencesElement.ValueKind != JsonValueKind.Array ||
                audiencesElement.GetArrayLength() == 0)
            {
                throw new InvalidOperationException($"Environment '{environmentCode}' federatedCredential must have a non-empty \"audiences\" array.");
            }

            foreach (var audienceElement in audiencesElement.EnumerateArray())
            {
                if (audienceElement.ValueKind == JsonValueKind.String)
                {
                    federatedCredential.Audiences.Add(audienceElement.GetString());
                }
            }

            return federatedCredential;
        }

        // Replaces every ${key} token in the template with its value from the placeholder map; throws if a token has no known value
        internal static string ResolvePlaceholders(string template, IDictionary<string, string> placeholders)
        {
            if (string.IsNullOrEmpty(template))
            {
                return template;
            }

            return PlaceholderPattern.Replace(template, match =>
            {
                var key = match.Groups[1].Value;
                if (!placeholders.TryGetValue(key, out var value) || value == null)
                {
                    throw new InvalidOperationException($"Unresolved placeholder '${{{key}}}' in '{template}'. Add a value for '{key}' to global-config.jsonc.");
                }

                return value;
            });
        }

        private static string GetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
    }
}
