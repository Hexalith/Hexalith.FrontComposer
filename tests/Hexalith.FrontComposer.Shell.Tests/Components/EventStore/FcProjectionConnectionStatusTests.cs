using Bunit;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Components.EventStore;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

using Microsoft.Extensions.DependencyInjection;
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
        _reconciliation.Start(epoch: 7);

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Refreshing data..."));
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

        cut.WaitForAssertion(() => cut.Markup.ShouldNotContain("Reconnected -- data refreshed"));
        cut.Markup.ShouldNotContain("No changes");
    }
}
