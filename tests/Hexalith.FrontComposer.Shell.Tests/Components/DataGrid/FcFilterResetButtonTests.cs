#pragma warning disable CA2007
using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.DataGrid;
using Hexalith.FrontComposer.Shell.Tests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.DataGrid;

/// <summary>
/// Story 2-3 AC1 — dedicated regression pins for <see cref="FcFilterResetButton"/>. Closes the
/// reset-button gap (no dedicated test file existed at baseline 8036c3c).
/// </summary>
public sealed class FcFilterResetButtonTests : BunitContext
{
    private const string ViewKeyValue = "acme:OrdersProjection";

    private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();

    public FcFilterResetButtonTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _ = Services.AddLogging();
        _ = Services.AddLocalization();
        _ = Services.AddFluentUIComponents();
        _ = Services.AddSingleton<IDispatcher>(_dispatcher);
    }

    [Fact]
    public async Task Click_DispatchesFiltersResetActionForView()
    {
        using CultureScope _ = new("en");
        IRenderedComponent<FcFilterResetButton> cut = Render<FcFilterResetButton>(parameters => parameters
            .Add(b => b.ViewKey, ViewKeyValue)
            .Add(b => b.HasActiveFilters, true)
            .Add(b => b.ActiveFilterCount, 2));

        await cut.InvokeAsync(() => cut.Find("[data-testid=\"fc-filter-reset\"]").Click());

        _dispatcher.Received(1).Dispatch(Arg.Is<FiltersResetAction>(action => action.ViewKey == ViewKeyValue));
    }

    [Fact]
    public void DisabledWhenNoActiveFilters()
    {
        using CultureScope _ = new("en");
        IRenderedComponent<FcFilterResetButton> cut = Render<FcFilterResetButton>(parameters => parameters
            .Add(b => b.ViewKey, ViewKeyValue)
            .Add(b => b.HasActiveFilters, false)
            .Add(b => b.ActiveFilterCount, 0));

        cut.Find("[data-testid=\"fc-filter-reset\"]").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void EnabledWhenFiltersActive()
    {
        using CultureScope _ = new("en");
        IRenderedComponent<FcFilterResetButton> cut = Render<FcFilterResetButton>(parameters => parameters
            .Add(b => b.ViewKey, ViewKeyValue)
            .Add(b => b.HasActiveFilters, true)
            .Add(b => b.ActiveFilterCount, 1));

        cut.Find("[data-testid=\"fc-filter-reset\"]").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void AriaLabelReflectsActiveFilterCount()
    {
        using CultureScope _ = new("en");
        IRenderedComponent<FcFilterResetButton> cut = Render<FcFilterResetButton>(parameters => parameters
            .Add(b => b.ViewKey, ViewKeyValue)
            .Add(b => b.HasActiveFilters, true)
            .Add(b => b.ActiveFilterCount, 3));

        cut.Markup.ShouldContain("aria-label=\"Reset all filters. 3 filters currently active.\"");
    }
}
