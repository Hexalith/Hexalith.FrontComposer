using System.Globalization;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Components.EventStore;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.ReconnectionReconciliation;

/// <summary>
/// Story 2.6 AC2 integration pin — wires the <em>real</em> reconcile engine to the <em>real</em> status surface
/// across the one seam the unit suites only cover in isolation: <see cref="ReconnectionReconciliationCoordinator"/>
/// → <see cref="ReconnectionReconciliationStateService"/> → <see cref="FcProjectionConnectionStatus"/>.
/// On a simulated reconnect reconcile pass it asserts that a <em>changed</em> result drives the
/// "Reconnected -- data refreshed" confirmation (auto-clearing after the configured notice duration) and emits a
/// sweep marker for the changed lane, while a <em>no-change</em> result stays silent and emits no marker.
/// Confirm-and-pin: ZERO <c>src/</c> change.
/// </summary>
public sealed class ReconnectReconcileStatusIntegrationTests : BunitContext {
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);

    private const int NoticeDurationMs = 1_000;

    private readonly FakeTimeProvider _time = new(FixedNow);
    private readonly ProjectionConnectionStateService _connection;
    private readonly ReconnectionReconciliationStateService _reconciliation;
    private readonly RecordingDispatcher _dispatcher = new();

    public ReconnectReconcileStatusIntegrationTests() {
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddSingleton<IStringLocalizer<FcShellResources>>(new StatusStubLocalizer());
        Services.AddFluentUIComponents();
        Services.AddSingleton<TimeProvider>(_time);

        _connection = new ProjectionConnectionStateService(_time, NullLogger<ProjectionConnectionStateService>.Instance);
        _reconciliation = new ReconnectionReconciliationStateService(_time, NullLogger<ReconnectionReconciliationStateService>.Instance);
        Services.AddSingleton<IProjectionConnectionState>(_connection);
        Services.AddSingleton<IReconnectionReconciliationState>(_reconciliation);

        IOptionsMonitor<FcShellOptions> options = Substitute.For<IOptionsMonitor<FcShellOptions>>();
        options.CurrentValue.Returns(new FcShellOptions { ProjectionReconnectedNoticeDurationMs = NoticeDurationMs });
        Services.AddSingleton(options);
    }

    [Fact]
    public async Task ReconcileWithChanges_SurfacesConfirmationCopy_EmitsSweepMarker_ThenAutoClears() {
        StubScheduler scheduler = new(new ProjectionReconciliationRefreshResult(1, ["orders"]));
        ReconnectionReconciliationCoordinator coordinator = new(
            scheduler,
            _reconciliation,
            _dispatcher,
            _time,
            NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();
        // Connection is Connected after the reconnect; only then does the reconcile confirmation win.
        _connection.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Connected));

        _ = await coordinator.ReconcileAsync(Xunit.TestContext.Current.CancellationToken);

        // Coordinator → real reconciliation state → real status component renders the confirmation.
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Reconnected -- data refreshed"));
        scheduler.Epochs.ShouldHaveSingleItem();

        // AC2 — changed lane gets a reconciliation sweep marker.
        _dispatcher.Actions.OfType<MarkReconciliationSweepAction>().ShouldHaveSingleItem()
            .ViewKeys.ShouldBe(["orders"]);

        // Auto-clears after the configured notice duration.
        _time.Advance(TimeSpan.FromMilliseconds(NoticeDurationMs + 100));
        cut.WaitForAssertion(() => cut.Markup.ShouldNotContain("Reconnected -- data refreshed"));
    }

    [Fact]
    public async Task ReconcileWithoutChanges_StaysSilent_AndEmitsNoSweepMarker() {
        StubScheduler scheduler = new(new ProjectionReconciliationRefreshResult(1, []));
        ReconnectionReconciliationCoordinator coordinator = new(
            scheduler,
            _reconciliation,
            _dispatcher,
            _time,
            NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();
        _connection.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Connected));

        _ = await coordinator.ReconcileAsync(Xunit.TestContext.Current.CancellationToken);

        cut.WaitForAssertion(() => _reconciliation.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Idle));
        cut.Markup.ShouldNotContain("Reconnected -- data refreshed");
        _dispatcher.Actions.OfType<MarkReconciliationSweepAction>().ShouldBeEmpty();
    }

    private sealed class StubScheduler(ProjectionReconciliationRefreshResult result) : IProjectionFallbackRefreshScheduler {
        public List<long> Epochs { get; } = [];

        public IDisposable RegisterLane(ProjectionFallbackLane lane) => new NoopDisposable();

        public Task<int> TriggerFallbackOnceAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<int> TriggerNudgeRefreshAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<ProjectionReconciliationRefreshResult> TriggerReconciliationOnceAsync(long epoch, CancellationToken cancellationToken = default) {
            Epochs.Add(epoch);
            return Task.FromResult(result);
        }
    }

    private sealed class NoopDisposable : IDisposable {
        public void Dispose() {
        }
    }

    private sealed class RecordingDispatcher : IDispatcher {
        public List<object> Actions { get; } = [];

#pragma warning disable CS0067 // Required by Fluxor IDispatcher contract; no subscribers in this pin.
        public event EventHandler<ActionDispatchedEventArgs>? ActionDispatched;
#pragma warning restore CS0067

        public void Dispatch(object action) => Actions.Add(action);
    }

    private sealed class StatusStubLocalizer : IStringLocalizer<FcShellResources> {
        private static readonly Dictionary<string, string> Strings = new(StringComparer.Ordinal) {
            ["ReconnectStatusText"] = "Reconnecting...",
            ["ReconciliationStatusText"] = "Refreshing data...",
            ["ReconnectedDataRefreshedText"] = "Reconnected -- data refreshed",
            ["SectionUpdatingText"] = "This section is being updated",
        };

        public LocalizedString this[string name]
            => Strings.TryGetValue(name, out string? value)
                ? new LocalizedString(name, value)
                : new LocalizedString(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments]
            => Strings.TryGetValue(name, out string? value)
                ? new LocalizedString(name, string.Format(CultureInfo.InvariantCulture, value, arguments))
                : new LocalizedString(name, name, resourceNotFound: true);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Strings.Select(static kv => new LocalizedString(kv.Key, kv.Value));
    }
}
