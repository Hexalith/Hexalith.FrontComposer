// ATDD RED PHASE — Story 3-3 Task 10.7 (D11; AC7)
// Fails at compile until Task 5.1 (FcSettingsButton component) lands.

using Bunit;

using Hexalith.FrontComposer.Shell.Components.Layout;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Story 3-3 Task 10.7 — <see cref="FcSettingsButton"/> renders a stealth Fluent button
/// with the Settings icon + aria-label; click invokes <see cref="IDialogService.ShowDialogAsync{T}"/>
/// with a 480 px modal (D11 + D13).
/// </summary>
public sealed class FcSettingsButtonTests : LayoutComponentTestBase
{
    [Fact]
    public void RendersFluentButtonWithSettingsIconAndAriaLabel()
    {
        IRenderedComponent<FcSettingsButton> cut = Render<FcSettingsButton>();

        cut.WaitForAssertion(() =>
        {
            // FluentButton with Stealth appearance — D11.
            cut.Markup.ShouldContain("appearance=\"stealth\"", Case.Insensitive);
            // The aria-label resolves through the Story 3-1 reused resource key.
            cut.Markup.ShouldContain("Open settings", Case.Sensitive);
        });
    }

    [Fact]
    public async Task ClickOpensSettingsDialog()
    {
        // Replace the Fluent UI registered IDialogService with an NSubstitute mock so we can
        // assert ShowDialogAsync was invoked.
        IDialogService dialogService = Substitute.For<IDialogService>();
        Services.Replace(ServiceDescriptor.Scoped(_ => dialogService));

        IRenderedComponent<FcSettingsButton> cut = Render<FcSettingsButton>();
        await cut.Find("button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        cut.WaitForAssertion(() =>
            dialogService.ReceivedCalls()
                .Any(c => c.GetMethodInfo().Name.StartsWith("ShowDialogAsync", StringComparison.Ordinal))
                .ShouldBeTrue("FcSettingsButton.OnClick must call IDialogService.ShowDialogAsync (D11)."));
    }
}
