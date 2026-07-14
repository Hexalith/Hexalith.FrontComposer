using System.Collections.Immutable;
using System.Text.Json;

using Microsoft.CodeAnalysis;

namespace Hexalith.FrontComposer.Cli;

internal static class MigrationDiagnosticSidecarReader {
    public const string SentinelPrefix = "__sidecar__/";

    public static IReadOnlyDictionary<string, ImmutableArray<Diagnostic>> Read(string projectDirectory) {
        // D7 (Story 9-2 third pass): manual-migration HFCM9002 sidecars are read here as a
        // **test-only synthetic** contract until Story 9-4 governs the final HFC ID assignment
        // and SourceTools generator emits real adopter sidecars. AC11 fires today only against
        // hand-crafted fixtures (`tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs`).
        // See Known Gaps row "P-D4 SourceTools-emitted manual-only diagnostic" — owner Story 9-4.
        string objDirectory = Path.Combine(projectDirectory, "obj");
        if (!Directory.Exists(objDirectory)) {
            return new Dictionary<string, ImmutableArray<Diagnostic>>(PathUtilities.PathComparer);
        }

        Dictionary<string, ImmutableArray<Diagnostic>.Builder> builders = new(PathUtilities.PathComparer);
        IEnumerable<string> sidecarPaths;
        try {
            sidecarPaths = Directory.EnumerateFiles(objDirectory, "*.diagnostics.json", SearchOption.AllDirectories)
                .Where(IsGeneratedDiagnosticsSidecar)
                .Order(StringComparer.Ordinal)
                .ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            return new Dictionary<string, ImmutableArray<Diagnostic>>(PathUtilities.PathComparer);
        }

        foreach (string path in sidecarPaths) {
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
                    if (!IsManualMigrationDiagnostic(entry)) {
                        continue;
                    }

                    string relativePath = NormalizePath(Get(entry, "path"), projectDirectory);
                    if (string.IsNullOrWhiteSpace(relativePath) || relativePath == PathUtilities.RedactedPathSentinel) {
                        AddSentinel(builders, path, projectDirectory);
                        continue;
                    }

                    if (!builders.TryGetValue(relativePath, out ImmutableArray<Diagnostic>.Builder? builder)) {
                        builder = ImmutableArray.CreateBuilder<Diagnostic>();
                        builders.Add(relativePath, builder);
                    }

                    // Preserve the sidecar's source-side message in `Properties` so the planner
                    // can surface it via the resulting MigrationEntry.What.
                    string what = Get(entry, "what");
                    ImmutableDictionary<string, string?> properties = string.IsNullOrEmpty(what)
                        ? ImmutableDictionary<string, string?>.Empty
                        : ImmutableDictionary<string, string?>.Empty.Add("what", what);
                    builder.Add(Diagnostic.Create(
                        MigrationDiagnostics.ManualMigration,
                        Location.None,
                        properties: properties,
                        messageArgs: null));
                }
            }
            catch (JsonException) {
                // Surface a single sentinel entry per unreadable sidecar instead of silently dropping it.
                AddSentinel(builders, path, projectDirectory);
            }
            catch (IOException) {
                AddSentinel(builders, path, projectDirectory);
            }
            catch (UnauthorizedAccessException) {
                AddSentinel(builders, path, projectDirectory);
            }
        }

        return builders.ToDictionary(
            x => x.Key,
            x => x.Value.ToImmutable(),
            PathUtilities.PathComparer);
    }

    private static void AddSentinel(
        Dictionary<string, ImmutableArray<Diagnostic>.Builder> builders,
        string sidecarPath,
        string projectDirectory) {
        string sentinelKey = NormalizePath(sidecarPath, projectDirectory);
        if (string.IsNullOrWhiteSpace(sentinelKey) || sentinelKey == PathUtilities.RedactedPathSentinel) {
            sentinelKey = OutputSanitizer.Sanitize(Path.GetFileName(sidecarPath), 120);
        }

        sentinelKey = SentinelPrefix + sentinelKey;

        if (!builders.TryGetValue(sentinelKey, out ImmutableArray<Diagnostic>.Builder? builder)) {
            builder = ImmutableArray.CreateBuilder<Diagnostic>();
            builders.Add(sentinelKey, builder);
        }

        builder.Add(Diagnostic.Create(MigrationDiagnostics.ManualMigration, Location.None));
    }

    private static bool IsGeneratedDiagnosticsSidecar(string path) {
        string normalized = path.Replace('\\', '/');
        return normalized.Contains("/generated/HexalithFrontComposer/", PathUtilities.PathComparison);
    }

    private static bool IsManualMigrationDiagnostic(JsonElement entry)
        => string.Equals(Get(entry, "id"), MigrationDiagnostics.ManualMigration.Id, StringComparison.Ordinal);

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

        string relative = Path.IsPathRooted(path)
            ? PathUtilities.ToProjectRelative(projectDirectory, path)
            : trimmed.Replace('\\', '/').TrimStart('/');
        // Reject `..` segments after normalization; AC23 disallows project-root escape.
        if (relative.Split('/').Any(segment => segment == "..")) {
            return PathUtilities.RedactedPathSentinel;
        }

        return OutputSanitizer.Sanitize(relative, 240);
    }
}
