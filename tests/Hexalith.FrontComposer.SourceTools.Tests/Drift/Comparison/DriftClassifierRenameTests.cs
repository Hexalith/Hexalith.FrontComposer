using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;
using Xunit;

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

    [Fact(Skip = SkipReason)]
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
        string message = renames[0].GetMessage();
        message.ShouldContain("Property 'OldLabel' was expected on TestDomain.OrderProjection but not found.");
        message.ShouldContain("'NewLabel' was added.");
        message.ShouldContain("If this is a rename, update the generated output.");
        message.ShouldMatch("See HFC1\\d{3}\\.");
    }

    [Fact(Skip = SkipReason)]
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

    [Fact(Skip = SkipReason)]
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

    private static IReadOnlyList<Diagnostic> Run(string source, string baselineJson) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        AdditionalText baselineText = new InMemoryAdditionalText("frontcomposer.drift-baseline.json", baselineJson);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()], additionalTexts: [baselineText]);
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }
}
