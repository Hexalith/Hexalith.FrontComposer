#pragma warning disable CA2007
using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.DataGrid;
using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
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

    [Theory]
    [InlineData("en", "Success", "Success, currently active", "Warning", "Warning, currently inactive")]
    [InlineData("fr-CA", "Réussite", "Réussite, état actuel\u00A0: actif", "Avertissement", "Avertissement, état actuel\u00A0: inactif")]
    public void ChipTextAndAccessibleNamesUseTheActiveCulture(
        string locale,
        string success,
        string successAriaLabel,
        string warning,
        string warningAriaLabel) {
        using CultureScope culture = new(locale);
        IRenderedComponent<FcStatusFilterChips> cut = Render<FcStatusFilterChips>(parameters => parameters
            .Add(c => c.ViewKey, ViewKeyValue)
            .Add(c => c.AvailableSlots, [BadgeSlot.Success, BadgeSlot.Warning])
            .Add(c => c.ActiveSlots, [BadgeSlot.Success]));

        var successChip = cut.Find("[data-fc-status-chip='Success']");
        successChip.TextContent.Trim().ShouldBe(success);
        successChip.GetAttribute("aria-label").ShouldBe(successAriaLabel);
        successChip.GetAttribute("aria-pressed").ShouldBe("true");

        var warningChip = cut.Find("[data-fc-status-chip='Warning']");
        warningChip.TextContent.Trim().ShouldBe(warning);
        warningChip.GetAttribute("aria-label").ShouldBe(warningAriaLabel);
    }

    [Fact]
    public void MissingSlotResourceFallsBackToTheSlotName() {
        IStringLocalizer<FcShellResources> localizer = Substitute.For<IStringLocalizer<FcShellResources>>();
        localizer[Arg.Any<string>()].Returns(call => new LocalizedString(
            call.Arg<string>(), call.Arg<string>(), resourceNotFound: true));
        localizer["StatusFilterChipInactiveLabel"].Returns(new LocalizedString(
            "StatusFilterChipInactiveLabel", "inactive"));
        localizer["StatusFilterChipAriaLabelTemplate", Arg.Any<object[]>()].Returns(call => {
            object[] arguments = (object[])call[1];
            return new LocalizedString("StatusFilterChipAriaLabelTemplate", $"{arguments[0]}, currently {arguments[1]}");
        });
        Services.AddSingleton(localizer);

        IRenderedComponent<FcStatusFilterChips> cut = Render<FcStatusFilterChips>(parameters => parameters
            .Add(c => c.ViewKey, ViewKeyValue)
            .Add(c => c.AvailableSlots, [BadgeSlot.Success]));

        var chip = cut.Find("[data-fc-status-chip='Success']");
        chip.TextContent.Trim().ShouldBe("Success");
        chip.GetAttribute("aria-label").ShouldBe("Success, currently inactive");
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
