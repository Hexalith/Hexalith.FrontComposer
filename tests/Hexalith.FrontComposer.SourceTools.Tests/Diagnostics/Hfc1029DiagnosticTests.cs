using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Diagnostics;

/// <summary>
/// Story 4-4 T9.5 / D15 / D22 — generator-level assertions for the HFC1029
/// FcColumnPrioritizer activation diagnostic. Per-projection dedupe + Information
/// severity + strict <c>&gt; 15</c> threshold (exactly 15 does NOT trigger per D6).
/// </summary>
public class Hfc1029DiagnosticTests {
    [Fact]
    public void FiresOnce_WhenProjectionHasMoreThan15Columns_PayloadNamesHiddenDefaults() {
        string source = BuildProjection(columnCount: 20);

        IReadOnlyList<Diagnostic> diagnostics = RunGenerator(source);

        Diagnostic[] hfc1029 = [.. diagnostics.Where(d => d.Id == "HFC1029")];
        hfc1029.Length.ShouldBe(1);
        hfc1029[0].Severity.ShouldBe(DiagnosticSeverity.Info);

        string message = hfc1029[0].GetMessage();
        message.ShouldContain("OrderProjection");
        message.ShouldContain("20", Case.Sensitive);
        // Columns at positions 11+ (indices 10..19) are hidden by default: Field11..Field20.
        message.ShouldContain("Field11");
        message.ShouldContain("Field20");
    }

    [Fact]
    public void DoesNotFire_AtExactly15Columns_StrictInequalityBoundary() {
        string source = BuildProjection(columnCount: 15);

        IReadOnlyList<Diagnostic> diagnostics = RunGenerator(source);

        diagnostics.Any(d => d.Id == "HFC1029").ShouldBeFalse();
    }

    [Fact]
    public void DedupesPerProjection_OneDiagnosticRegardlessOfColumnCount() {
        // D22 / per-projection dedupe — the diagnostic emits exactly once even when the
        // projection carries many hidden-by-default columns.
        string source = BuildProjection(columnCount: 40);

        IReadOnlyList<Diagnostic> diagnostics = RunGenerator(source);

        diagnostics.Count(d => d.Id == "HFC1029").ShouldBe(1);
    }

    private static string BuildProjection(int columnCount) {
        StringBuilder source = new();
        _ = source.AppendLine("namespace TestDomain");
        _ = source.AppendLine("{");
        _ = source.AppendLine("    using Hexalith.FrontComposer.Contracts.Attributes;");
        _ = source.AppendLine();
        _ = source.AppendLine("    [Projection(\"Orders\")]");
        _ = source.AppendLine("    public partial class OrderProjection");
        _ = source.AppendLine("    {");
        for (int i = 1; i <= columnCount; i++) {
            _ = source.AppendLine("        public string Field" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + " { get; set; } = string.Empty;");
        }

        _ = source.AppendLine("    }");
        _ = source.AppendLine("}");
        return source.ToString();
    }

    private static IReadOnlyList<Diagnostic> RunGenerator(string source) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }
}
