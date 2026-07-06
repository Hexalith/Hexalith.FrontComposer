#pragma warning disable CA2007
using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.DataGrid;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.DataGrid;

/// <summary>
/// Story 2-3 AC1 (optional pin alongside the filter UI) — <see cref="FcStatusFilterChips"/>
/// toggle dispatches <see cref="StatusFilterToggledAction"/> and reflects active state via
/// <c>aria-pressed</c>.
/// </summary>
public sealed class FcStatusFilterChipsTests : BunitContext {
    private const string ViewKeyValue = "acme:OrdersProjection";

    private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();

    public FcStatusFilterChipsTests() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _ = Services.AddLogging();
        _ = Services.AddLocalization();
        _ = Services.AddFluentUIComponents();
        _ = Services.AddSingleton<IDispatcher>(_dispatcher);
    }

    [Fact]
    public async Task TogglingChip_DispatchesStatusFilterToggledActionWithSlotName() {
        using CultureScope _ = new("en");
        IReadOnlyList<BadgeSlot> available = [BadgeSlot.Success, BadgeSlot.Warning];
        IReadOnlyList<BadgeSlot> active = [];

        IRenderedComponent<FcStatusFilterChips> cut = Render<FcStatusFilterChips>(parameters => parameters
            .Add(c => c.ViewKey, ViewKeyValue)
            .Add(c => c.AvailableSlots, available)
            .Add(c => c.ActiveSlots, active));

        await cut.InvokeAsync(() => cut.Find("[data-fc-status-chip=\"Success\"]").Click());

        cut.Find("[data-fc-status-chip=\"Success\"]").GetAttribute("aria-pressed").ShouldBe("true");
        cut.Find("[data-fc-status-chip=\"Success\"]").GetAttribute("data-fc-chip-active").ShouldBe("true");
        _dispatcher.Received(1).Dispatch(ArgEx.Is<StatusFilterToggledAction>(action =>
            action.ViewKey == ViewKeyValue && action.SlotName == "Success"));
    }

    [Fact]
    public void RendersChipGroup_WithActivePressedState() {
        using CultureScope _ = new("en");
        IReadOnlyList<BadgeSlot> available = [BadgeSlot.Success, BadgeSlot.Warning];
        IReadOnlyList<BadgeSlot> active = [BadgeSlot.Success];

        IRenderedComponent<FcStatusFilterChips> cut = Render<FcStatusFilterChips>(parameters => parameters
            .Add(c => c.ViewKey, ViewKeyValue)
            .Add(c => c.AvailableSlots, available)
            .Add(c => c.ActiveSlots, active));

        cut.Markup.ShouldContain("role=\"group\"");
        cut.Markup.ShouldContain("data-testid=\"fc-status-filter-chips\"");
        cut.Find("[data-fc-status-chip=\"Success\"]").GetAttribute("aria-pressed").ShouldBe("true");
        cut.Find("[data-fc-status-chip=\"Warning\"]").GetAttribute("aria-pressed").ShouldBe("false");
    }
}
