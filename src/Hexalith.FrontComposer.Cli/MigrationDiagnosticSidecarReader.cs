using System.Collections.Immutable;
using System.Text.Json;

using Microsoft.CodeAnalysis;

namespace Hexalith.FrontComposer.Cli;

internal static class MigrationDiagnosticSidecarReader {
    private const string SentinelPrefix = "__sidecar__/";

    public static (IReadOnlyDictionary<string, ImmutableArray<Diagnostic>> Diagnostics, IReadOnlySet<string> UnreadablePaths)
        Read(string projectDirectory) {
        // D7 (Story 9-2 third pass): manual-migration HFCM9002 sidecars are read here as a
        // **test-only synthetic** contract until Story 9-4 governs the final HFC ID assignment
        // and SourceTools generator emits real adopter sidecars. AC11 fires today only against
        // hand-crafted fixtures (`tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs`).
        // See Known Gaps row "P-D4 SourceTools-emitted manual-only diagnostic" — owner Story 9-4.
        string objDirectory = Path.Combine(projectDirectory, "obj");
        Dictionary<string, ImmutableArray<Diagnostic>.Builder> builders = new(PathUtilities.PathComparer);
        HashSet<string> unreadablePaths = new(PathUtilities.PathComparer);
        List<string> sidecarPaths = [];
        Stack<string> pendingDirectories = new();
        try {
            _ = File.GetAttributes(objDirectory);
            pendingDirectories.Push(objDirectory);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException) {
            // A project that has never been built normally has no obj directory.
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            AddSentinel(unreadablePaths, objDirectory, projectDirectory);
        }

        while (pendingDirectories.TryPop(out string? directory)) {
            try {
                foreach (string child in Directory.EnumerateDirectories(directory)) {
                    pendingDirectories.Push(child);
                }

                sidecarPaths.AddRange(Directory.EnumerateFiles(directory, "*.diagnostics.json")
                    .Where(IsGeneratedDiagnosticsSidecar));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
                AddSentinel(unreadablePaths, objDirectory, projectDirectory);
            }
        }

        foreach (string path in sidecarPaths.Order(StringComparer.Ordinal)) {
            try {
                using FileStream stream = File.OpenRead(path);
                using var document = JsonDocument.Parse(stream);
                JsonElement entries = document.RootElement;
                if (entries.ValueKind == JsonValueKind.Object && entries.TryGetProperty("diagnostics", out JsonElement diagnostics)) {
                    entries = diagnostics;
                }

                if (entries.ValueKind != JsonValueKind.Array) {
                    AddSentinel(unreadablePaths, path, projectDirectory);
                    continue;
                }

                foreach (JsonElement entry in entries.EnumerateArray()) {
                    try {
                        string id = entry.ValueKind == JsonValueKind.Object ? Get(entry, "id") : string.Empty;
                        if (string.IsNullOrWhiteSpace(id)) {
                            AddSentinel(unreadablePaths, path, projectDirectory);
                            continue;
                        }

                        if (!string.Equals(id, MigrationDiagnostics.ManualMigration.Id, StringComparison.Ordinal)) {
                            continue;
                        }

                        string? relativePath = NormalizeSourcePath(Get(entry, "path"), projectDirectory);
                        if (relativePath is null) {
                            AddSentinel(unreadablePaths, path, projectDirectory);
                            continue;
                        }

                        // Preserve the sidecar's source-side message in `Properties` so the planner
                        // can surface it via the resulting MigrationEntry.What.
                        string what = Get(entry, "what");
                        ImmutableDictionary<string, string?> properties = string.IsNullOrEmpty(what)
                            ? ImmutableDictionary<string, string?>.Empty
                            : ImmutableDictionary<string, string?>.Empty.Add("what", what);
                        if (!builders.TryGetValue(relativePath, out ImmutableArray<Diagnostic>.Builder? builder)) {
                            builder = ImmutableArray.CreateBuilder<Diagnostic>();
                            builders.Add(relativePath, builder);
                        }

                        builder.Add(Diagnostic.Create(
                            MigrationDiagnostics.ManualMigration,
                            Location.None,
                            properties: properties,
                            messageArgs: null));
                    }
                    catch (InvalidOperationException) {
                        // JSON string decoding can fail after the surrounding document has been parsed.
                        AddSentinel(unreadablePaths, path, projectDirectory);
                    }
                }
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or InvalidOperationException) {
                AddSentinel(unreadablePaths, path, projectDirectory);
            }
        }

        return (builders.ToDictionary(x => x.Key, x => x.Value.ToImmutable(), PathUtilities.PathComparer), unreadablePaths);
    }

    private static void AddSentinel(HashSet<string> unreadablePaths, string sidecarPath, string projectDirectory)
        => unreadablePaths.Add(SentinelPrefix + (NormalizeSourcePath(sidecarPath, projectDirectory) ?? Path.GetFileName(sidecarPath)));

    private static bool IsGeneratedDiagnosticsSidecar(string path) {
        string normalized = path.Replace('\\', '/');
        return normalized.Contains("/generated/HexalithFrontComposer/", PathUtilities.PathComparison);
    }

    private static string Get(JsonElement entry, string property)
        => entry.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static string? NormalizeSourcePath(string path, string projectDirectory) {
        if (string.IsNullOrWhiteSpace(path) || path.Any(char.IsControl) || path.Contains("://", StringComparison.Ordinal)) {
            return null;
        }

        string normalized = path.Replace('\\', '/');
        bool hasDrivePrefix = normalized.Length >= 2 && char.IsAsciiLetter(normalized[0]) && normalized[1] == ':';
        if (((hasDrivePrefix || Path.IsPathRooted(normalized)) && !Path.IsPathFullyQualified(normalized))
            || (!OperatingSystem.IsWindows() && path.StartsWith('\\'))
            || normalized.Split('/').Contains("..", StringComparer.Ordinal)) {
            return null;
        }

        try {
            string fullPath = Path.GetFullPath(normalized, projectDirectory);
            string relative = PathUtilities.ToProjectRelative(projectDirectory, fullPath);
            return relative is "" or PathUtilities.RedactedPathSentinel ? null : relative;
        }
        catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException) {
            return null;
        }
    }
}
