using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;
using Xunit;

namespace Hexalith.FrontComposer.SourceTools.Tests.Drift.Comparison;

/// <summary>
/// AC2 + AC3 / T3 — projection property add/remove classification. Activation runs
/// the FrontComposerGenerator with a checked-in baseline `AdditionalText` next to a
/// modified `[Projection]` source and asserts the new HFC drift diagnostics carry
/// projection name, member name, and one of the documented affected-surface labels.
/// </summary>
public sealed class DriftClassifierProjectionPropertyTests {
    private const string SkipReason = "RED-PHASE: T3 — projection drift classifier not yet introduced.";

    private const string BaselineWithPriorityAndDueDate = """
        {
          "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
          "algorithm": "frontcomposer-structural-v1",
          "contracts": [
            {
              "family": "projection",
              "type": "TestDomain.OrderProjection",
              "boundedContext": "Orders",
              "properties": [
                { "name": "Id",       "category": "String",         "nullable": false, "columnPriority": 0 },
                { "name": "Priority", "category": "Enum",           "nullable": false, "columnPriority": 1 },
                { "name": "DueDate",  "category": "DateTimeOffset", "nullable": true,  "columnPriority": 2 }
              ]
            }
          ]
        }
        """;

    [Fact()]
    public void Removed_DataGridProperty_EmitsDriftDiagnostic_WithDeclarationAndMemberAndSurface() {
        string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                public string Id { get; set; } = string.Empty;
                public OrderPriority Priority { get; set; }
            }
            public enum OrderPriority { Low, High }
            """;

        IReadOnlyList<Diagnostic> diagnostics = RunGeneratorWithBaseline(source, BaselineWithPriorityAndDueDate);

        Diagnostic[] removeDrifts = [.. diagnostics.Where(d => d.Id.StartsWith("HFC10", StringComparison.Ordinal)
                                                            && d.GetMessage().Contains("DueDate", StringComparison.Ordinal))];
        removeDrifts.Length.ShouldBe(1, "AC2 emits exactly one diagnostic per removed property.");
        removeDrifts[0].GetMessage().ShouldContain("OrderProjection");
        removeDrifts[0].GetMessage().ShouldContain("DueDate");
        // AC2 — message must list documented affected surfaces. Story 9-1 review CB-35: the
        // earlier `Any` substring check passed on incidental "column" matches (e.g. "ColumnPriority"
        // in unrelated metadata text). For the canonical removed-DateTimeOffset case the
        // production message says: "Affected surface: DataGrid column, detail field, MCP descriptor".
        // Pin the assertion to require BOTH `DataGrid` and `detail` so a regression that emits
        // only metadata-style prose is caught.
        string message = removeDrifts[0].GetMessage();
        message.ShouldContain("Affected surface", Case.Insensitive,
            customMessage: "AC2 — diagnostic must label the affected surface explicitly.");
        message.ShouldContain("DataGrid", Case.Insensitive,
            customMessage: "AC2 — DateTimeOffset removal affects the DataGrid column surface.");
        message.ShouldContain("detail", Case.Insensitive,
            customMessage: "AC2 — DateTimeOffset removal affects the detail field surface.");
    }

    [Theory()]
    [InlineData("public int RowVersion { get; set; }",      "DataGrid")]
    [InlineData("public string Notes { get; set; } = \"\";", "detail")]
    [InlineData("public IReadOnlyList<string> Tags { get; } = [];", "unsupported")]
    public void Added_Property_ClassifiesIntoDocumentedSurfaceLabel(string addedProperty, string expectedSurface) {
        string source = $$"""
            using System.Collections.Generic;
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                public string Id { get; set; } = string.Empty;
                public OrderPriority Priority { get; set; }
                public System.DateTimeOffset? DueDate { get; set; }
                {{addedProperty}}
            }
            public enum OrderPriority { Low, High }
            """;

        IReadOnlyList<Diagnostic> diagnostics = RunGeneratorWithBaseline(source, BaselineWithPriorityAndDueDate);

        Diagnostic[] addDrifts = [.. diagnostics.Where(d => d.Id.StartsWith("HFC10", StringComparison.Ordinal)
                                                         && d.GetMessage().Contains("added", StringComparison.OrdinalIgnoreCase))];
        addDrifts.Length.ShouldBeGreaterThanOrEqualTo(1, "AC3 emits a diagnostic for each added property.");
        addDrifts[0].GetMessage().ShouldContain(expectedSurface, Case.Insensitive);
    }

    [Fact()]
    public void NoDrift_WhenSourceMatchesBaselineExactly() {
        string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                public string Id { get; set; } = string.Empty;
                public OrderPriority Priority { get; set; }
                public System.DateTimeOffset? DueDate { get; set; }
            }
            public enum OrderPriority { Low, High }
            """;

        IReadOnlyList<Diagnostic> diagnostics = RunGeneratorWithBaseline(source, BaselineWithPriorityAndDueDate);

        diagnostics.Where(d => d.Id.StartsWith("HFC10", StringComparison.Ordinal)
                            && (d.GetMessage().Contains("added", StringComparison.OrdinalIgnoreCase)
                                || d.GetMessage().Contains("removed", StringComparison.OrdinalIgnoreCase)))
            .ShouldBeEmpty("Equal shape ⇒ no drift diagnostics.");
    }

    private static IReadOnlyList<Diagnostic> RunGeneratorWithBaseline(string source, string baselineJson) {
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

    public sealed class InMemoryAdditionalText(string path, string text) : AdditionalText {
        public override string Path { get; } = path;
        public override Microsoft.CodeAnalysis.Text.SourceText? GetText(CancellationToken cancellationToken = default)
            => Microsoft.CodeAnalysis.Text.SourceText.From(text);
    }
}
