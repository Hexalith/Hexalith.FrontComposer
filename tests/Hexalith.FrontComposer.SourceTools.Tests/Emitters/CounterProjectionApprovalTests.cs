using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

/// <summary>
/// Story 4-1 T5.5 / F1 (review round 2) — mechanical diff-token gate that
/// catches author-authored callouts that lie about the intended scope of a
/// re-baseline. Every line that differs between the regenerated output and the
/// canonical <c>.verified.txt</c> approval MUST contain at least one of the
/// approved insertion tokens; an unexpected line addition fails the test with
/// a message naming the offending content + the Story-4-1 discipline.
/// </summary>
/// <remarks>
/// <para>The "approved insertion tokens" mirror the Story 4-1 Phase 2 inserts:
/// subtitle invocation, loading skeleton, empty placeholder, the cascading
/// rendering namespace, the Contracts attributes namespace, and the per-row
/// cascade tokens introduced by Phase 3 G12 (ProjectionContext / CascadingValue /
/// fc-row-context-actions / TemplateColumn).</para>
/// <para>The fixture mirrors the Counter sample's projection shape: text Name,
/// numeric Count, boolean IsActive, DateTime CreatedAt — the same model used by
/// <see cref="RazorEmitterTests.BasicProjection_Snapshot"/>.</para>
/// </remarks>
public sealed class CounterProjectionApprovalTests {
    private static readonly EquatableArray<BadgeMappingEntry> _emptyBadges =
        new(ImmutableArray<BadgeMappingEntry>.Empty);

    private static readonly string[] _approvedInsertionTokens = {
        // Phase 2 — three-state shell + subtitle host
        "FcProjectionSubtitle",
        "FcProjectionLoadingSkeleton",
        "FcProjectionEmptyPlaceholder",
        "SkeletonLayout",
        "FallbackCount",
        "DistinctStatusCount",
        "EntityLabel",
        "EntityPluralLabel",
        "IsLoading",
        "IStringLocalizer",
        "FcShellLocalizer",
        "ResolveEmptyStateSecondaryText",
        "EmptyStateSecondaryText",
        "ResourceNotFound",
        "CtaCommandName",
        "SecondaryText",
        // Cascading namespaces emitted by the shells
        "Hexalith.FrontComposer.Shell.Components.Rendering",
        "Hexalith.FrontComposer.Contracts.Attributes",
        "ProjectionRole",
        "WhenState",
        "ProjectionType",
        // Phase 3 G12 — per-row ProjectionContext cascade
        "ProjectionContext",
        "CascadingValue",
        "TemplateColumn",
        "fc-row-context-actions",
        "_rowContext",
        "rb.OpenComponent",
        "rb.AddAttribute",
        "rb.CloseComponent",
        "ib.",
        "Hexalith.FrontComposer.Contracts.Rendering",
        "ImmutableDictionary",
        "KeyValuePair",
        "global::System.Collections",
        "global::Microsoft.AspNetCore.Components",
        "global::Hexalith.FrontComposer",
        "Story 4-1",
        "G12",
        "Story 2-2",
        "AC1b",
        "destructive-command",
        "renderer integration",
        "inline-command",
        "fragment.",
        "buttons here per AC1b.",
        "the cascade contract",
    };

    [Fact]
    public void DiffStaysWithinExpectedInsertionSet() {
        // Re-generate the canonical Counter-shape projection emit output.
        RazorModel model = new(
            typeName: "OrderProjection",
            @namespace: "TestDomain",
            boundedContext: "Orders",
            columns: new EquatableArray<ColumnModel>(ImmutableArray.Create(
                new ColumnModel("Name", "Name", TypeCategory.Text, null, false, _emptyBadges),
                new ColumnModel("Count", "Count", TypeCategory.Numeric, "N0", false, _emptyBadges),
                new ColumnModel("IsActive", "Is Active", TypeCategory.Boolean, "Yes/No", false, _emptyBadges),
                new ColumnModel("CreatedAt", "Created At", TypeCategory.DateTime, "d", false, _emptyBadges))));

        string regenerated = RazorEmitter.Emit(model);

        string approvedPath = ResolveApprovedPath();
        File.Exists(approvedPath).ShouldBeTrue($"Expected approval file at {approvedPath}");

        string approved = File.ReadAllText(approvedPath);

        List<(int LineNumber, string Content)> approvedLines = NormalizeLines(approved);
        IEnumerable<(int LineNumber, string Content)> diffLines = EnumerateDiffLines(regenerated, approvedLines);

        List<string> violations = new();
        foreach ((int lineNumber, string content) in diffLines) {
            if (!ContainsAnyApprovedToken(content)) {
                violations.Add(
                    $"Unexpected emitter drift on regenerated line {lineNumber}: '{content.Trim()}' — "
                    + "see Story 4-1 T5.5 discipline. Author must (a) justify the drift in the PR description, "
                    + "(b) bump the approved insertion-token set in CounterProjectionApprovalTests if the drift is intentional.");
            }
        }

        violations.ShouldBeEmpty();
    }

    private static string ResolveApprovedPath() {
        // Look up the .verified.txt sibling of the BasicProjection snapshot.
        string testDirectory = Path.GetDirectoryName(typeof(CounterProjectionApprovalTests).Assembly.Location)!;
        // Walk up from bin/Debug/netX.0 to repo root + tests/.../Emitters/
        DirectoryInfo? cursor = new(testDirectory);
        while (cursor is not null && !File.Exists(Path.Combine(cursor.FullName, "Hexalith.FrontComposer.sln"))) {
            cursor = cursor.Parent;
        }

        string repoRoot = cursor?.FullName
            ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        return Path.Combine(
            repoRoot,
            "tests",
            "Hexalith.FrontComposer.SourceTools.Tests",
            "Emitters",
            "RazorEmitterTests.BasicProjection_Snapshot.verified.txt");
    }

    private static List<(int LineNumber, string Content)> NormalizeLines(string content) {
        List<(int LineNumber, string Content)> lines = [];
        string[] rawLines = content.Split('\n');
        for (int i = 0; i < rawLines.Length; i++) {
            string trimmed = rawLines[i].TrimEnd('\r').TrimEnd();
            if (!string.IsNullOrWhiteSpace(trimmed)) {
                lines.Add((i + 1, trimmed));
            }
        }

        return lines;
    }

    private static IEnumerable<(int LineNumber, string Content)> EnumerateDiffLines(
        string regenerated,
        IReadOnlyList<(int LineNumber, string Content)> approvedLines) {
        List<(int LineNumber, string Content)> regeneratedLines = NormalizeLines(regenerated);
        int approvedIndex = 0;

        foreach ((int lineNumber, string content) in regeneratedLines) {
            if (approvedIndex < approvedLines.Count
                && content == approvedLines[approvedIndex].Content) {
                approvedIndex++;
                continue;
            }

            yield return (lineNumber, content);
        }
    }

    private static bool ContainsAnyApprovedToken(string content) {
        if (content is "{" or "}") {
            return true;
        }

        foreach (string token in _approvedInsertionTokens) {
            if (content.Contains(token, StringComparison.Ordinal)) {
                return true;
            }
        }

        return false;
    }
}
