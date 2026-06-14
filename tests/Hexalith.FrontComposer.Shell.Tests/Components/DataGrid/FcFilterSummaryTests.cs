using Bunit;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.DataGrid;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.DataGrid;

/// <summary>
/// Story 2-3 AC1 — dedicated regression pins for <see cref="FcFilterSummary"/>. Closes the
/// filter-summary gap (no dedicated test file existed at baseline 8036c3c). Culture pinned to
/// <c>en</c> for resource-key stability.
/// </summary>
public sealed class FcFilterSummaryTests : BunitContext {
    public FcFilterSummaryTests() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _ = Services.AddLogging();
        _ = Services.AddLocalization();
    }

    [Fact]
    public void RendersStatusRegionAndTestId_WithShowingPrefix_WhenAnyFilterActive() {
        using CultureScope _ = new("en");
        Dictionary<string, string> filters = new() { ["Name"] = "Acme" };

        IRenderedComponent<FcFilterSummary> cut = Render<FcFilterSummary>(parameters => parameters
            .Add(s => s.ViewKey, "acme:Orders")
            .Add(s => s.Filters, filters)
            .Add(s => s.FilteredCount, 3)
            .Add(s => s.TotalCount, 10)
            .Add(s => s.EntityPlural, "orders"));

        cut.Markup.ShouldContain("role=\"status\"");
        cut.Markup.ShouldContain("data-testid=\"fc-filter-summary\"");
        cut.Markup.ShouldContain("Showing 3 of 10 orders");
    }

    [Fact]
    public void EmitsOneClausePerActiveStatusColumnSearchAndSort() {
        using CultureScope _ = new("en");
        Dictionary<string, string> filters = new() {
            ["Name"] = "Acme",
            [ReservedFilterKeys.StatusKey] = "Success",
            [ReservedFilterKeys.SearchKey] = "abc",
        };
        Dictionary<string, string> headers = new() { ["Name"] = "Name" };

        IRenderedComponent<FcFilterSummary> cut = Render<FcFilterSummary>(parameters => parameters
            .Add(s => s.ViewKey, "acme:Orders")
            .Add(s => s.Filters, filters)
            .Add(s => s.HumanisedColumnHeaders, headers)
            .Add(s => s.SortColumn, "Name")
            .Add(s => s.SortDescending, false)
            .Add(s => s.FilteredCount, 1)
            .Add(s => s.TotalCount, 10)
            .Add(s => s.EntityPlural, "orders"));

        cut.Markup.ShouldContain("Status: Success");
        cut.Markup.ShouldContain("Name contains");
        cut.Markup.ShouldContain("Acme");
        cut.Markup.ShouldContain("Search:");
        cut.Markup.ShouldContain("Sorted by Name ascending");
    }

    [Fact]
    public void ExcludesReservedPrefixedKeysFromColumnClauses() {
        using CultureScope _ = new("en");
        Dictionary<string, string> filters = new() {
            [ReservedFilterKeys.StatusKey] = "Success",
        };

        IRenderedComponent<FcFilterSummary> cut = Render<FcFilterSummary>(parameters => parameters
            .Add(s => s.ViewKey, "acme:Orders")
            .Add(s => s.Filters, filters)
            .Add(s => s.FilteredCount, 2)
            .Add(s => s.TotalCount, 10)
            .Add(s => s.EntityPlural, "orders"));

        // The reserved __status key feeds the Status clause, never a "__status contains" column clause.
        cut.Markup.ShouldNotContain("__status");
        cut.Markup.ShouldContain("Status: Success");
    }

    [Fact]
    public void VisibleForSortOnly_WithDescendingDirection_WhenNoFiltersActive() {
        // A non-default sort with no column/status/search filters still surfaces the summary, and the
        // descending direction renders its own clause text (complements the ascending+filters case).
        using CultureScope _ = new("en");
        Dictionary<string, string> filters = [];

        IRenderedComponent<FcFilterSummary> cut = Render<FcFilterSummary>(parameters => parameters
            .Add(s => s.ViewKey, "acme:Orders")
            .Add(s => s.Filters, filters)
            .Add(s => s.SortColumn, "Name")
            .Add(s => s.SortDescending, true)
            .Add(s => s.FilteredCount, 10)
            .Add(s => s.TotalCount, 10)
            .Add(s => s.EntityPlural, "orders"));

        cut.Markup.ShouldContain("data-testid=\"fc-filter-summary\"");
        cut.Markup.ShouldContain("Sorted by Name descending");
    }

    [Fact]
    public void HiddenWhenNoFilterOrSortActive() {
        using CultureScope _ = new("en");
        Dictionary<string, string> filters = [];

        IRenderedComponent<FcFilterSummary> cut = Render<FcFilterSummary>(parameters => parameters
            .Add(s => s.ViewKey, "acme:Orders")
            .Add(s => s.Filters, filters)
            .Add(s => s.FilteredCount, 10)
            .Add(s => s.TotalCount, 10)
            .Add(s => s.EntityPlural, "orders"));

        cut.Markup.ShouldNotContain("fc-filter-summary");
    }
}
