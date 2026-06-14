using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

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

        // Story 9-1 review CB-36: AC6 inherits the AC2/AC5 default-Warning severity contract.
        contextDrifts[0].Severity.ShouldBe(DiagnosticSeverity.Warning,
            "AC6 — bounded context rename emits Warning by default.");

        string message = contextDrifts[0].GetMessage();
        message.ShouldContain("Sales", Case.Insensitive);
        message.ShouldContain("Orders", Case.Insensitive);
        foreach (string surface in RequiredAffectedSurfaces) {
            message.ShouldContain(surface, Case.Insensitive);
        }
    }

    [Fact()]
    public void BoundedContext_RemovedAttributeOnSource_FailsClosedOrEmitsRenameWithEmptyGot() {
        // Story 9-1 review CB-22: production reads boundedContext with `?? string.Empty`.
        // When the source has no [BoundedContext] (parsed as empty/whitespace), the comparison
        // against a baseline-with-context should still produce a deterministic outcome — either
        // a rename diagnostic listing the empty value or a structural drift, never silent.
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Sales",
                "properties": [{ "name": "Id", "category": "String", "nullable": false }] }] }
            """;
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [Projection]
            public partial class OrderProjection {
                public string Id { get; set; } = string.Empty;
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        // Removing the attribute may produce a bounded-context drift (renamed Sales→empty), or
        // a structural drift (declaration identity changes because identity includes context).
        // Either is acceptable — silent absence of a diagnostic is not.
        diagnostics.Any(d => d.Id.StartsWith("HFC10", StringComparison.Ordinal)
                          && (d.GetMessage().Contains("Sales", StringComparison.Ordinal)
                           || d.GetMessage().Contains("OrderProjection", StringComparison.Ordinal)))
            .ShouldBeTrue("CB-22 — removing [BoundedContext] from source must surface a deterministic drift diagnostic, not silent.");
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
            generators: [generator.AsSourceGenerator()],
            additionalTexts: [baselineText],
            optionsProvider: CompilationHelper.DriftEnabledOptions());
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }
}
