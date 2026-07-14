using System.Text.Json;

namespace Hexalith.FrontComposer.Cli;

internal static class DiagnosticFileReader {
    public static IEnumerable<InspectDiagnostic> Read(string projectDirectory, string generatedDirectory) {
        List<InspectDiagnostic> result = [];
        foreach (string path in Directory.EnumerateFiles(generatedDirectory, "*.diagnostics.json", SearchOption.TopDirectoryOnly)
                     .Order(StringComparer.Ordinal)) {
            try {
                using FileStream stream = File.OpenRead(path);
                using var document = JsonDocument.Parse(stream);
                JsonElement root = document.RootElement;
                IEnumerable<JsonElement> entries = root.ValueKind == JsonValueKind.Array
                    ? root.EnumerateArray()
                    : root.TryGetProperty("diagnostics", out JsonElement diagnostics) && diagnostics.ValueKind == JsonValueKind.Array
                        ? diagnostics.EnumerateArray()
                        : [];

                foreach (JsonElement entry in entries) {
                    string id = Get(entry, "id");
                    if (!id.StartsWith("HFC", StringComparison.Ordinal)) {
                        continue;
                    }

                    result.Add(new InspectDiagnostic(
                        OutputSanitizer.Sanitize(id, 32),
                        OutputSanitizer.Sanitize(Get(entry, "severity"), 16),
                        OutputSanitizer.Sanitize(Get(entry, "relatedType"), 160),
                        OutputSanitizer.Sanitize(NormalizePath(Get(entry, "path"), projectDirectory), 240),
                        OutputSanitizer.Sanitize(Get(entry, "what")),
                        OutputSanitizer.Sanitize(Get(entry, "expected")),
                        OutputSanitizer.Sanitize(Get(entry, "got")),
                        OutputSanitizer.Sanitize(Get(entry, "fix")),
                        OutputSanitizer.Sanitize(Get(entry, "docsLink"))));
                }
            }
            catch (JsonException) {
                result.Add(SidecarUnreadable(path, projectDirectory, "Diagnostic sidecar JSON could not be parsed."));
            }
            catch (IOException) {
                result.Add(SidecarUnreadable(path, projectDirectory, "Diagnostic sidecar could not be read."));
            }
            catch (UnauthorizedAccessException) {
                result.Add(SidecarUnreadable(path, projectDirectory, "Diagnostic sidecar could not be read."));
            }
        }

        return result;
    }

    private static InspectDiagnostic SidecarUnreadable(string path, string projectDirectory, string what)
        => new(
            "HFCM0002",
            "Warning",
            null,
            OutputSanitizer.Sanitize(NormalizePath(path, projectDirectory), 240),
            what,
            "Diagnostic sidecars must be valid JSON arrays or { diagnostics: [] } documents.",
            "Sidecar parsing failed.",
            "Re-run the build, or delete the corrupt sidecar.",
            "docs/migrations/index.md");

    private static string Get(JsonElement entry, string property)
        => entry.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static string NormalizePath(string path, string projectDirectory) {
        if (string.IsNullOrWhiteSpace(path)) {
            return string.Empty;
        }

        string trimmed = path.Trim();
        if (trimmed.Length >= 2 && char.IsAsciiLetter(trimmed[0]) && trimmed[1] == ':') {
            return PathUtilities.RedactedPathSentinel;
        }

        if (trimmed.Contains("://", StringComparison.Ordinal)) {
            return PathUtilities.RedactedPathSentinel;
        }

        string fullPath;
        try {
            fullPath = Path.IsPathRooted(trimmed)
                ? trimmed
                : Path.GetFullPath(trimmed, projectDirectory);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) {
            return PathUtilities.RedactedPathSentinel;
        }

        return PathUtilities.ToProjectRelative(projectDirectory, fullPath);
    }
}
