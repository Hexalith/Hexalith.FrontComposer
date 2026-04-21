// Story 3-4 Task 10.7 / 10.7b (D11 / D15 / D17 — AC2 / AC5).
#pragma warning disable CA2007
using System.Collections.Immutable;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.State.CommandPalette;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

public sealed class FcCommandPaletteTests : LayoutComponentTestBase
{
    public FcCommandPaletteTests()
    {
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("en");
    }

    [Fact]
    public void RendersDialogBodyWithSearchAndResultRoot()
    {
        EnsureStoreInitialized();
        int invocationsBefore = KeyboardModule.Invocations.Count;
        IRenderedComponent<FcCommandPalette> cut = Render<FcCommandPalette>();

        cut.Markup.ShouldContain("data-testid=\"fc-palette-root\"");
        cut.Markup.ShouldContain("data-testid=\"fc-palette-search\"");
        cut.Markup.ShouldContain("Command palette");
        cut.Markup.ShouldContain("role=\"dialog\"");
        cut.Markup.ShouldContain("aria-label=\"Command palette\"");

        cut.WaitForAssertion(() =>
            KeyboardModule.Invocations.Any(i => i.Identifier == "focusElement" && KeyboardModule.Invocations.Count > invocationsBefore).ShouldBeTrue());
    }

    [Fact]
    public void AriaLiveRegion_RendersStatusRoleAndPoliteAtomic()
    {
        EnsureStoreInitialized();
        IRenderedComponent<FcCommandPalette> cut = Render<FcCommandPalette>();

        // The aria-live region exists and is rendered with role="status" + aria-live="polite".
        cut.Markup.ShouldContain("role=\"status\"");
        cut.Markup.ShouldContain("aria-live=\"polite\"");
        cut.Markup.ShouldContain("aria-atomic=\"true\"");
    }

    [Fact]
    public void SearchInput_WiresResultsAccessibilityAttributes()
    {
        EnsureStoreInitialized();
        IRenderedComponent<FcCommandPalette> cut = Render<FcCommandPalette>();

        cut.Markup.ShouldContain("aria-controls=\"fc-palette-results\"");
        cut.Markup.ShouldContain("aria-expanded=\"true\"");
        cut.Markup.ShouldContain("aria-autocomplete=\"list\"");
    }

    [Fact]
    public void LiveRegion_UpdatesWhenResultsChange()
    {
        EnsureStoreInitialized();
        IRenderedComponent<FcCommandPalette> cut = Render<FcCommandPalette>();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

        dispatcher.Dispatch(new PaletteOpenedAction("open-1"));
        dispatcher.Dispatch(new PaletteResultsComputedAction(
            string.Empty,
            [new PaletteResult(
                PaletteResultCategory.Projection,
                "Counter",
                "Counter",
                "/counter/counter-view",
                null,
                100,
                false,
                typeof(CounterProjectionStub))]));

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='fc-palette-live']").TextContent.ShouldBe("1 results"));
    }

    [Fact]
    public async Task SamePageActivation_PrimesBodyFocusFallbackOnDispose()
    {
        EnsureStoreInitialized();
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/counter/counter-view");

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new PaletteOpenedAction("open-1"));
        dispatcher.Dispatch(new PaletteResultsComputedAction(
            string.Empty,
            [new PaletteResult(
                PaletteResultCategory.Projection,
                "Counter",
                "Counter",
                "/counter/counter-view",
                null,
                100,
                false,
                typeof(CounterProjectionStub))]));

        IRenderedComponent<FcCommandPalette> cut = Render<FcCommandPalette>();

        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='fc-palette-root']").KeyDown(new KeyboardEventArgs { Key = "Enter" }));
        await cut.Instance.DisposeAsync();

        FocusModule.Invocations.Any(i => i.Identifier == "focusBodyIfNeeded").ShouldBeTrue();
    }

    private sealed class CounterProjectionStub { }
}
