using System.Text.Json;
using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Docs;

/// <summary>
/// Guards the FC-NIP command-target decision from the shared language-neutral manifest.
/// </summary>
[Trait("Category", "Governance")]
public sealed class FcNipCommandTargetIdentityContractTests {
    private const string ManifestPath = "tests/contract-fixtures/fc-nip-command-target-identity-contract.json";
    private const string StoryNineTwoPath = "_bmad-output/implementation-artifacts/9-2-wire-fcnewitemindicator-producer-and-generated-grid-consumer.md";
    private const string IndicatorStateServicePath = "src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs";

    [Fact]
    public void SharedManifest_WhenApplied_PinsDocumentsAndExactTables() {
        using JsonDocument manifest = LoadAndValidateManifest(ReadRaw(ManifestPath));

        foreach (JsonElement document in manifest.RootElement.GetProperty("documents").EnumerateArray()) {
            string content = ReadNormalized(document.GetProperty("path").GetString()!);
            foreach (JsonElement fragment in document.GetProperty("contains").EnumerateArray()) {
                content.ShouldContain(NormalizeWhitespace(fragment.GetString()!), Case.Sensitive);
            }

            foreach (JsonElement fragment in document.GetProperty("notContains").EnumerateArray()) {
                content.ShouldNotContain(NormalizeWhitespace(fragment.GetString()!), Case.Sensitive);
            }
        }

        foreach (JsonElement table in manifest.RootElement.GetProperty("tables").EnumerateArray()) {
            string[][] expectedRows = table.GetProperty("rows").EnumerateArray()
                .Select(static row => row.EnumerateArray().Select(static cell => cell.GetString()!).ToArray())
                .ToArray();
            AssertTableRows(
                ReadRaw(table.GetProperty("path").GetString()!),
                table.GetProperty("heading").GetString()!,
                expectedRows);
        }
    }

    [Theory]
    [InlineData("backslash")]
    [InlineData("empty-tables")]
    [InlineData("duplicate-table")]
    [InlineData("row-width")]
    [InlineData("duplicate-cell")]
    public void SharedManifest_WhenStructurallyUnsafe_IsRejected(string scenario) {
        string json = scenario switch {
            "backslash" => ManifestJson("docs\\escape.md", "[" + TableJson(2) + "]"),
            "empty-tables" => ManifestJson("docs/example.md", "[]"),
            "duplicate-table" => ManifestJson("docs/example.md", "[" + TableJson(2) + "," + TableJson(2) + "]"),
            "row-width" => ManifestJson("docs/example.md", "[" + TableJson(2, "[\"a\"]") + "]"),
            "duplicate-cell" => ManifestJson("docs/example.md", "[" + TableJson(2, "[\"a\",\"a\"]") + "]"),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };

        Should.Throw<InvalidDataException>(() => LoadAndValidateManifest(json));
    }

    [Fact]
    public void ExistingSourceEvidence_WhenReviewed_ShowsTheConvergedProducerBoundary() {
        string rowIdentity = ReadNormalized("src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandRowIdentity.cs");
        string eventStoreStatusQuery = ReadNormalized("src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStorePendingCommandStatusQuery.cs");
        string commandFormEmitter = ReadNormalized("src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs");
        string razorEmitter = ReadNormalized("src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs");
        string storyNineTwo = ReadNormalized(StoryNineTwoPath);
        string indicatorState = ReadNormalized(IndicatorStateServicePath);

        AssertContainsAll(rowIdentity, "projection row identity cascaded to generated command forms", "It must not be populated from raw", "command payloads or user-editable form values");
        AssertContainsAll(eventStoreStatusQuery, "MessageId: pendingCommand.MessageId", "string? AggregateId", "int? EventCount");
        foreach (string forbidden in new[] { "EntityKey:", "ProjectionTypeName:", "LaneKey:", "ExpectedStatusSlot:", "PriorStatusSlot:" }) {
            eventStoreStatusQuery.ShouldNotContain(forbidden, Case.Sensitive);
        }

        AssertContainsAll(
            commandFormEmitter,
            "CascadingParameter",
            "CommandTypeName: typeof(",
            "form.CommandTarget?.ResolutionMode == CommandTargetResolutionMode.SameAsSource",
            "ResolveCommandTargetAsync(_model, cts.Token)",
            "var commandForDispatch = targetResolution.Command",
            "PendingCommandOutcomeResolver.AssociateAccepted",
            "PendingCommandOutcomeResolver.Resolve");
        foreach (string forbidden in new[] { "PendingCommandState.ResolveTerminal", "PendingCommandState.Register", "EntityKey: status.AggregateId", "ResultPayload" }) {
            commandFormEmitter.ShouldNotContain(forbidden, Case.Sensitive);
        }

        AssertContainsAll(razorEmitter, "PendingCommandRowIdentityFor(row)", "CascadingValue<global::Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandRowIdentity?>");
        AssertContainsAll(storyNineTwo, "Status: done", "FrontComposer-owned pending-command row metadata", "Source-level wiring was proven", "Do not hide FC-NIP row identity in optional EventStore/domain-defined `ResultPayload`");
        indicatorState.ShouldContain("DefaultLifetime = TimeSpan.FromSeconds(10)", Case.Sensitive);
    }

