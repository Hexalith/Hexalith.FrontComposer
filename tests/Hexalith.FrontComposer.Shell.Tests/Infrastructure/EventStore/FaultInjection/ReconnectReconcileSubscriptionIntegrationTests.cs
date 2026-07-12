#pragma warning disable CA2007 // ConfigureAwait — test code (matches FaultInjection directory convention)

using System.Collections.Immutable;
using System.Globalization;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Shell.Components.EventStore;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Infrastructure.ProjectionConnection;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
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

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore.FaultInjection;

/// <summary>
/// Story 2.6 AC2 end-to-end pin — drives a reconnect through the <em>real</em>
/// <see cref="ProjectionSubscriptionService"/> connection-state seam
/// (<c>OnConnectionStateChangedAsync</c>) and asserts the full chain composes:
/// the fault harness raises <c>Reconnecting</c>→<c>Reconnected</c>, the service rejoins its active
/// groups, the <em>real</em> <see cref="ReconnectionReconciliationCoordinator"/> runs one reconcile
/// pass against the <em>real</em> <see cref="ProjectionFallbackRefreshScheduler"/>, a sweep marker is
/// dispatched for the changed lane, and the <em>real</em> <see cref="FcProjectionConnectionStatus"/>
/// surfaces "Reconnecting…" → "Reconnected -- data refreshed" (then auto-clears).
/// </summary>
/// <remarks>
/// This closes the seam the existing coverage only exercises in isolation: the fault suites drive the
/// subscription service but with a <em>stub</em> scheduler/coordinator (and never inject a
/// reconciliation coordinator), while the dev-story AC2 pin
/// (<c>ReconnectReconcileStatusIntegrationTests</c>) starts at <c>coordinator.ReconcileAsync(...)</c>
/// directly, bypassing the subscription service's reconnect trigger. Here every collaborator is the
/// production type, wired exactly as DI wires them in the running shell. Confirm-and-pin: ZERO
/// <c>src/</c> change.
/// </remarks>
public sealed class ReconnectReconcileSubscriptionIntegrationTests : BunitContext {
    private const string ProjectionType = "orders";
    private const string TenantId = "acme";
    private const string ViewKey = "orders:acme";
    private const string HubPath = "/hubs/projection-changes";
    private const int NoticeDurationMs = 1_000;
    private const string ReconnectingCopy = "Reconnecting...";
    private const string RefreshedCopy = "Reconnected -- data refreshed";

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero));
    private readonly ProjectionConnectionStateService _connection;
    private readonly ReconnectionReconciliationStateService _reconciliation;
    private readonly ProjectionFallbackRefreshScheduler _scheduler;
    private readonly RecordingDispatcher _dispatcher = new();

    public ReconnectReconcileSubscriptionIntegrationTests() {
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
        options.CurrentValue.Returns(new FcShellOptions {
            ProjectionReconnectedNoticeDurationMs = NoticeDurationMs,
            MaxProjectionFallbackPollingLanes = 8,
        });
        Services.AddSingleton(options);

        _scheduler = new ProjectionFallbackRefreshScheduler(
            _connection,
            Substitute.For<IProjectionPageLoader>(),
            options,
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
    }

    [Fact]
    public async Task ReconnectThroughSubscriptionService_WithChangedReconcile_RejoinsSurfacesConfirmationAndSweepMarker() {
        Func<int> laneRefreshes = RegisterLane(ProjectionFallbackLaneRefreshOutcome.Changed);
        ReconnectionReconciliationCoordinator coordinator = CreateCoordinator();
        await using FaultInjectingProjectionHubConnection harness = new();
        await using ProjectionSubscriptionService sut = CreateSut(harness, coordinator);

        // Establish the active subscription (Start + first Join) BEFORE the drop.
        await sut.SubscribeAsync(ProjectionType, TenantId, Xunit.TestContext.Current.CancellationToken);
        harness.GetHitCount(HarnessCheckpoint.Join(ProjectionType, TenantId)).ShouldBe(1);

        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();

        // Connection drops — the status surfaces "Reconnecting…".
        await harness.RaiseStateAsync(HarnessConnectionStates.Reconnecting(new IOException("transport")));
        cut.WaitForAssertion(() => cut.Markup.ShouldContain(ReconnectingCopy));

        // Reconnect — the service rejoins, the real coordinator reconciles, the changed lane gets a
        // sweep marker, and the status flips to the confirmation.
        await harness.RaiseStateAsync(HarnessConnectionStates.Reconnected("conn-2"));

        // Rejoin ran exactly once more for the active group.
        harness.GetHitCount(HarnessCheckpoint.Join(ProjectionType, TenantId)).ShouldBe(2);
        // The reconcile pass actually refreshed the registered lane through the real scheduler.
        laneRefreshes().ShouldBeGreaterThanOrEqualTo(1);
        // AC2 — the changed lane received a reconciliation sweep marker.
        _dispatcher.Actions.OfType<MarkReconciliationSweepAction>().ShouldHaveSingleItem()
            .ViewKeys.ShouldBe([ViewKey]);
        // AC2 — the mounted status component surfaces the "changed" confirmation.
        cut.WaitForAssertion(() => cut.Markup.ShouldContain(RefreshedCopy));

        // The confirmation auto-clears after the configured notice duration.
        _time.Advance(TimeSpan.FromMilliseconds(NoticeDurationMs + 100));
        cut.WaitForAssertion(() => cut.Markup.ShouldNotContain(RefreshedCopy));
    }

    [Fact]
    public async Task ReconnectThroughSubscriptionService_WithUnchangedReconcile_RejoinsButStaysSilent() {
        _ = RegisterLane(ProjectionFallbackLaneRefreshOutcome.NotModified);
        ReconnectionReconciliationCoordinator coordinator = CreateCoordinator();
        await using FaultInjectingProjectionHubConnection harness = new();
        await using ProjectionSubscriptionService sut = CreateSut(harness, coordinator);

        await sut.SubscribeAsync(ProjectionType, TenantId, Xunit.TestContext.Current.CancellationToken);
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();

        await harness.RaiseStateAsync(HarnessConnectionStates.Reconnected("conn-2"));

        // Rejoin still happened on reconnect.
        harness.GetHitCount(HarnessCheckpoint.Join(ProjectionType, TenantId)).ShouldBe(2);
        // Reconcile completed back to Idle without surfacing a confirmation.
        cut.WaitForAssertion(() => _reconciliation.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Idle));
        cut.Markup.ShouldNotContain(RefreshedCopy);
        // AC2 — a no-change reconcile emits no sweep marker.
        _dispatcher.Actions.OfType<MarkReconciliationSweepAction>().ShouldBeEmpty();
    }

    private Func<int> RegisterLane(ProjectionFallbackLaneRefreshOutcome outcome) {
        int count = 0;
        _ = _scheduler.RegisterLane(new ProjectionFallbackLane(
            ViewKey,
            ProjectionType,
            TenantId,
            Skip: 0,
            Take: 50,
            Filters: ImmutableDictionary<string, string>.Empty,
            SortColumn: null,
            SortDescending: false,
            SearchQuery: null,
            RefreshAsync: cancellationToken => {
                cancellationToken.ThrowIfCancellationRequested();
                _ = Interlocked.Increment(ref count);
                return ValueTask.FromResult(outcome);
            }));
        return () => Volatile.Read(ref count);
    }

    private ReconnectionReconciliationCoordinator CreateCoordinator()
        => new(
            _scheduler,
            _reconciliation,
            _dispatcher,
            _time,
            NullLogger<ReconnectionReconciliationCoordinator>.Instance);

    private ProjectionSubscriptionService CreateSut(
        FaultInjectingProjectionHubConnection harness,
        IReconnectionReconciliationCoordinator coordinator) {
        EventStoreOptions options = new() {
            BaseAddress = new Uri("https://eventstore.test"),
            RequireAccessToken = false,
            ProjectionChangesHubPath = HubPath,
        };
        FaultInjectingProjectionHubConnectionFactory factory = new(harness, new Uri($"https://eventstore.test{HubPath}"));
        return new ProjectionSubscriptionService(
            global::Microsoft.Extensions.Options.Options.Create(options),
            factory,
            _connection,
            _scheduler,
            new TestNotifier(),
            NullLogger<ProjectionSubscriptionService>.Instance,
            fallbackDriver: null,
            reconciliationCoordinator: coordinator);
    }

    private sealed class TestNotifier : IProjectionChangeNotifier {
        public event Action<string>? ProjectionChanged;

        public void NotifyChanged(string projectionType) => ProjectionChanged?.Invoke(projectionType);
    }

    private sealed class RecordingDispatcher : IDispatcher {
        private readonly object _sync = new();
        private readonly List<object> _actions = [];

        public IReadOnlyList<object> Actions {
            get {
                lock (_sync) {
                    return [.. _actions];
                }
            }
        }

#pragma warning disable CS0067 // Required by Fluxor IDispatcher contract; no subscribers in this pin.
        public event EventHandler<ActionDispatchedEventArgs>? ActionDispatched;
#pragma warning restore CS0067

        public void Dispatch(object action) {
            lock (_sync) {
                _actions.Add(action);
            }
        }
    }

    private sealed class StatusStubLocalizer : IStringLocalizer<FcShellResources> {
        private static readonly Dictionary<string, string> Strings = new(StringComparer.Ordinal) {
            ["ReconnectStatusText"] = ReconnectingCopy,
            ["ReconciliationStatusText"] = "Refreshing data...",
            ["ReconnectedDataRefreshedText"] = RefreshedCopy,
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
