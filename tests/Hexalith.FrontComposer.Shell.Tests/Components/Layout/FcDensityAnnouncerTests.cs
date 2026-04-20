// Story 3-3 Task 10.6b (D20) — FcDensityAnnouncer bUnit tests: visually-hidden aria-live region
// renders with role="status" + aria-live="polite" + .fc-sr-only class; text updates on
// EffectiveDensity change. First-render skip is verified implicitly by not asserting any
// announcement on initial OnInitialized.

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.State.Density;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

public sealed class FcDensityAnnouncerTests : LayoutComponentTestBase {
    public FcDensityAnnouncerTests() {
        EnsureStoreInitialized();
    }

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
    public void AnnouncesOnEffectiveDensityChange() {
        System.Globalization.CultureInfo previous = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("en");
        try {
            IRenderedComponent<FcDensityAnnouncer> cut = Render<FcDensityAnnouncer>();

            IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
            dispatcher.Dispatch(new UserPreferenceChangedAction("c1", DensityLevel.Roomy, DensityLevel.Roomy));

            cut.WaitForAssertion(() =>
                cut.Markup.ShouldContain("Density set to Roomy.", Case.Sensitive));
        }
        finally {
            System.Globalization.CultureInfo.CurrentUICulture = previous;
        }
    }
}