    private static JsonDocument LoadAndValidateManifest(string json) {
        JsonDocument document;
        try {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException exception) {
            throw new InvalidDataException("The FC-NIP contract manifest is not valid JSON.", exception);
        }

        try {
            JsonElement root = document.RootElement;
            RequireObjectProperties(root, "schemaVersion", "documents", "tables");
            Require(root.GetProperty("schemaVersion").ValueKind == JsonValueKind.Number && root.GetProperty("schemaVersion").GetInt32() == 1, "schemaVersion must be 1.");
            JsonElement documents = root.GetProperty("documents");
            JsonElement tables = root.GetProperty("tables");
            Require(documents.ValueKind == JsonValueKind.Array && documents.GetArrayLength() > 0, "documents must be a non-empty array.");
            Require(tables.ValueKind == JsonValueKind.Array && tables.GetArrayLength() > 0, "tables must be a non-empty array.");

            HashSet<string> documentPaths = new(StringComparer.Ordinal);
            foreach (JsonElement manifestDocument in documents.EnumerateArray()) {
                RequireObjectProperties(manifestDocument, "path", "contains", "notContains");
                string path = RequireSafePath(manifestDocument.GetProperty("path"));
                Require(documentPaths.Add(path), $"Duplicate document path '{path}'.");
                ValidateFragments(manifestDocument.GetProperty("contains"), "contains");
                ValidateFragments(manifestDocument.GetProperty("notContains"), "notContains");
            }

            HashSet<string> tableIdentities = new(StringComparer.Ordinal);
            foreach (JsonElement table in tables.EnumerateArray()) {
                RequireObjectProperties(table, "path", "heading", "rows");
                string path = RequireSafePath(table.GetProperty("path"));
                Require(documentPaths.Contains(path), $"Table path '{path}' must also be declared in documents.");
                string heading = RequireNormalizedString(table.GetProperty("heading"), "table heading");
                Require(tableIdentities.Add(path + "\n" + heading), $"Duplicate table identity '{path}' / '{heading}'.");
                JsonElement rows = table.GetProperty("rows");
                Require(rows.ValueKind == JsonValueKind.Array && rows.GetArrayLength() > 0, "table rows must be a non-empty array.");
                int? width = null;
                foreach (JsonElement row in rows.EnumerateArray()) {
                    Require(row.ValueKind == JsonValueKind.Array && row.GetArrayLength() > 0, "Each table row must be a non-empty array.");
                    width ??= row.GetArrayLength();
                    Require(row.GetArrayLength() == width, "Table rows have inconsistent widths.");
                    HashSet<string> cells = new(StringComparer.Ordinal);
                    foreach (JsonElement cell in row.EnumerateArray()) {
                        string value = RequireNormalizedString(cell, "table cell");
                        Require(cells.Add(value), $"Duplicate table cell '{value}'.");
                    }
                }
            }

            return document;
        }
        catch {
            document.Dispose();
            throw;
        }
    }

    private static void ValidateFragments(JsonElement fragments, string name) {
        Require(fragments.ValueKind == JsonValueKind.Array, $"{name} must be an array.");
        HashSet<string> unique = new(StringComparer.Ordinal);
        foreach (JsonElement fragment in fragments.EnumerateArray()) {
            string value = RequireNormalizedString(fragment, name + " fragment");
            Require(unique.Add(value), $"Duplicate {name} fragment '{value}'.");
        }
    }

