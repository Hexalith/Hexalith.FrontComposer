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
    public void ActionQueueNoEnumProjection_EmitsHfc1022_ForMissingStatusEnum() => _ = ShouldContainSingleDiagnostic(
            RunGeneratorDiagnostics(RoleSpecificTestSources.ActionQueueNoEnumProjection),
            "HFC1022",
            DiagnosticSeverity.Warning,
            "ActionQueueNoEnumProjection",
            "requires an enum status property",
            "falls back to the unfiltered item list");

    [Fact]
    public void WhenStateTypoProjection_EmitsHfc1022_ForUnknownMember() => _ = ShouldContainSingleDiagnostic(
            RunGeneratorDiagnostics(RoleSpecificTestSources.WhenStateTypoProjection),
            "HFC1022",
            DiagnosticSeverity.Warning,
            "Pendng",
            "Valid members: Approved, Pending, Submitted");

    [Fact]
    public void DashboardWrongShapeProjection_EmitsHfc1023_ForDashboardFallback() => _ = ShouldContainSingleDiagnostic(
            RunGeneratorDiagnostics(RoleSpecificTestSources.DashboardWrongShapeProjection),
            "HFC1023",
            DiagnosticSeverity.Info,
            "Dashboard projection rendering is deferred to Story 6-3",
            "falls back to Default DataGrid rendering in v1");

    // ---------- Story 2.1 AC2 — positive render/emit invariants (named, separate from the snapshot) ----------

    [Fact]
    public void ActionQueueProjection_EmitsWhenStateFilter_ForNamedMembers() {
        // AC2 positive WhenState pin — complements WhenStateTypoProjection_EmitsHfc1022 (the
        // negative/unknown-member pin above). The ActionQueue role must filter the rendered item
        // list down to the enum members named in [ProjectionRole(..., WhenState = "Pending,Submitted")].
        (string generatedSource, _) = RunGenerator(
            RoleSpecificTestSources.ActionQueueProjection,
            "RoleSpecific.Orders.ActionQueueProjection.g.razor.cs",
            TestContext.Current.CancellationToken);

        generatedSource.ShouldContain("\"WhenState\", \"Pending,Submitted\"");
        generatedSource.ShouldContain("x.Status.ToString() == \"Pending\"");
        generatedSource.ShouldContain("x.Status.ToString() == \"Submitted\"");
    }

    [Fact]
    public void ActionQueueProjection_RendersBadgeColumn_ThroughFcStatusBadge_WithAccessibleColumnHeader() {
        // AC2 badge a11y pin (FC-A11Y Layer-1 invariant for projection output): a [ProjectionBadge]-
        // mapped enum column renders through FcStatusBadge carrying the ColumnHeader that FcStatusBadge
        // turns into the mandatory aria-label (pinned by
        // FcStatusBadgeTests.AriaLabelCombinesColumnHeaderAndLabelInEnglish → aria-label="Status: ...").
        (string generatedSource, _) = RunGenerator(
            RoleSpecificTestSources.ActionQueueProjection,
            "RoleSpecific.Orders.ActionQueueProjection.g.razor.cs",
            TestContext.Current.CancellationToken);

        generatedSource.ShouldContain("Hexalith.FrontComposer.Shell.Components.Badges.FcStatusBadge");
        generatedSource.ShouldContain("\"ColumnHeader\", \"Status\"");
    }

    [Fact]
    public void TimelineProjection_EmitsChronologicalOrdering_OnTimestampProperty() {
        // AC2 role-layout pin — Timeline. The Timeline role must order the rendered items
        // chronologically by the timestamp property and lay them out as timeline rows (not a grid).
        // Previously only implicit inside TimelineProjection_Approval's snapshot; a careless
        // snapshot re-accept could silently drop the ordering.
        (string generatedSource, _) = RunGenerator(
            RoleSpecificTestSources.TimelineProjection,
            "RoleSpecific.Audit.TimelineProjection.g.razor.cs",
            TestContext.Current.CancellationToken);

        generatedSource.ShouldContain("state.Items.OrderByDescending(x => x.OccurredAt)");
        generatedSource.ShouldContain("\"fc-timeline-row\"");
    }

    [Fact]
    public void StatusOverviewProjection_EmitsAggregation_GroupedByStatusWithCount() {
        // AC2 role-layout pin — StatusOverview. The StatusOverview role must aggregate items by the
        // status enum and surface a count, ordered by descending count. Previously only implicit
        // inside StatusOverviewProjection_Approval's snapshot.
        (string generatedSource, _) = RunGenerator(
            RoleSpecificTestSources.StatusOverviewProjection,
            "RoleSpecific.Tickets.StatusOverviewProjection.g.razor.cs",
            TestContext.Current.CancellationToken);

        generatedSource.ShouldContain(".GroupBy(x => x.Status)");
        generatedSource.ShouldContain(".OrderByDescending(g => g.Count)");
    }

    [Fact]
    public void DetailRecordProjection_EmitsSingleRecordLayout_NotGrid() {
        // AC2 role-layout pin — DetailRecord. The DetailRecord role must render a single record
        // (first item) inside a card layout rather than a DataGrid. Previously only implicit inside
        // DetailRecordProjection_Approval's snapshot.
        (string generatedSource, _) = RunGenerator(
            RoleSpecificTestSources.DetailRecordProjection,
            "RoleSpecific.Customers.DetailRecordProjection.g.razor.cs",
            TestContext.Current.CancellationToken);

        generatedSource.ShouldContain("state.Items[0]");
        generatedSource.ShouldContain("OpenComponent<FluentCard>");
    }

    // ---------- Helpers ----------

    private static Task VerifySynthetic(string source, string generatedFileName) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        (string generatedSource, IReadOnlyList<Diagnostic> diagnostics) = RunGenerator(source, generatedFileName, ct);

        StringBuilder snapshot = new();
        _ = snapshot.AppendLine("=== Generated view ===");
        _ = snapshot.Append(generatedSource);
        if (!generatedSource.EndsWith('\n')) {
            _ = snapshot.AppendLine();
        }

        _ = snapshot.AppendLine();
        _ = snapshot.AppendLine("=== Diagnostics (HFC*** only) ===");
        IEnumerable<Diagnostic> fcDiagnostics = diagnostics
            .Where(d => d.Id.StartsWith("HFC", StringComparison.Ordinal))
            .OrderBy(d => d.Id, StringComparer.Ordinal)
            .ThenBy(d => d.GetMessage(), StringComparer.Ordinal);

        bool any = false;
        foreach (Diagnostic diagnostic in fcDiagnostics) {
            any = true;
            _ = snapshot.Append(diagnostic.Id);
            _ = snapshot.Append(' ');
            _ = snapshot.Append(diagnostic.Severity);
            _ = snapshot.Append(": ");
            _ = snapshot.AppendLine(diagnostic.GetMessage());
        }
        if (!any) {
            _ = snapshot.AppendLine("(none)");
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
        _ = razorTree.ShouldNotBeNull(
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
