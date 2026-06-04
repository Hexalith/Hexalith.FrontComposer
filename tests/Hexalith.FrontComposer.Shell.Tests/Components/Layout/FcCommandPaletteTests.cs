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

        // Pass-5 P17 — previous Any-predicate had a closure-over-outer-collection bug
        // (`Count > invocationsBefore` evaluated identically for every item). Compare count
        // once, then search the invocation list independently.
        cut.WaitForAssertion(() =>
        {
            KeyboardModule.Invocations.Count.ShouldBeGreaterThan(invocationsBefore);
            KeyboardModule.Invocations.Any(i => i.Identifier == "focusElement").ShouldBeTrue();
        });
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
        // Pass-5 P1 — `aria-expanded` is now dynamic: reflects listbox-popup visibility per the
        // WAI-ARIA combobox pattern (true when Results populated, false when empty).
        EnsureStoreInitialized();
        IRenderedComponent<FcCommandPalette> cut = Render<FcCommandPalette>();

        cut.Markup.ShouldContain("aria-controls=\"fc-palette-results\"");
        cut.Markup.ShouldContain("aria-expanded=\"false\"");
        cut.Markup.ShouldContain("aria-autocomplete=\"list\"");
    }

    [Fact]
    public void SearchInput_AriaExpandedTrue_WhenResultsPopulated()
    {
        // Pass-5 P1 companion — verifies the true branch of the dynamic aria-expanded.
        EnsureStoreInitialized();
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

        cut.Markup.ShouldContain("aria-expanded=\"true\"");
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
        // Pass-5 P16 — record the focus-invocation baseline BEFORE the activation so the
        // assertion can prove the same-page Enter keypress specifically triggered the fallback
        // (not an unrelated earlier focusBodyIfNeeded invocation).
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
        int focusInvocationsBefore = FocusModule.Invocations.Count(i => i.Identifier == "focusBodyIfNeeded");

        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='fc-palette-root']").KeyDown(new KeyboardEventArgs { Key = "Enter" }));
        await cut.Instance.DisposeAsync();

        FocusModule.Invocations.Count(i => i.Identifier == "focusBodyIfNeeded").ShouldBeGreaterThan(focusInvocationsBefore);
    }

    [Fact]
    public async Task DifferentPageActivation_DoesNotPrimeBodyFocusFallback()
    {
        // Pass-5 P16 — complements SamePageActivation to prove the fallback is scoped to
        // same-page activations; a navigation to a different URL must NOT trigger focusBodyIfNeeded.
        EnsureStoreInitialized();
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/");

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
        int focusInvocationsBefore = FocusModule.Invocations.Count(i => i.Identifier == "focusBodyIfNeeded");

        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='fc-palette-root']").KeyDown(new KeyboardEventArgs { Key = "Enter" }));
        await cut.Instance.DisposeAsync();

        FocusModule.Invocations.Count(i => i.Identifier == "focusBodyIfNeeded").ShouldBe(focusInvocationsBefore);
    }

    [Fact]
    public void SearchInput_RendersAsAriaCombobox()
    {
        // Story 2.7 Task 1 (AC1) — default-lane pin for the combobox ROLE itself. Pre-existing pins
        // asserted aria-controls / aria-expanded / aria-autocomplete but never that the input carries
        // role="combobox" + aria-haspopup="listbox" (the WAI-ARIA combobox pattern entry point).
        EnsureStoreInitialized();
        IRenderedComponent<FcCommandPalette> cut = Render<FcCommandPalette>();

        cut.Markup.ShouldContain("role=\"combobox\"");
        cut.Markup.ShouldContain("aria-haspopup=\"listbox\"");
    }

    [Fact]
    public async Task ArrowKeys_MoveSelection_AndTrackAriaActiveDescendant()
    {
        // Story 2.7 Task 1 (AC1) — DEFAULT-LANE proof that ArrowDown/ArrowUp in the RENDERED palette
        // advance the selection AND the input's aria-activedescendant. Previously this end-to-end path
        // (rendered keydown → reducer → activedescendant) was only exercised in the EXCLUDED
        // e2e-palette lane; the reducer clamp logic alone was the only default-lane coverage.
        EnsureStoreInitialized();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new PaletteOpenedAction("open-1"));
        dispatcher.Dispatch(new PaletteResultsComputedAction(
            string.Empty,
            [
                new PaletteResult(
                    PaletteResultCategory.Projection, "Counter", "Counter", "/counter/counter-view", null, 120, false, typeof(CounterProjectionStub)),
                new PaletteResult(
                    PaletteResultCategory.Projection, "Orders", "Orders", "/orders/orders-view", null, 100, false, typeof(CounterProjectionStub)),
            ]));

        IRenderedComponent<FcCommandPalette> cut = Render<FcCommandPalette>();

        // Initial selection sits on flat index 0.
        cut.Markup.ShouldContain("aria-activedescendant=\"fc-palette-result-0\"");

        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='fc-palette-root']").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" }));
        cut.WaitForAssertion(() =>
            cut.Markup.ShouldContain("aria-activedescendant=\"fc-palette-result-1\""));

        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='fc-palette-root']").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" }));
        cut.WaitForAssertion(() =>
            cut.Markup.ShouldContain("aria-activedescendant=\"fc-palette-result-0\""));
    }

    [Fact]
    public async Task Escape_DispatchesPaletteClosed_ClosingThePalette()
    {
        // Story 2.7 Task 1 (AC1) — DEFAULT-LANE proof that Escape in the rendered palette dispatches
        // PaletteClosedAction (IsOpen → false). Previously only the excluded e2e-palette lane proved
        // the rendered Escape path.
        EnsureStoreInitialized();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        IState<FrontComposerCommandPaletteState> state =
            Services.GetRequiredService<IState<FrontComposerCommandPaletteState>>();
        dispatcher.Dispatch(new PaletteOpenedAction("open-1"));
        dispatcher.Dispatch(new PaletteResultsComputedAction(
            string.Empty,
            [new PaletteResult(
                PaletteResultCategory.Projection, "Counter", "Counter", "/counter/counter-view", null, 100, false, typeof(CounterProjectionStub))]));

        state.Value.IsOpen.ShouldBeTrue();

        IRenderedComponent<FcCommandPalette> cut = Render<FcCommandPalette>();
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='fc-palette-root']").KeyDown(new KeyboardEventArgs { Key = "Escape" }));

        cut.WaitForAssertion(() => state.Value.IsOpen.ShouldBeFalse());
    }

    [Fact]
    public async Task Enter_ActivatesSelectedProjection_NavigatingToItsRoute()
    {
        // Story 2.7 Task 1 (AC1) — DEFAULT-LANE proof that Enter in the RENDERED palette activates the
        // selected projection through the REAL effect (PaletteResultActivatedAction → NavigationManager
        // .NavigateTo(RouteUrl)). AC1 names Enter activation explicitly; the rendered keydown→dispatch→
        // navigate path was previously proven ONLY in the EXCLUDED e2e-palette lane (Arrow/Escape got
        // default-lane pins, Enter did not). Mirrors the Escape pin's rendered-keydown approach.
        EnsureStoreInitialized();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        dispatcher.Dispatch(new PaletteOpenedAction("open-1"));
        dispatcher.Dispatch(new PaletteResultsComputedAction(
            string.Empty,
            [new PaletteResult(
                PaletteResultCategory.Projection, "Counter", "Counter", "/counter/counter-view", null, 100, false, typeof(CounterProjectionStub))]));

        IRenderedComponent<FcCommandPalette> cut = Render<FcCommandPalette>();
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='fc-palette-root']").KeyDown(new KeyboardEventArgs { Key = "Enter" }));

        cut.WaitForAssertion(() => navigation.Uri.ShouldEndWith("/counter/counter-view"));
    }

    private sealed class CounterProjectionStub { }
}
