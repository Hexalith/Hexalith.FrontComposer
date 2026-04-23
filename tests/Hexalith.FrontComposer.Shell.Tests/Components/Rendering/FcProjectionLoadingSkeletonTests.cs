using System.Globalization;

using Bunit;

using Hexalith.FrontComposer.Shell.Components.Rendering;
using Hexalith.FrontComposer.Shell.Tests.Components.Layout;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

/// <summary>
/// Story 4-1 T4.5 / D6 — <see cref="FcProjectionLoadingSkeleton"/> tests covering
/// ColumnCount cap, aria-busy + role=status, and the role-aware Layout variants.
/// </summary>
public sealed class FcProjectionLoadingSkeletonTests : LayoutComponentTestBase {
    public FcProjectionLoadingSkeletonTests() {
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");
        EnsureStoreInitialized();
    }

    [Fact]
    public void RendersDataGridLayoutByDefault_WithAriaBusyAndRoleStatus() {
        IRenderedComponent<FcProjectionLoadingSkeleton> cut = Render<FcProjectionLoadingSkeleton>(parameters => parameters
            .Add(p => p.ColumnCount, 4));

        cut.Markup.ShouldContain("role=\"status\"");
        cut.Markup.ShouldContain("aria-busy=\"true\"");
        cut.Markup.ShouldContain("fc-projection-skeleton-layout-datagrid");
    }

    [Fact]
    public void RendersCardLayoutWhenLayoutIsCard() {
        IRenderedComponent<FcProjectionLoadingSkeleton> cut = Render<FcProjectionLoadingSkeleton>(parameters => parameters
            .Add(p => p.ColumnCount, 0)
            .Add(p => p.Layout, SkeletonLayout.Card));

        cut.Markup.ShouldContain("fc-projection-skeleton-layout-card");
    }

    [Fact]
    public void RendersTimelineLayoutWhenLayoutIsTimeline() {
        IRenderedComponent<FcProjectionLoadingSkeleton> cut = Render<FcProjectionLoadingSkeleton>(parameters => parameters
            .Add(p => p.ColumnCount, 0)
            .Add(p => p.Layout, SkeletonLayout.Timeline));

        cut.Markup.ShouldContain("fc-projection-skeleton-layout-timeline");
    }

    [Fact]
    public void RespectsRowCount() {
        IRenderedComponent<FcProjectionLoadingSkeleton> cut = Render<FcProjectionLoadingSkeleton>(parameters => parameters
            .Add(p => p.ColumnCount, 3)
            .Add(p => p.RowCount, 2));

        // Header row + 2 body rows = 3 rows with fc-projection-skeleton-row class.
        int rowOccurrences = CountOccurrences(cut.Markup, "fc-projection-skeleton-row");
        rowOccurrences.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void UsesEntityLabelInResolvedAriaLabel() {
        IRenderedComponent<FcProjectionLoadingSkeleton> cut = Render<FcProjectionLoadingSkeleton>(parameters => parameters
            .Add(p => p.ColumnCount, 2)
            .Add(p => p.EntityLabel, "orders"));

        cut.Markup.ShouldContain("aria-label=\"Loading orders\"");
    }

    private static int CountOccurrences(string source, string needle) {
        int count = 0;
        int pos = 0;
        while ((pos = source.IndexOf(needle, pos, StringComparison.OrdinalIgnoreCase)) != -1) {
            count++;
            pos += needle.Length;
        }

        return count;
    }
}
