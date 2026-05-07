using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;
using Xunit;

using static Hexalith.FrontComposer.SourceTools.Tests.Drift.Comparison.DriftClassifierProjectionPropertyTests;

namespace Hexalith.FrontComposer.SourceTools.Tests.Drift.Comparison;

/// <summary>
/// AC6 / T3 — bounded-context name change classification. A single diagnostic must list every
/// affected surface (navigation grouping, generated registration, persisted session grouping,
/// MCP resource grouping, badge/action-queue grouping). The diagnostic carries both the
/// previous and current context names.
/// </summary>
public sealed class DriftClassifierBoundedContextTests {
    private const string SkipReason = "RED-PHASE: T3 — bounded-context drift classifier not yet introduced.";

    private static readonly string[] RequiredAffectedSurfaces = [
        "navigation",
        "registration",
        "session",
        "MCP",
        "action queue",
    ];

    [Fact()]
    public void BoundedContextRename_EmitsSingleDiagnosticListingAllAffectedSurfaces() {
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Sales",
                "properties": [{ "name": "Id", "category": "String", "nullable": false }] }] }
            """;
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                public string Id { get; set; } = string.Empty;
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        Diagnostic[] contextDrifts = [.. diagnostics.Where(d => d.GetMessage().Contains("BoundedContext", StringComparison.Ordinal)
                                                              || d.GetMessage().Contains("bounded context", StringComparison.OrdinalIgnoreCase))];
        contextDrifts.Length.ShouldBe(1, "AC6: exactly one diagnostic per bounded-context rename.");

        string message = contextDrifts[0].GetMessage();
        message.ShouldContain("Sales", Case.Insensitive);
        message.ShouldContain("Orders", Case.Insensitive);
        foreach (string surface in RequiredAffectedSurfaces) {
            message.ShouldContain(surface, Case.Insensitive);
        }
    }

    [Fact()]
    public void BoundedContext_Stable_NoDiagnostic() {
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [{ "name": "Id", "category": "String", "nullable": false }] }] }
            """;
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                public string Id { get; set; } = string.Empty;
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        diagnostics.Any(d => d.GetMessage().Contains("bounded context", StringComparison.OrdinalIgnoreCase))
            .ShouldBeFalse("Equal context name ⇒ no diagnostic.");
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
