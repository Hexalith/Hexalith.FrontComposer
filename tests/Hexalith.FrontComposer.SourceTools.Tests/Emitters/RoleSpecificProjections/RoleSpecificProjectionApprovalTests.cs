using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters.RoleSpecificProjections;

/// <summary>
/// Story 4-1 T5.3 — approval baselines for the eight role-specific synthetic projections
/// declared in <see cref="RoleSpecificTestSources"/>. Each test drives the full
/// Parse → Transform → Emit pipeline via <see cref="FrontComposerGenerator"/> and verifies
/// the generated <c>.g.razor.cs</c> output plus the transform-stage diagnostic payload
/// against a <c>.verified.txt</c> snapshot.
/// </summary>
/// <remarks>
/// The snapshot is a composed string: the generated view source followed by a diagnostic
/// block that lists every non-error HFC*** diagnostic produced for the projection type.
/// Per T5.2 "Approvals for negative-path synthetics MUST capture the expected diagnostic
/// comments alongside the emitted file" — a single approval covers both surfaces so
/// role-specific regressions in either stage surface in one diff.
/// </remarks>
public class RoleSpecificProjectionApprovalTests {
    // ---------- Happy path ----------

    [Fact]
    public Task ActionQueueProjection_Approval()
        => VerifySynthetic(
            RoleSpecificTestSources.ActionQueueProjection,
            generatedFileName: "RoleSpecific.Orders.ActionQueueProjection.g.razor.cs");

    [Fact]
    public Task StatusOverviewProjection_Approval()
        => VerifySynthetic(
            RoleSpecificTestSources.StatusOverviewProjection,
            generatedFileName: "RoleSpecific.Tickets.StatusOverviewProjection.g.razor.cs");

    [Fact]
    public Task DetailRecordProjection_Approval()
        => VerifySynthetic(
            RoleSpecificTestSources.DetailRecordProjection,
            generatedFileName: "RoleSpecific.Customers.DetailRecordProjection.g.razor.cs");

    [Fact]
    public Task TimelineProjection_Approval()
        => VerifySynthetic(
            RoleSpecificTestSources.TimelineProjection,
            generatedFileName: "RoleSpecific.Audit.TimelineProjection.g.razor.cs");

    [Fact]
    public Task DashboardProjection_Approval()
        => VerifySynthetic(
            RoleSpecificTestSources.DashboardProjection,
            generatedFileName: "RoleSpecific.Metrics.DashboardProjection.g.razor.cs");

    // ---------- Negative path ----------

    [Fact]
    public Task ActionQueueNoEnumProjection_Approval()
        => VerifySynthetic(
            RoleSpecificTestSources.ActionQueueNoEnumProjection,
            generatedFileName: "RoleSpecific.Negative.ActionQueueNoEnumProjection.g.razor.cs");

    [Fact]
    public Task WhenStateTypoProjection_Approval()
        => VerifySynthetic(
            RoleSpecificTestSources.WhenStateTypoProjection,
            generatedFileName: "RoleSpecific.Negative.WhenStateTypoProjection.g.razor.cs");

    [Fact]
    public Task DashboardWrongShapeProjection_Approval()
        => VerifySynthetic(
            RoleSpecificTestSources.DashboardWrongShapeProjection,
            generatedFileName: "RoleSpecific.Negative.DashboardWrongShapeProjection.g.razor.cs");

    // ---------- Shared diagnostic expectations (named, minimal asserts kept separate from the snapshot) ----------

    [Fact]
    public void ActionQueueNoEnumProjection_EmitsHfc1022_ForMissingStatusEnum() {
        _ = ShouldContainSingleDiagnostic(
            RunGeneratorDiagnostics(RoleSpecificTestSources.ActionQueueNoEnumProjection),
            "HFC1022",
            DiagnosticSeverity.Warning,
            "ActionQueueNoEnumProjection",
            "requires an enum status property",
            "falls back to the unfiltered item list");
    }

    [Fact]
    public void WhenStateTypoProjection_EmitsHfc1022_ForUnknownMember() {
        _ = ShouldContainSingleDiagnostic(
            RunGeneratorDiagnostics(RoleSpecificTestSources.WhenStateTypoProjection),
            "HFC1022",
            DiagnosticSeverity.Warning,
            "Pendng",
            "Valid members: Approved, Pending, Submitted");
    }

