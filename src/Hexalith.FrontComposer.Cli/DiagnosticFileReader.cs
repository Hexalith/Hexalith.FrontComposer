using System.Text.Json;

namespace Hexalith.FrontComposer.Cli;

internal static class DiagnosticFileReader {
    internal const string UnreadableSidecarId = "HFCM0002";

    public static List<InspectDiagnostic>? Read(string projectDirectory, string generatedDirectory) {
        string[] paths;
        try {
            paths = Directory.GetFiles(generatedDirectory, "*.diagnostics.json", SearchOption.TopDirectoryOnly);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            return null;
        }

        List<InspectDiagnostic> result = [];
        foreach (string path in paths.Order(StringComparer.Ordinal)) {
            try {
                using FileStream stream = File.OpenRead(path);
                using var document = JsonDocument.Parse(stream);
                JsonElement root = document.RootElement;
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("diagnostics", out JsonElement diagnostics)) {
                    root = diagnostics;
                }

                if (root.ValueKind != JsonValueKind.Array) {
                    result.Add(SidecarUnreadable(path, projectDirectory, "Diagnostic sidecar must contain a diagnostics array."));
                    continue;
                }

                bool malformedEntry = false;
                foreach (JsonElement entry in root.EnumerateArray()) {
                    if (entry.ValueKind != JsonValueKind.Object) {
                        malformedEntry = true;
                        continue;
                    }

                    try {
                        string id = Get(entry, "id");
                        if (string.IsNullOrWhiteSpace(id)) {
                            malformedEntry = true;
                            continue;
                        }

                        if (!id.StartsWith("HFC", StringComparison.Ordinal)) {
                            continue;
                        }

                        if (entry.TryGetProperty("relatedType", out JsonElement relatedType)
                            && relatedType.ValueKind is not (JsonValueKind.Null or JsonValueKind.String)) {
                            malformedEntry = true;
                            continue;
                        }

                        result.Add(CreateDiagnostic(entry, id, relatedType, projectDirectory));
                    }
                    catch (InvalidOperationException) {
                        // A parsed JSON string can still fail when decoded.
                        malformedEntry = true;
                    }
                }

                if (malformedEntry) {
                    result.Add(SidecarUnreadable(path, projectDirectory, "Diagnostic sidecar contains unreadable entries."));
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

    private static InspectDiagnostic CreateDiagnostic(JsonElement entry, string id, JsonElement relatedType, string projectDirectory) {
        string? typeName = relatedType.ValueKind == JsonValueKind.String ? relatedType.GetString() : null;
        return new InspectDiagnostic(
            OutputSanitizer.Sanitize(id, 32),
            OutputSanitizer.Sanitize(Get(entry, "severity"), 16),
            string.IsNullOrWhiteSpace(typeName) ? null : OutputSanitizer.Sanitize(typeName, 160),
            OutputSanitizer.Sanitize(NormalizePath(Get(entry, "path"), projectDirectory), 240),
            OutputSanitizer.Sanitize(Get(entry, "what")),
            OutputSanitizer.Sanitize(Get(entry, "expected")),
            OutputSanitizer.Sanitize(Get(entry, "got")),
            OutputSanitizer.Sanitize(Get(entry, "fix")),
            OutputSanitizer.Sanitize(Get(entry, "docsLink")));
    }

    private static InspectDiagnostic SidecarUnreadable(string path, string projectDirectory, string what)
        => new(
            UnreadableSidecarId,
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

        try {
            return PathUtilities.ToProjectRelative(projectDirectory, Path.GetFullPath(trimmed, projectDirectory));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) {
            return PathUtilities.RedactedPathSentinel;
        }
    }
}
