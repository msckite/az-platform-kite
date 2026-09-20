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
                    var mailNickName = GetString(element, "mailNickName");

                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        throw new InvalidOperationException("Each security group must have a non-empty \"displayName\".");
                    }

                    if (string.IsNullOrWhiteSpace(mailNickName))
                    {
                        throw new InvalidOperationException($"Security group '{displayName}' must have a non-empty \"mailNickName\".");
                    }

                    var securityGroup = new PlatformSecurityGroupConfig
                    {
                        DisplayName = displayName,
                        MailNickName = mailNickName,
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
                        var resourceGroupId = GetString(roleElement, "resourceGroupId");

                        if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(resourceGroupId))
                        {
                            throw new InvalidOperationException($"Security group '{displayName}' has a role assignment missing \"role\" or \"resourceGroupId\".");
                        }

                        securityGroup.RoleAssignments.Add(new PlatformRoleAssignmentConfig { Role = role, ResourceGroupId = resourceGroupId });
                    }

                    securityGroups.Add(securityGroup);
                }

                return securityGroups;
            }
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
