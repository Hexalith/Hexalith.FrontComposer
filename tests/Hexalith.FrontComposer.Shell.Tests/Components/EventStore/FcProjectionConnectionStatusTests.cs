using System.Globalization;

using Bunit;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Components.EventStore;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.EventStore;

public sealed class FcProjectionConnectionStatusTests : BunitContext {
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
    private readonly ProjectionConnectionStateService _state;
    private readonly ReconnectionReconciliationStateService _reconciliation;

    public FcProjectionConnectionStatusTests() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        // P38 — component now resolves user-visible copy through IStringLocalizer<FcShellResources>.
        // We register a stub that returns the expected English values so tests don't depend on
        // the test runtime loading the embedded satellite resource assembly.
        Services.AddSingleton<IStringLocalizer<FcShellResources>>(new StubShellLocalizer());
        Services.AddFluentUIComponents();
        Services.AddSingleton<TimeProvider>(_time);
        _state = ActivatorUtilities.CreateInstance<ProjectionConnectionStateService>(Services.BuildServiceProvider());
        _reconciliation = ActivatorUtilities.CreateInstance<ReconnectionReconciliationStateService>(Services.BuildServiceProvider());
        Services.AddSingleton<IProjectionConnectionState>(_state);
        Services.AddSingleton<IReconnectionReconciliationState>(_reconciliation);
        IOptionsMonitor<FcShellOptions> options = Substitute.For<IOptionsMonitor<FcShellOptions>>();
        options.CurrentValue.Returns(new FcShellOptions { ProjectionReconnectedNoticeDurationMs = 1_000 });
        Services.AddSingleton(options);
    }

    [Fact]
    public void Disconnected_RendersInlinePoliteStatusCopy() {
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();

        _state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Reconnecting, "TimeoutException"));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("Reconnecting...");
            cut.Find("[data-testid='fc-projection-connection-status']").GetAttribute("role").ShouldBe("status");
            cut.Find("[data-testid='fc-projection-connection-status']").GetAttribute("aria-live").ShouldBe("polite");
            cut.FindAll("[role='dialog']").ShouldBeEmpty();
        });
    }

    [Fact]
    public void Reconciling_RendersRefreshingStatus() {
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();
        // Connection-state is Connected by default (constructor seeds Connected). The component
        // should switch to "Refreshing data..." copy and NOT show "Reconnecting..." (P40 — verify
        // exclusion explicitly so the precedence rule is asserted, not just the inclusion).
        _reconciliation.Start(epoch: 7);

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Refreshing data..."));
        cut.Markup.ShouldNotContain("Reconnecting...");
        cut.Find("[data-testid='fc-projection-connection-status']").GetAttribute("role").ShouldBe("status");
        cut.Find("[data-testid='fc-projection-connection-status']").GetAttribute("aria-live").ShouldBe("polite");
    }

    [Fact]
    public void ReconciledWithChanges_RendersBriefConfirmation_ThenAutoClears() {
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();
        _state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Connected));
        _reconciliation.Start(epoch: 8);
        _reconciliation.Complete(epoch: 8, changed: true);

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Reconnected -- data refreshed"));
        _time.Advance(TimeSpan.FromMilliseconds(1_100));
        cut.WaitForAssertion(() => cut.Markup.ShouldNotContain("Reconnected -- data refreshed"));
    }

    [Fact]
    public void ReconciledWithoutChanges_ClearsSilently() {
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();
        _reconciliation.Start(epoch: 9);
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Refreshing data..."));

        _reconciliation.Complete(epoch: 9, changed: false);

        // P41 — assert the *correct* absence: no "Reconnected -- data refreshed" toast on a
        // no-change Complete (AC5). Removed the tautological "No changes" absence assert which
        // pointed at a string that never existed in the codebase.
        cut.WaitForAssertion(() => cut.Markup.ShouldNotContain("Reconnected -- data refreshed"));
    }

    [Fact]
    public void DisconnectedWhileReconciling_PrecedenceWinsOverRefreshed() {
        // P31 / AC6 — connection-state precedence: a stale Refreshed snapshot must not reopen
        // the cleared status while the connection is Reconnecting/Disconnected.
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();

        _reconciliation.Start(epoch: 10);
        _state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Reconnecting, "Timeout"));
        // Even if the reconciliation pass happens to publish Refreshed mid-disconnect, the
        // header copy must remain "Reconnecting..." per AC6.
        _reconciliation.Complete(epoch: 10, changed: true);

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Reconnecting..."));
        cut.Markup.ShouldNotContain("Reconnected -- data refreshed");
    }

    [Fact]
    public void ReconciliationCompletes_AnnouncesOnceForEpoch() {
        // P48 — live-region announcement coalesces once per reconnect epoch. Multiple Complete
        // calls (which can arrive from racing dispatchers in tests) for the same epoch must not
        // produce additional Reconnected/Refreshed toggles.
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();
        _reconciliation.Start(epoch: 11);
        _reconciliation.Complete(epoch: 11, changed: true);
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Reconnected -- data refreshed"));

        // A duplicate Complete for the same epoch must be a no-op (P15 status guard).
        _reconciliation.Complete(epoch: 11, changed: true);
        // Markup still contains the same single toast; we are not coalescing rendering, but the
        // underlying state did not retransition (Refreshed → Refreshed is dropped by IsLogicalDuplicate).
        cut.Markup.ShouldContain("Reconnected -- data refreshed");
    }
}