    private static string RequireSafePath(JsonElement element) {
        string path = RequireNormalizedString(element, "path");
        Require(!path.Contains('\\') && !Path.IsPathRooted(path), $"Manifest path '{path}' is not repository-relative POSIX syntax.");
        Require(path.Split('/').All(static part => part.Length > 0 && part is not "." and not ".."), $"Manifest path '{path}' contains traversal or empty segments.");
        return path;
    }

    private static string RequireNormalizedString(JsonElement element, string name) {
        Require(element.ValueKind == JsonValueKind.String, $"{name} must be a string.");
        string value = element.GetString()!;
        Require(value.Length > 0 && string.Equals(value, NormalizeWhitespace(value), StringComparison.Ordinal), $"{name} must be non-empty and whitespace-normalized.");
        return value;
    }

    private static void RequireObjectProperties(JsonElement element, params string[] names) {
        Require(element.ValueKind == JsonValueKind.Object, "Manifest node must be an object.");
        string[] actual = element.EnumerateObject().Select(static property => property.Name).Order(StringComparer.Ordinal).ToArray();
        string[] expected = names.Order(StringComparer.Ordinal).ToArray();
        Require(actual.SequenceEqual(expected, StringComparer.Ordinal), $"Manifest object properties must be exactly: {string.Join(", ", expected)}.");
    }

    private static void Require(bool condition, string message) {
        if (!condition) {
            throw new InvalidDataException(message);
        }
    }

    private static string ManifestJson(string path, string tables)
        => $$"""{"schemaVersion":1,"documents":[{"path":"{{path.Replace("\\", "\\\\", StringComparison.Ordinal)}}","contains":[],"notContains":[]}],"tables":{{tables}}}""";

    private static string TableJson(int headerWidth, string? secondRow = null) {
        string firstRow = "[" + string.Join(',', Enumerable.Range(0, headerWidth).Select(static index => "\"x" + index + "\"")) + "]";
        return $$"""{"path":"docs/example.md","heading":"## Table","rows":[{{firstRow}},{{secondRow ?? firstRow}}]}""";
    }

    private static void AssertContainsAll(string document, params string[] expectedFragments) {
        foreach (string fragment in expectedFragments) {
            document.ShouldContain(fragment, Case.Sensitive);
        }
    }

    private static void AssertTableRows(string document, string heading, params string[][] expectedRows) {
        string[][] actualRows = ParseTableRows(document, heading);
        actualRows.ShouldBe(expectedRows);
    }

    private static string[][] ParseTableRows(string document, string heading) {
        string[] lines = document.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        int headingIndex = Array.FindIndex(lines, line => string.Equals(line.Trim(), heading, StringComparison.Ordinal));
        Require(headingIndex >= 0, $"'{heading}' heading is missing.");
        int sectionEnd = Array.FindIndex(lines, headingIndex + 1, line => line.TrimStart().StartsWith('#'));
        sectionEnd = sectionEnd < 0 ? lines.Length : sectionEnd;
        int headerIndex = Array.FindIndex(lines, headingIndex + 1, sectionEnd - headingIndex - 1, line => line.TrimStart().StartsWith('|'));
        Require(headerIndex >= 0 && headerIndex + 2 <= sectionEnd, $"'{heading}' table is missing or truncated.");
        Require(Regex.IsMatch(lines[headerIndex + 1].Trim(), @"^\|[\s:|-]+\|$"), $"'{heading}' table is missing its separator row.");
        int width = SplitTableRow(lines[headerIndex]).Length;
        List<string[]> rows = [];
        for (int index = headerIndex + 2; index < sectionEnd && lines[index].TrimStart().StartsWith('|'); index++) {
            string[] row = SplitTableRow(lines[index]);
            Require(row.Length == width, $"'{heading}' table row has an inconsistent width.");
            rows.Add(row);
        }

        return [.. rows];
    }

    private static string[] SplitTableRow(string line)
        => Regex.Replace(line.Trim(), @"^\||\|$", string.Empty).Split('|').Select(static cell => cell.Trim()).ToArray();

    private static string ReadNormalized(string relative) => NormalizeWhitespace(ReadRaw(relative));

    private static string ReadRaw(string relative) => File.ReadAllText(Absolute(relative));

    private static string NormalizeWhitespace(string value) => Regex.Replace(value, @"\s+", " ");

    private static string Absolute(string relative) => Path.Combine(ProjectRoot(), relative.Replace('/', Path.DirectorySeparatorChar));

    private static string ProjectRoot() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.FrontComposer.slnx"))) {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root could not be found.");
    }
}
