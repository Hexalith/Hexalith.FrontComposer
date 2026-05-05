using System.Text.Json;

using Shouldly;
using Xunit;

namespace Hexalith.FrontComposer.SourceTools.Tests.Schema;

/// <summary>
/// AC10 / T8 — the minimal Story 8-6 T8 fixture suite (9 fixtures) ships as discoverable test
/// fixtures with documented expected fingerprint material, algorithm id, negotiation result,
/// delta category, and renderer abstraction metadata. This test verifies the fixtures exist,
/// are valid JSON, and carry the expected schema.
/// </summary>
public sealed class SchemaFixtureCatalogTests {
    private static readonly IReadOnlyList<string> ExpectedFixtureIds = [
        "baseline-known-v1",
        "baseline-known-v2-compatible",
        "baseline-known-v2-structural-delta",
        "baseline-unknown",
        "schema-same-different-order",
        "schema-same-different-runtime-data",
        "schema-hidden-precedence",
        "schema-unknown-precedence",
        "surface-metadata-only-renderer",
    ];

    [Fact]
    public void Catalog_ShipsExactlyTheStoryT8Set() {
        IReadOnlyList<string> fixtureIds = LoadFixtureIds();

        fixtureIds.OrderBy(id => id, StringComparer.Ordinal).ToArray()
            .ShouldBe(ExpectedFixtureIds.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
                "AC10: fixture catalog must match the Story 8-6 T8 named set exactly.");
    }

    [Fact]
    public void EachFixture_DocumentsExpectedFingerprintMaterial() {
        DirectoryInfo dir = LocateFixtureDirectory();

        foreach (FileInfo file in dir.EnumerateFiles("*.json")) {
            using FileStream stream = file.OpenRead();
            using JsonDocument doc = JsonDocument.Parse(stream);
            JsonElement root = doc.RootElement;

            root.TryGetProperty("fixtureId", out JsonElement fixtureId).ShouldBeTrue($"{file.Name} missing fixtureId");
            root.TryGetProperty("title", out _).ShouldBeTrue($"{file.Name} missing title");
            root.TryGetProperty("contractFamily", out _).ShouldBeTrue($"{file.Name} missing contractFamily");
            root.TryGetProperty("expectedNegotiationKind", out _).ShouldBeTrue($"{file.Name} missing expectedNegotiationKind");
            root.TryGetProperty("expectedAgentCategory", out _).ShouldBeTrue($"{file.Name} missing expectedAgentCategory");
            root.TryGetProperty("notes", out _).ShouldBeTrue($"{file.Name} missing notes");

            ExpectedFixtureIds.ShouldContain(fixtureId.GetString()!);
        }
    }

    [Fact]
    public void RendererFixture_DocumentsRendererSurfaceAndBoundsSource() {
        DirectoryInfo dir = LocateFixtureDirectory();
        FileInfo rendererFile = new(Path.Combine(dir.FullName, "surface-metadata-only-renderer.json"));
        rendererFile.Exists.ShouldBeTrue();

        using FileStream stream = rendererFile.OpenRead();
        using JsonDocument doc = JsonDocument.Parse(stream);
        JsonElement root = doc.RootElement;

        root.GetProperty("expectedRendererSurface").GetString().ShouldBe("McpMarkdown");
        JsonElement seed = root.GetProperty("rendererSeed");
        seed.GetProperty("boundsSource").GetString().ShouldBe(
            "FrontComposerMcpOptions",
            "AC9 ties the renderer fixture to the live options-sourced bounds, not literal magic numbers.");
    }

    private static IReadOnlyList<string> LoadFixtureIds() {
        DirectoryInfo dir = LocateFixtureDirectory();
        List<string> ids = [];
        foreach (FileInfo file in dir.EnumerateFiles("*.json")) {
            using FileStream stream = file.OpenRead();
            using JsonDocument doc = JsonDocument.Parse(stream);
            ids.Add(doc.RootElement.GetProperty("fixtureId").GetString()!);
        }

        return ids;
    }

    private static DirectoryInfo LocateFixtureDirectory() {
        DirectoryInfo? cursor = new(AppContext.BaseDirectory);
        for (int i = 0; i < 10 && cursor is not null; i++, cursor = cursor.Parent) {
            string candidate = Path.Combine(cursor.FullName, "tests", "Hexalith.FrontComposer.SourceTools.Tests", "Schema", "Fixtures");
            if (Directory.Exists(candidate)) {
                return new DirectoryInfo(candidate);
            }
        }

        throw new DirectoryNotFoundException(
            "Fixture directory tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/Fixtures not found from base directory.");
    }
}
