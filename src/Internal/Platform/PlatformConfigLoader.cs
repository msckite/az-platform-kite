using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using MSCKite.Azure.Platform.Internal.Common;

namespace MSCKite.Azure.Platform.Internal.Platform
{
    // Loads global-config.jsonc and the platform config files (resource-groups, security-groups, environments), and resolves ${placeholder} tokens shared across both
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
                throw new FileNotFoundException($"Config file not found: {path}", path);
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
                throw new FileNotFoundException($"Config file not found: {path}", path);
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
                throw new FileNotFoundException($"Config file not found: {path}", path);
            }

            TemplateVersionHelper.ReadVersion(path, "templateVersion");

            using (var document = JsonDocument.Parse(ApplyEnvironmentDefaults(File.ReadAllText(path), path), DocumentOptions))
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

                    if (!element.TryGetProperty("pipelines", out var pipelinesElement) ||
                        pipelinesElement.ValueKind != JsonValueKind.Array ||
                        pipelinesElement.GetArrayLength() == 0)
                    {
                        throw new InvalidOperationException($"Environment '{environmentCode}' must have a non-empty \"pipelines\" array.");
                    }

                    var environment = new PlatformEnvironmentConfig
                    {
                        DisplayName = GetString(element, "displayName"),
                        EnvironmentCode = environmentCode,
                        ResourceGroupId = resourceGroupId
                    };

                    foreach (var pipelineElement in pipelinesElement.EnumerateArray())
                    {
                        environment.Pipelines.Add(ParsePipeline(pipelineElement, environmentCode, environment.Pipelines));
                    }

                    environments.Add(environment);
                }

                return environments;
            }
        }

        // A pipeline bundles the identity with the GitHub environment that receives its client id, so the two can never be paired up wrongly.
        // Names must differ within an environment, or two pipelines would overwrite each other's identity, credential, or GitHub environment secrets.
        private static PlatformPipelineConfig ParsePipeline(JsonElement element, string environmentCode, List<PlatformPipelineConfig> siblings)
        {
            var purpose = GetString(element, "purpose");
            if (string.IsNullOrWhiteSpace(purpose))
            {
                throw new InvalidOperationException($"Environment '{environmentCode}' has a pipeline without a non-empty \"purpose\".");
            }

            if (siblings.Any(sibling => string.Equals(sibling.Purpose, purpose, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException($"Environment '{environmentCode}' has more than one pipeline with purpose '{purpose}'.");
            }

            if (!element.TryGetProperty("userAssignedIdentity", out var identityElement) || identityElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException($"Environment '{environmentCode}' pipeline '{purpose}' must have a \"userAssignedIdentity\" object.");
            }

            if (!element.TryGetProperty("githubEnvironment", out var githubElement) || githubElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException($"Environment '{environmentCode}' pipeline '{purpose}' must have a \"githubEnvironment\" object.");
            }

            var pipeline = new PlatformPipelineConfig
            {
                Purpose = purpose,
                UserAssignedIdentity = ParseUserAssignedIdentity(identityElement, environmentCode),
                GitHubEnvironment = ParseGitHubEnvironment(githubElement, environmentCode)
            };

            foreach (var sibling in siblings)
            {
                RequireDistinct(pipeline.UserAssignedIdentity.Name, sibling.UserAssignedIdentity.Name, "userAssignedIdentity name", environmentCode, purpose, sibling.Purpose);
                RequireDistinct(pipeline.UserAssignedIdentity.FederatedCredential.Name, sibling.UserAssignedIdentity.FederatedCredential.Name, "federatedCredential name", environmentCode, purpose, sibling.Purpose);
                RequireDistinct(pipeline.GitHubEnvironment.Name, sibling.GitHubEnvironment.Name, "githubEnvironment name", environmentCode, purpose, sibling.Purpose);
            }

            return pipeline;
        }

        private static void RequireDistinct(string value, string siblingValue, string what, string environmentCode, string purpose, string siblingPurpose)
        {
            if (string.Equals(value, siblingValue, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Environment '{environmentCode}' pipelines '{siblingPurpose}' and '{purpose}' must not share the same {what} ('{value}').");
            }
        }

        // Folds the optional top-level "defaults" object of environments.jsonc into every environment, so the rest of the loader sees fully spelled-out entries.
        // A value set on an environment always wins; the defaults only fill what it leaves out. Secrets and variables are merged by name.
        private static string ApplyEnvironmentDefaults(string json, string path)
        {
            if (!(JsonNode.Parse(json, null, DocumentOptions) is JsonObject root) || !root.ContainsKey("defaults"))
            {
                return json;
            }

            if (!(root["defaults"] is JsonObject defaults))
            {
                throw new InvalidOperationException($"'{path}' has a \"defaults\" property that must be an object.");
            }

            if (root["environments"] is JsonArray environments)
            {
                foreach (var environment in environments.OfType<JsonObject>())
                {
                    ApplyDefaultsToEnvironment(environment, defaults);
                }
            }

            return root.ToJsonString();
        }

        private static void ApplyDefaultsToEnvironment(JsonObject environment, JsonObject defaults)
        {
            if (!(environment["pipelines"] is JsonArray pipelines))
            {
                return;
            }

            foreach (var pipeline in pipelines.OfType<JsonObject>())
            {
                if (defaults["federatedCredential"] is JsonObject credentialDefaults &&
                    pipeline["userAssignedIdentity"] is JsonObject identity &&
                    identity["federatedCredential"] is JsonObject credential)
                {
                    FillMissingProperties(credential, credentialDefaults);
                }

                if (defaults["githubEnvironment"] is JsonObject githubDefaults && pipeline["githubEnvironment"] is JsonObject github)
                {
                    ApplyDefaultsToGitHubEnvironment(github, githubDefaults);
                }
            }
        }

        private static void ApplyDefaultsToGitHubEnvironment(JsonObject github, JsonObject defaults)
        {
            if (defaults["protectionRules"] is JsonObject ruleDefaults)
            {
                if (github["protectionRules"] is JsonObject rules)
                {
                    FillMissingProperties(rules, ruleDefaults);
                }
                else if (!github.ContainsKey("protectionRules"))
                {
                    github["protectionRules"] = ruleDefaults.DeepClone();
                }
            }

            MergeNamedEntries(github, defaults, "secrets");
            MergeNamedEntries(github, defaults, "variables");
        }

        private static void FillMissingProperties(JsonObject target, JsonObject defaults)
        {
            foreach (var property in defaults)
            {
                if (!target.ContainsKey(property.Key))
                {
                    target[property.Key] = property.Value?.DeepClone();
                }
            }
        }

        // Puts the default entries first, then the environment's own entries; an own entry replaces a default with the same name
        private static void MergeNamedEntries(JsonObject target, JsonObject defaults, string propertyName)
        {
            if (!(defaults[propertyName] is JsonArray defaultEntries) || defaultEntries.Count == 0)
            {
                return;
            }

            if (target.ContainsKey(propertyName) && !(target[propertyName] is JsonArray))
            {
                return;
            }

            var ownEntries = target[propertyName] as JsonArray ?? new JsonArray();
            var ownNames = new HashSet<string>(
                ownEntries.OfType<JsonObject>().Select(entry => entry["name"] is JsonValue name && name.TryGetValue<string>(out var value) ? value : null).Where(name => name != null),
                StringComparer.OrdinalIgnoreCase);

            var merged = new JsonArray();
            foreach (var entry in defaultEntries)
            {
                var name = entry is JsonObject entryObject && entryObject["name"] is JsonValue nameValue && nameValue.TryGetValue<string>(out var value) ? value : null;
                if (name == null || !ownNames.Contains(name))
                {
                    merged.Add(entry?.DeepClone());
                }
            }

            foreach (var entry in ownEntries)
            {
                merged.Add(entry?.DeepClone());
            }

            target[propertyName] = merged;
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

        // An empty policy would block every deployment to the environment, so require at least one branch or tag pattern
        private static PlatformDeploymentBranchPolicyConfig ParseDeploymentBranchPolicy(JsonElement element, string environmentName)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException($"GitHub environment '{environmentName}' protectionRules.deploymentBranchPolicy must be an object with \"branches\" and/or \"tags\" arrays.");
            }

            var policy = new PlatformDeploymentBranchPolicyConfig();
            ParsePatterns(element, "branches", policy.Branches, environmentName);
            ParsePatterns(element, "tags", policy.Tags, environmentName);

            if (policy.Branches.Count == 0 && policy.Tags.Count == 0)
            {
                throw new InvalidOperationException($"GitHub environment '{environmentName}' protectionRules.deploymentBranchPolicy must list at least one branch or tag pattern; omit deploymentBranchPolicy to leave the environment's branch policy unmanaged.");
            }

            return policy;
        }

        private static void ParsePatterns(JsonElement element, string propertyName, List<string> patterns, string environmentName)
        {
            if (!element.TryGetProperty(propertyName, out var patternsElement))
            {
                return;
            }

            if (patternsElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException($"GitHub environment '{environmentName}' protectionRules.deploymentBranchPolicy.{propertyName} must be an array of name patterns.");
            }

            foreach (var patternElement in patternsElement.EnumerateArray())
            {
                var pattern = patternElement.ValueKind == JsonValueKind.String ? patternElement.GetString() : null;
                if (string.IsNullOrWhiteSpace(pattern))
                {
                    throw new InvalidOperationException($"GitHub environment '{environmentName}' protectionRules.deploymentBranchPolicy.{propertyName} must only contain non-empty name patterns.");
                }

                if (!patterns.Contains(pattern))
                {
                    patterns.Add(pattern);
                }
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

                if (protectionRulesElement.TryGetProperty("deploymentBranchPolicy", out var branchPolicyElement) && branchPolicyElement.ValueKind != JsonValueKind.Null)
                {
                    githubEnvironment.DeploymentBranchPolicy = ParseDeploymentBranchPolicy(branchPolicyElement, name);
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
                throw new InvalidOperationException($"Environment '{environmentCode}' federatedCredential must have a non-empty \"name\", \"issuer\", and \"subjectType\" (set them on the credential or under \"defaults.federatedCredential\").");
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
                throw new InvalidOperationException($"Environment '{environmentCode}' federatedCredential must have a non-empty \"audiences\" array (set it on the credential or under \"defaults.federatedCredential\").");
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
