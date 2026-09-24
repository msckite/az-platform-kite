using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MSCKite.Azure.Platform.Internal.Common
{
    internal sealed class WorkflowManifestFile
    {
        internal string Source { get; set; }

        internal string Destination { get; set; }
    }

    internal sealed class WorkflowManifestStrategy
    {
        internal string Name { get; set; }

        internal List<string> Environments { get; } = new List<string>();

        internal List<WorkflowManifestFile> Files { get; } = new List<WorkflowManifestFile>();
    }

    internal sealed class WorkflowManifestSet
    {
        internal List<WorkflowManifestFile> Shared { get; } = new List<WorkflowManifestFile>();

        internal List<WorkflowManifestFile> Files { get; } = new List<WorkflowManifestFile>();

        internal Dictionary<string, WorkflowManifestStrategy> Strategies { get; } =
            new Dictionary<string, WorkflowManifestStrategy>(StringComparer.OrdinalIgnoreCase);
    }

    internal sealed class WorkflowManifest
    {
        internal string Path { get; set; }

        internal Version TemplateVersion { get; set; }

        internal WorkflowManifestSet Platform { get; } = new WorkflowManifestSet();

        internal WorkflowManifestSet Project { get; } = new WorkflowManifestSet();
    }

    // Reads templates/github/workflows/manifest.jsonc, the source/destination map for the workflow templates
    internal static class WorkflowManifestLoader
    {
        private static readonly JsonDocumentOptions DocumentOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        internal static WorkflowManifest Load(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Workflow manifest not found: {path}", path);
            }

            var manifest = new WorkflowManifest
            {
                Path = path,
                TemplateVersion = TemplateVersionHelper.ReadVersion(path, "templateVersion")
            };

            using (var document = JsonDocument.Parse(File.ReadAllText(path), DocumentOptions))
            {
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException($"'{path}' must contain a JSON object.");
                }

                if (root.TryGetProperty("platform", out var platformElement))
                {
                    ReadWorkflowSet(platformElement, path, "platform", manifest.Platform);
                }
                else
                {
                    ReadWorkflowSet(root, path, "legacy platform", manifest.Platform);
                }

                if (root.TryGetProperty("project", out var projectElement))
                {
                    ReadWorkflowSet(projectElement, path, "project", manifest.Project);
                }

                if (manifest.Platform.Files.Count == 0 && manifest.Project.Strategies.Count == 0)
                {
                    throw new InvalidOperationException($"'{path}' must declare at least one platform or project strategy.");
                }
            }

            return manifest;
        }

        private static void ReadWorkflowSet(JsonElement element, string path, string propertyName, WorkflowManifestSet workflowSet)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException($"'{propertyName}' in '{path}' must be an object.");
            }

            if (element.TryGetProperty("shared", out var sharedElement))
            {
                ReadFiles(sharedElement, path, $"{propertyName}.shared", workflowSet.Shared);
            }

            if (element.TryGetProperty("files", out var filesElement))
            {
                ReadFiles(filesElement, path, $"{propertyName}.files", workflowSet.Files);
            }

            if (element.TryGetProperty("strategies", out var strategiesElement))
            {
                if (strategiesElement.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException($"'{propertyName}.strategies' in '{path}' must be an object.");
                }

                foreach (var property in strategiesElement.EnumerateObject())
                {
                    if (property.Value.ValueKind != JsonValueKind.Object)
                    {
                        throw new InvalidOperationException($"Strategy '{property.Name}' in '{path}' must be an object.");
                    }

                    var strategy = new WorkflowManifestStrategy { Name = property.Name };

                    if (property.Value.TryGetProperty("environments", out var environmentsElement) &&
                        environmentsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var environment in environmentsElement.EnumerateArray())
                        {
                            if (environment.ValueKind == JsonValueKind.String)
                            {
                                strategy.Environments.Add(environment.GetString());
                            }
                        }
                    }

                    if (!property.Value.TryGetProperty("files", out var strategyFilesElement))
                    {
                        throw new InvalidOperationException($"Strategy '{property.Name}' in '{path}' must contain a \"files\" array.");
                    }

                    ReadFiles(strategyFilesElement, path, $"{propertyName}.strategies.{property.Name}.files", strategy.Files);

                    if (strategy.Files.Count == 0)
                    {
                        throw new InvalidOperationException($"Strategy '{property.Name}' in '{path}' must list at least one file.");
                    }

                    workflowSet.Strategies[property.Name] = strategy;
                }
            }

            if (workflowSet.Files.Count == 0 && workflowSet.Strategies.Count == 0)
            {
                throw new InvalidOperationException($"'{propertyName}' in '{path}' must declare files or strategies.");
            }
        }

        private static void ReadFiles(JsonElement element, string path, string propertyName, List<WorkflowManifestFile> files)
        {
            if (element.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException($"'{propertyName}' in '{path}' must be an array.");
            }

            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException($"Each entry in '{propertyName}' of '{path}' must be an object.");
                }

                var source = GetString(item, "source");
                var destination = GetString(item, "destination");

                if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(destination))
                {
                    throw new InvalidOperationException($"Each entry in '{propertyName}' of '{path}' must have a non-empty \"source\" and \"destination\".");
                }

                files.Add(new WorkflowManifestFile
                {
                    Source = ValidateRelativePath(source, path, propertyName),
                    Destination = ValidateRelativePath(destination, path, propertyName)
                });
            }
        }

        // Manifest paths are always repository-relative, so absolute or traversing paths are rejected before any file is touched
        private static string ValidateRelativePath(string value, string path, string propertyName)
        {
            var normalized = value.Trim().Replace('\\', '/').Trim('/');

            if (normalized.Length == 0 ||
                System.IO.Path.IsPathRooted(normalized) ||
                normalized.Contains(":") ||
                Array.Exists(normalized.Split('/'), segment => segment == ".."))
            {
                throw new InvalidOperationException($"'{value}' in '{propertyName}' of '{path}' must be a relative path without '..' segments.");
            }

            return normalized;
        }

        private static string GetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
    }
}
