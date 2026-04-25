#pragma warning disable CA2007
using System.Collections.Immutable;
using System.Globalization;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Components.DataGrid;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.DataGrid;

/// <summary>
/// Story 4-4 T4.2 / D8 / D11 — <see cref="FcSlowQueryNotice"/> renders only when the view's elapsed
/// timing exceeds <see cref="FcShellOptions.SlowQueryThresholdMs"/>, auto-dismisses after 5 s on the
/// injected <see cref="TimeProvider"/>, and keeps dismiss state view-scoped.
/// </summary>
public sealed class FcSlowQueryNoticeTests : BunitContext {
    private readonly FakeTimeProvider _time = new();
    private readonly IState<LoadedPageState> _state = Substitute.For<IState<LoadedPageState>>();
    private LoadedPageState _stateValue = new();

    public FcSlowQueryNoticeTests() {
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");
        JSInterop.Mode = JSRuntimeMode.Loose;

        _state.Value.Returns(_ => _stateValue);
        Services.AddSingleton(_state);
        Services.AddSingleton<TimeProvider>(_time);
        Services.AddSingleton<IOptionsMonitor<FcShellOptions>>(
            MakeOptionsMonitor(new FcShellOptions { SlowQueryThresholdMs = 2_000 }));
        Services.AddLogging();
        Services.AddLocalization();
        Services.AddFluentUIComponents();
    }

    private static IOptionsMonitor<FcShellOptions> MakeOptionsMonitor(FcShellOptions options) {
        IOptionsMonitor<FcShellOptions> monitor = Substitute.For<IOptionsMonitor<FcShellOptions>>();
        monitor.CurrentValue.Returns(options);
        return monitor;
    }

    private void SetElapsed(string viewKey, long elapsedMs) =>
        _stateValue = _stateValue with {
            LastElapsedMsByKey = _stateValue.LastElapsedMsByKey.SetItem(viewKey, elapsedMs),
        };

    [Fact]
    public void DoesNotRender_WhenElapsedIsBelowThreshold() {
        SetElapsed("acme:OrdersProjection", 500);

        IRenderedComponent<FcSlowQueryNotice> cut = Render<FcSlowQueryNotice>(p => p
            .Add(c => c.ViewKey, "acme:OrdersProjection"));

        cut.FindAll("[data-testid=\"fc-slow-query-notice\"]").Count.ShouldBe(0);
    }

    [Fact]
    public void Renders_WhenElapsedExceedsThreshold() {
        SetElapsed("acme:OrdersProjection", 5_000);

        IRenderedComponent<FcSlowQueryNotice> cut = Render<FcSlowQueryNotice>(p => p
            .Add(c => c.ViewKey, "acme:OrdersProjection"));

        cut.FindAll("[data-testid=\"fc-slow-query-notice\"]").Count.ShouldBe(1);
    }

    [Fact]
    public void AutoDismisses_AfterFiveSeconds() {
        SetElapsed("acme:OrdersProjection", 5_000);

        IRenderedComponent<FcSlowQueryNotice> cut = Render<FcSlowQueryNotice>(p => p
            .Add(c => c.ViewKey, "acme:OrdersProjection"));

        cut.FindAll("[data-testid=\"fc-slow-query-notice\"]").Count.ShouldBe(1);

        _time.Advance(TimeSpan.FromSeconds(5));
        cut.WaitForAssertion(() =>
            cut.FindAll("[data-testid=\"fc-slow-query-notice\"]").Count.ShouldBe(0));
    }
}
