// Story 3-3 Task 10.6b (D20) — FcDensityAnnouncer bUnit tests: visually-hidden aria-live region
// renders with role="status" + aria-live="polite" + .fc-sr-only class; text updates on
// post-hydration EffectiveDensity changes. Load-time hydration and the first measured viewport
// tier stay silent (Story 13.6 O1).

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.State.Density;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

public sealed class FcDensityAnnouncerTests : LayoutComponentTestBase {
    public FcDensityAnnouncerTests() => EnsureStoreInitialized();

    [Fact]
    public void RendersVisuallyHiddenAriaLiveRegion() {
        IRenderedComponent<FcDensityAnnouncer> cut = Render<FcDensityAnnouncer>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("role=\"status\"", Case.Insensitive);
            cut.Markup.ShouldContain("aria-live=\"polite\"", Case.Insensitive);
            cut.Markup.ShouldContain("aria-atomic=\"true\"", Case.Insensitive);
            cut.Markup.ShouldContain("fc-sr-only", Case.Insensitive);
        });
    }

    [Fact]
    public void AnnouncesPostHydrationUserSelection() {
        WithEnglishUiCulture(() => {
            IRenderedComponent<FcDensityAnnouncer> cut = Render<FcDensityAnnouncer>();
            IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
            CompleteHydration(dispatcher, DensityLevel.Comfortable);

            dispatcher.Dispatch(new UserPreferenceChangedAction("c1", DensityLevel.Roomy, DensityLevel.Roomy));

            cut.WaitForAssertion(() =>
                cut.Markup.ShouldContain("Density set to Roomy.", Case.Sensitive));
        });
    }

    [Fact]
    public void LoadTimeHydrationChangeIsSilent() {
        WithEnglishUiCulture(() => {
            IRenderedComponent<FcDensityAnnouncer> cut = Render<FcDensityAnnouncer>();
            IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

            // The feature default is Comfortable; hydration resolving Compact is not an operator change.
            CompleteHydration(dispatcher, DensityLevel.Compact);

            Services.GetRequiredService<IState<FrontComposerDensityState>>().Value.EffectiveDensity.ShouldBe(DensityLevel.Compact);
            cut.Render();
            cut.Markup.ShouldNotContain("Density set to", Case.Sensitive);
        });
    }

    [Fact]
    public void FirstMeasuredViewportTierRecomputeIsSilent() {
        WithEnglishUiCulture(() => {
            IRenderedComponent<FcDensityAnnouncer> cut = Render<FcDensityAnnouncer>();
            IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
            CompleteHydration(dispatcher, DensityLevel.Comfortable);

            // The first measured tier replaces the bootstrap Tablet cap; its recompute stays silent.
            dispatcher.Dispatch(new ViewportTierChangedAction(ViewportTier.CompactDesktop));

            IState<FrontComposerDensityState> state = Services.GetRequiredService<IState<FrontComposerDensityState>>();
            cut.WaitForAssertion(() => state.Value.EffectiveDensity.ShouldBe(DensityLevel.Compact));
            cut.Render();
            cut.Markup.ShouldNotContain("Density set to", Case.Sensitive);
        });
    }

    [Fact]
    public void LaterViewportForcedChangeIsAnnounced() {
        WithEnglishUiCulture(() => {
            IRenderedComponent<FcDensityAnnouncer> cut = Render<FcDensityAnnouncer>();
            IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
            CompleteHydration(dispatcher, DensityLevel.Comfortable);
            dispatcher.Dispatch(new ViewportTierChangedAction(ViewportTier.CompactDesktop));
            IState<FrontComposerDensityState> state = Services.GetRequiredService<IState<FrontComposerDensityState>>();
            cut.WaitForAssertion(() => state.Value.EffectiveDensity.ShouldBe(DensityLevel.Compact));

            // A later tier change forces Comfortable for touch targets and is announced.
            dispatcher.Dispatch(new ViewportTierChangedAction(ViewportTier.Tablet));

            cut.WaitForAssertion(() => {
                cut.Find("[role='status']").TextContent.Trim().ShouldBe("Density set to Comfortable.");
            });
        });
    }

    [Fact]
    public void HydrationWithStoredPreferenceIsSilent() {
        WithEnglishUiCulture(() => {
            IRenderedComponent<FcDensityAnnouncer> cut = Render<FcDensityAnnouncer>();
            IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

            // Two measured tiers first, so the viewport-report guard no longer silences anything;
            // a stored preference arriving through hydration must still stay silent.
            dispatcher.Dispatch(new ViewportTierChangedAction(ViewportTier.CompactDesktop));
            dispatcher.Dispatch(new ViewportTierChangedAction(ViewportTier.Desktop));
            dispatcher.Dispatch(new DensityHydratingAction());
            dispatcher.Dispatch(new DensityHydratedAction(DensityLevel.Roomy, DensityLevel.Roomy));
            dispatcher.Dispatch(new DensityHydratedCompletedAction());

            Services.GetRequiredService<IState<FrontComposerDensityState>>().Value.EffectiveDensity.ShouldBe(DensityLevel.Roomy);
            cut.Render();
            cut.Markup.ShouldNotContain("Density set to", Case.Sensitive);
        });
    }

    private static void CompleteHydration(IDispatcher dispatcher, DensityLevel effective) {
        dispatcher.Dispatch(new DensityHydratingAction());
        dispatcher.Dispatch(new DensityHydratedAction(null, effective));
        dispatcher.Dispatch(new DensityHydratedCompletedAction());
    }

    private static void WithEnglishUiCulture(Action assertion) {
        System.Globalization.CultureInfo previous = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("en");
        try {
            assertion();
        }
        finally {
            System.Globalization.CultureInfo.CurrentUICulture = previous;
        }
    }
}
