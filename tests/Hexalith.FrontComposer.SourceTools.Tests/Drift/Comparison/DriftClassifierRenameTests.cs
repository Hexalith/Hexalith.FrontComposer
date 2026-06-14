using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

using static Hexalith.FrontComposer.SourceTools.Tests.Drift.Comparison.DriftClassifierProjectionPropertyTests;

namespace Hexalith.FrontComposer.SourceTools.Tests.Drift.Comparison;

/// <summary>
/// AC4 / T3 — likely-rename heuristic. Deterministic 1:1 only: exactly one removed +
/// exactly one added of compatible category in the same stable declaration identity ⇒
/// rename diagnostic with the verbatim Epic 9 message wording. Ambiguous cases must
/// degrade into separate add/remove diagnostics.
/// </summary>
public sealed class DriftClassifierRenameTests {
    private const string SkipReason = "RED-PHASE: T3 — rename heuristic not yet introduced.";

    [Fact()]
    public void OneRemoved_OneAdded_CompatibleCategory_EmitsRenameWithEpic9Wording() {
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [
                  { "name": "Id",       "category": "String", "nullable": false, "columnPriority": 0 },
                  { "name": "OldLabel", "category": "String", "nullable": false, "columnPriority": 1 }
                ] }] }
            """;
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                public string Id { get; set; } = string.Empty;
                public string NewLabel { get; set; } = string.Empty;
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        Diagnostic[] renames = [.. diagnostics.Where(d => d.GetMessage().Contains("rename", StringComparison.OrdinalIgnoreCase))];
        renames.Length.ShouldBe(1, "AC4 — 1:1 deterministic match emits exactly one rename diagnostic.");
        // Epic 9 verbatim wording shape (story §AC4):
        // "Property '{OldName}' was expected on {TypeName} but not found. '{NewName}' was added.
        //  If this is a rename, update the generated output. See HFC{id}."
        // Story 9-1 review CB-5: anchor with regex enforcing the entire sentence shape so a
        // regression that drops single quotes, reorders sentences, or drops the trailing
        // "See HFC..." pointer is caught. The `[\s\S]*?` between sentences allows for an
        // optional drift-prefix preamble emitted by the production message template.
        string message = renames[0].GetMessage();
        message.ShouldMatch(
            @"Property 'OldLabel' was expected on TestDomain\.OrderProjection but not found\.[\s\S]*?'NewLabel' was added\.[\s\S]*?If this is a rename, update the generated output\.[\s\S]*?See HFC1\d{3}\.",
            customMessage: "AC4 — full Epic 9 wording shape with single-quoted member names, period-terminated sentences, and trailing 'See HFC####.' pointer must be preserved.");
    }

    [Fact()]
    public void MultipleAdded_MultipleRemoved_NoDeterministicMatch_DegradesToAddRemovePairs() {
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [
                  { "name": "OldA", "category": "String", "nullable": false },
                  { "name": "OldB", "category": "String", "nullable": false }
                ] }] }
            """;
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                public string NewA { get; set; } = string.Empty;
                public string NewB { get; set; } = string.Empty;
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        diagnostics.Any(d => d.GetMessage().Contains("rename", StringComparison.OrdinalIgnoreCase))
            .ShouldBeFalse("AC4 — ambiguous matches MUST NOT be classified as renames.");
        diagnostics.Count(d => d.GetMessage().Contains("not found", StringComparison.OrdinalIgnoreCase)).ShouldBe(2);
        diagnostics.Count(d => d.GetMessage().Contains("added", StringComparison.OrdinalIgnoreCase)).ShouldBe(2);
    }

    [Fact()]
    public void OneRemoved_OneAdded_IncompatibleCategory_NotClassifiedAsRename() {
        // String → Enum is not a rename — categories differ; degrade to add+remove.
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [{ "name": "Status", "category": "String", "nullable": false }] }] }
            """;
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                public OrderState Phase { get; set; }
            }
            public enum OrderState { New, Done }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        diagnostics.Any(d => d.GetMessage().Contains("rename", StringComparison.OrdinalIgnoreCase))
            .ShouldBeFalse();
    }

    [Theory()]
    // Story 9-1 review CB-23: pin the rename-heuristic compatibility boundary so a regression
    // that broadens (or narrows) the "compatible enough to rename" rule is caught.
    // Numeric within a width family (Int32→Int64): documented as a TYPE change on the same
    // member name; cross-name numeric pairs are NOT collapsed into a rename, they degrade to
    // add+remove. Same for DateTime→DateTimeOffset and String→String? (nullability-only).
    [InlineData("Int32", "Int64", "false", "false")]
    [InlineData("DateTime", "DateTimeOffset", "false", "false")]
    public void OneRemoved_OneAdded_NumericOrTemporalBoundary_NotClassifiedAsRename(
        string baselineCategory, string addedCategory, string baselineNullable, string addedNullable) {
        string baseline = $$"""
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [{ "name": "OldField", "category": "{{baselineCategory}}", "nullable": {{baselineNullable}} }] }] }
            """;
        string source = $$"""
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                public {{ClrTypeFor(addedCategory, addedNullable)}} NewField { get; set; }{{Initializer(addedCategory, addedNullable)}}
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        diagnostics.Any(d => d.GetMessage().Contains("rename", StringComparison.OrdinalIgnoreCase))
            .ShouldBeFalse($"AC4 boundary — {baselineCategory}→{addedCategory} cross-name change must NOT collapse into a rename.");
        diagnostics.Count(d => d.GetMessage().Contains("not found", StringComparison.OrdinalIgnoreCase)).ShouldBe(1,
            $"AC4 boundary — {baselineCategory}→{addedCategory} must surface OldField as removed.");
        diagnostics.Count(d => d.GetMessage().Contains("added", StringComparison.OrdinalIgnoreCase)
                            && d.GetMessage().Contains("NewField", StringComparison.Ordinal)).ShouldBe(1,
            $"AC4 boundary — {baselineCategory}→{addedCategory} must surface NewField as added.");
    }

    private static string ClrTypeFor(string category, string nullable) => (category, nullable) switch {
        ("Int32", "false") => "int",
        ("Int64", "false") => "long",
        ("DateTime", "false") => "System.DateTime",
        ("DateTimeOffset", "false") => "System.DateTimeOffset",
        ("String", "true") => "string?",
        ("String", "false") => "string",
        _ => "object",
    };

    private static string Initializer(string category, string nullable)
        => category == "String" && nullable == "false" ? " = string.Empty;" : string.Empty;

    private static IReadOnlyList<Diagnostic> Run(string source, string baselineJson) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        AdditionalText baselineText = new InMemoryAdditionalText("frontcomposer.drift-baseline.json", baselineJson);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: [baselineText],
            optionsProvider: CompilationHelper.DriftEnabledOptions());
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }
}
