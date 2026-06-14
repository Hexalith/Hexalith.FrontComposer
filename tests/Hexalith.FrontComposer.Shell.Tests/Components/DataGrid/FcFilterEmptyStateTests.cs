using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Shell.Components.DataGrid;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.DataGrid;

/// <summary>
/// Story 2-3 AC2 — dedicated regression pins for <see cref="FcFilterEmptyState"/>, the distinct
/// filter-induced-empty surface (as opposed to the no-data <c>FcProjectionEmptyPlaceholder</c>).
/// Closes the filter-empty gap (no dedicated test file existed at baseline 8036c3c).
/// </summary>
public sealed class FcFilterEmptyStateTests : BunitContext {
    private const string ViewKeyValue = "acme:OrdersProjection";

    private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();

    public FcFilterEmptyStateTests() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _ = Services.AddLogging();
        _ = Services.AddLocalization();
        _ = Services.AddFluentUIComponents();
        _ = Services.AddSingleton<IDispatcher>(_dispatcher);
    }

    [Fact]
    public void RendersPoliteLiveRegion() {
        using CultureScope _ = new("en");
        IRenderedComponent<FcFilterEmptyState> cut = RenderState(2);

        cut.Markup.ShouldContain("role=\"status\"");
        cut.Markup.ShouldContain("aria-live=\"polite\"");
        cut.Markup.ShouldContain("data-testid=\"fc-filter-empty-state\"");
    }

    [Fact]
    public void RendersLocalizedFilteredEmptyMessage() {
        using CultureScope _ = new("en");
        IRenderedComponent<FcFilterEmptyState> cut = RenderState(2);

        cut.Markup.ShouldContain("No orders match the current filters. Reset filters to see all 10 orders.");
    }

    [Fact]
    public void RendersResetAffordanceWhenFiltersActive() {
        using CultureScope _ = new("en");
        IRenderedComponent<FcFilterEmptyState> cut = RenderState(2);

        cut.Markup.ShouldContain("data-testid=\"fc-filter-reset\"");
    }

    [Fact]
    public void DegradesToTextOnlyWhenNoActiveFilters() {
        using CultureScope _ = new("en");
        IRenderedComponent<FcFilterEmptyState> cut = RenderState(0);

        cut.Markup.ShouldContain("data-testid=\"fc-filter-empty-state\"");
        cut.Markup.ShouldNotContain("data-testid=\"fc-filter-reset\"");
    }

    private IRenderedComponent<FcFilterEmptyState> RenderState(int activeFilterCount)
        => Render<FcFilterEmptyState>(parameters => parameters
            .Add(s => s.ViewKey, ViewKeyValue)
            .Add(s => s.TotalCount, 10)
            .Add(s => s.EntityPlural, "orders")
            .Add(s => s.ActiveFilterCount, activeFilterCount));
}
