// ATDD RED PHASE — Story 3-3 Task 10.7 (D11; AC7)
// Fails at compile until Task 5.1 (FcSettingsButton component) lands.

using Bunit;

using Hexalith.FrontComposer.Shell.Components.Layout;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Story 3-3 Task 10.7 — <see cref="FcSettingsButton"/> renders a stealth Fluent button
/// with the Settings icon + aria-label; click invokes <see cref="IDialogService.ShowDialogAsync{T}"/>
/// with a 480 px modal (D11 + D13).
/// </summary>
public sealed class FcSettingsButtonTests : LayoutComponentTestBase {
    [Fact]
    public void RendersFluentButtonWithSettingsIconAndAriaLabel() {
        System.Globalization.CultureInfo previous = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("en");
        try {
            IRenderedComponent<FcSettingsButton> cut = Render<FcSettingsButton>();

            cut.WaitForAssertion(() => {
                // FluentButton stealth appearance — D11. Fluent UI v5 rc2 maps the "stealth"
                // concept to the Subtle appearance slot (no dedicated Stealth enum value).
                cut.Markup.ShouldContain("appearance=\"subtle\"", Case.Insensitive);
                // The aria-label resolves through the Story 3-1 reused resource key.
                cut.Markup.ShouldContain("Open settings", Case.Sensitive);
            });
        }
        finally {
            System.Globalization.CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public async Task ClickOpensSettingsDialog() {
        RecordingDialogService dialogService = new();
        Services.Replace(ServiceDescriptor.Scoped<IDialogService>(_ => dialogService));

        IRenderedComponent<FcSettingsButton> cut = Render<FcSettingsButton>();

        await cut.Find("[data-testid=\"fc-settings-button\"]")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        cut.WaitForAssertion(() => dialogService.ShowDialogCallCount.ShouldBe(1));
        dialogService.LastDialogType.ShouldBe(typeof(FcSettingsDialog));
        dialogService.LastOptions.ShouldNotBeNull();
        dialogService.LastOptions!.Modal.ShouldBe(true);
        dialogService.LastOptions.Width.ShouldBe("480px");
        string.IsNullOrWhiteSpace(dialogService.LastOptions.Header.Title).ShouldBeFalse();
    }
}
