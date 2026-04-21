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

    private sealed class CounterProjectionStub { }
}
