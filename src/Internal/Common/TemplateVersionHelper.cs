using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MSCKite.Azure.Platform.Internal.Common
{
    internal static class TemplateVersionHelper
    {
        private static readonly Regex SemanticVersionPattern = new Regex(
            @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$",
            RegexOptions.Compiled);

        internal static Version ReadVersion(string path, string propertyName)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"File not found: {path}", path);
            }

            var options = new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            using (var document = JsonDocument.Parse(File.ReadAllText(path), options))
            {
                if (document.RootElement.ValueKind != JsonValueKind.Object ||
                    !document.RootElement.TryGetProperty(propertyName, out var versionElement) ||
                    versionElement.ValueKind != JsonValueKind.String)
                {
                    throw new InvalidOperationException($"'{path}' must contain a string '{propertyName}' property.");
                }

                var versionText = versionElement.GetString();
                if (string.IsNullOrWhiteSpace(versionText) || !SemanticVersionPattern.IsMatch(versionText) ||
                    !Version.TryParse(versionText, out var version))
                {
                    throw new InvalidOperationException($"'{path}' has an invalid '{propertyName}' value '{versionText}'. Expected Major.Minor.Patch SemVer.");
                }

                return version;
            }
        }
    }
}