    [Fact]
    public void DashboardWrongShapeProjection_EmitsHfc1023_ForDashboardFallback() {
        _ = ShouldContainSingleDiagnostic(
            RunGeneratorDiagnostics(RoleSpecificTestSources.DashboardWrongShapeProjection),
            "HFC1023",
            DiagnosticSeverity.Info,
            "Dashboard projection rendering is deferred to Story 6-3",
            "falls back to Default DataGrid rendering in v1");
    }

    // ---------- Helpers ----------

    private static Task VerifySynthetic(string source, string generatedFileName) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        (string generatedSource, IReadOnlyList<Diagnostic> diagnostics) = RunGenerator(source, generatedFileName, ct);

        StringBuilder snapshot = new();
        snapshot.AppendLine("=== Generated view ===");
        snapshot.Append(generatedSource);
        if (!generatedSource.EndsWith('\n')) {
            snapshot.AppendLine();
        }

        snapshot.AppendLine();
        snapshot.AppendLine("=== Diagnostics (HFC*** only) ===");
        IEnumerable<Diagnostic> fcDiagnostics = diagnostics
            .Where(d => d.Id.StartsWith("HFC", StringComparison.Ordinal))
            .OrderBy(d => d.Id, StringComparer.Ordinal)
            .ThenBy(d => d.GetMessage(), StringComparer.Ordinal);

        bool any = false;
        foreach (Diagnostic diagnostic in fcDiagnostics) {
            any = true;
            snapshot.Append(diagnostic.Id);
            snapshot.Append(' ');
            snapshot.Append(diagnostic.Severity);
            snapshot.Append(": ");
            snapshot.AppendLine(diagnostic.GetMessage());
        }
        if (!any) {
            snapshot.AppendLine("(none)");
        }

        return Verify(snapshot.ToString());
    }

    private static (string generatedSource, IReadOnlyList<Diagnostic> diagnostics) RunGenerator(
        string source,
        string generatedFileName,
        CancellationToken cancellationToken) {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        compilation.GetDiagnostics(cancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("Synthetic role-specific source must compile before generation. Errors: "
                + FormatDiagnostics(compilation.GetDiagnostics(cancellationToken)));

        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation, cancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("Generator should not produce error diagnostics for role-specific approvals. Errors: "
                + FormatDiagnostics(result.Diagnostics));

        CSharpCompilation outputCompilation = compilation.AddSyntaxTrees(result.GeneratedTrees.ToArray());
        outputCompilation.GetDiagnostics(cancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("Generated role-specific output must compile before it is approved. Errors: "
                + FormatDiagnostics(outputCompilation.GetDiagnostics(cancellationToken)));

        SyntaxTree? razorTree = result.GeneratedTrees
            .FirstOrDefault(t => Path.GetFileName(t.FilePath) == generatedFileName);
        razorTree.ShouldNotBeNull(
            $"Expected generator output '{generatedFileName}' but the driver produced: "
            + string.Join(", ", result.GeneratedTrees.Select(t => Path.GetFileName(t.FilePath))));

        string generatedSource = razorTree.GetText(cancellationToken).ToString();
        return (generatedSource, result.Diagnostics);
    }

    private static IReadOnlyList<Diagnostic> RunGeneratorDiagnostics(string source) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }

    private static Diagnostic ShouldContainSingleDiagnostic(
        IReadOnlyList<Diagnostic> diagnostics,
        string diagnosticId,
        DiagnosticSeverity expectedSeverity,
        params string[] expectedMessageFragments) {
        Diagnostic[] matches = diagnostics.Where(d => d.Id == diagnosticId).ToArray();
        matches.Length.ShouldBe(
            1,
            $"Expected exactly one {diagnosticId} diagnostic but saw: {FormatDiagnostics(diagnostics)}");

        Diagnostic match = matches[0];
        match.Severity.ShouldBe(expectedSeverity, $"{diagnosticId} should be {expectedSeverity}.");

        string message = match.GetMessage();
        foreach (string fragment in expectedMessageFragments) {
            message.ShouldContain(fragment);
        }

        return match;
    }

    private static string FormatDiagnostics(IEnumerable<Diagnostic> diagnostics)
        => string.Join(
            " | ",
            diagnostics.Select(d => {
                FileLinePositionSpan span = d.Location.GetLineSpan();
                string path = string.IsNullOrWhiteSpace(span.Path) ? "<unknown>" : Path.GetFileName(span.Path);
                int line = span.StartLinePosition.Line >= 0 ? span.StartLinePosition.Line + 1 : 0;
                return $"{d.Id} {d.Severity} @ {path}:{line} - {d.GetMessage()}";
            }));
}
