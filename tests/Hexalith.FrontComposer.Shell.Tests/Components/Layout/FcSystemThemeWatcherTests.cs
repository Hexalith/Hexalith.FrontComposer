using Bunit;

using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Story 3-1 system theme watcher tests.
/// </summary>
public sealed class FcSystemThemeWatcherTests : LayoutComponentTestBase
{
    [Fact]
    public async Task Applies_dark_mode_when_fluxor_state_is_system()
    {
        DispatchTheme(ThemeValue.System);
        IRenderedComponent<FcSystemThemeWatcher> cut = Render<FcSystemThemeWatcher>();
        ThemeService.ClearReceivedCalls();

        await cut.Instance.OnSystemThemeChangedAsync(true);

        await ThemeService.Received(1)
            .SetThemeAsync(Arg.Is<ThemeSettings>(settings => settings.Mode == ThemeMode.Dark && settings.Color == "#0097A7"));
    }

    [Fact]
    public async Task Ignores_os_changes_when_fluxor_state_is_explicit()
    {
        DispatchTheme(ThemeValue.Dark);
        IRenderedComponent<FcSystemThemeWatcher> cut = Render<FcSystemThemeWatcher>();
        ThemeService.ClearReceivedCalls();

        await cut.Instance.OnSystemThemeChangedAsync(false);

        ThemeService.ReceivedCalls().ShouldBeEmpty();
    }
}
