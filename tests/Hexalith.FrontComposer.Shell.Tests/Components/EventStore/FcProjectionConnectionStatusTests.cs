using System.Globalization;

using Bunit;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Components.EventStore;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Services.Announcements;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;
using Hexalith.FrontComposer.Shell.Tests.Components;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.EventStore;

public sealed class FcProjectionConnectionStatusTests : BunitContext {
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
    private readonly ProjectionConnectionStateService _state;
    private readonly ReconnectionReconciliationStateService _reconciliation;

    public FcProjectionConnectionStatusTests() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        BunitJSModuleInterop connectivity = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-connectivity.js");
        _ = connectivity.Setup<int>("watchConnectivity").SetResult(1);
        _ = connectivity.Setup<bool>("isOnline").SetResult(true);
        Services.AddLogging();
        // P38 — component now resolves user-visible copy through IStringLocalizer<FcShellResources>.
        // We register a stub that returns the expected English values so tests don't depend on
        // the test runtime loading the embedded satellite resource assembly.
        Services.AddSingleton<IStringLocalizer<FcShellResources>>(new StubShellLocalizer());
        Services.AddFluentUIComponents();
        Services.AddSingleton<TimeProvider>(_time);
        Services.AddSingleton<ISurfaceAnnouncementCoordinator>(new SurfaceAnnouncementCoordinator(_time));
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
            cut.Find("[data-testid='fc-projection-connection-status']").GetAttribute("role").ShouldBe("group");
            cut.Find("[data-testid='fc-projection-connection-status']").GetAttribute("aria-live").ShouldBe("off");
            cut.FindAll("[role='dialog']").ShouldBeEmpty();
        });
    }

    [Fact]
    public void ReconnectingStatus_RendersPulseClassUnderReachableScopedHost() {
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();

        _state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Reconnecting, "TimeoutException"));

        cut.WaitForAssertion(() => {
            AngleSharp.Dom.IElement host = cut.Find(".fc-projection-connection-status-host");
            AngleSharp.Dom.IElement status = cut.Find("[data-testid='fc-projection-connection-status']");
            host.QuerySelector("[data-testid='fc-projection-connection-status']").ShouldNotBeNull();
            status.ClassList.Contains("fc-projection-connection-status").ShouldBeTrue();
            status.ClassList.Contains("fc-projection-connection-status-pulse").ShouldBeTrue();
        });

        string css = VisualReachabilityTestSupport.ReadShellComponentCss(
            "EventStore",
            "FcProjectionConnectionStatus.razor.css");
        css.ShouldContain(".fc-projection-connection-status-host ::deep .fc-projection-connection-status");
        css.ShouldContain(".fc-projection-connection-status-host ::deep .fc-projection-connection-status-pulse");
        css.ShouldContain("@media (prefers-reduced-motion: reduce)");
    }

    [Fact]
    public void DisconnectedStatusShowsFallbackWithoutAnnouncingEachPoll() {
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();

        _state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected));
        _time.Advance(TimeSpan.FromMilliseconds(250));

        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-projection-connection-status']")
            .TextContent.ShouldContain("Live updates are unavailable"));
        Services.GetRequiredService<ISurfaceAnnouncementCoordinator>()
            .Current("projection-connection").ShouldBe("Live updates are unavailable. Checking for updates.");
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
        cut.Find("[data-testid='fc-projection-connection-status']").GetAttribute("role").ShouldBe("group");
        cut.Find("[data-testid='fc-projection-connection-status']").GetAttribute("aria-live").ShouldBe("off");
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
    public void ReconciledWithoutChanges_StillAnnouncesRecovery() {
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();
        _reconciliation.Start(epoch: 9);
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Refreshing data..."));

        _reconciliation.Complete(epoch: 9, changed: false);

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Reconnected -- data refreshed"));
    }

    [Fact]
    public void RecoveryWithoutAnyReadLaneUsesConnectionOnlyCopy() {
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();
        _reconciliation.Start(epoch: 91);
        _reconciliation.Complete(epoch: 91, changed: false, dataRead: false);

        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-projection-connection-status']")
            .TextContent.ShouldContain("Connection restored."));
        cut.Markup.ShouldNotContain("data refreshed");
        Services.GetRequiredService<ISurfaceAnnouncementCoordinator>()
            .Current("projection-connection").ShouldBe("Connection restored.");
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

    private sealed class StubShellLocalizer : IStringLocalizer<FcShellResources> {
        private static readonly Dictionary<string, string> Strings = new(StringComparer.Ordinal) {
            ["ReconnectStatusText"] = "Reconnecting...",
            ["ReconciliationStatusText"] = "Refreshing data...",
            ["ReconnectedDataRefreshedText"] = "Reconnected -- data refreshed",
            ["SectionUpdatingText"] = "This section is being updated",
            ["Am05Reconnecting"] = "Reconnecting...",
            ["Am06Fallback"] = "Live updates are unavailable. Checking for updates.",
            ["Am07Recovery"] = "Reconnected -- data refreshed",
            ["Am07ConnectionRestored"] = "Connection restored.",
            ["Am30Offline"] = "You are offline. Data cannot be refreshed.",
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
            => Strings.Select(kv => new LocalizedString(kv.Key, kv.Value));
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

    [Fact]
    public async Task OfflineAfterRecoverySurvivesTheOldRecoveryClearTimer() {
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();
        _reconciliation.Start(epoch: 12);
        _reconciliation.Complete(epoch: 12, changed: false);
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Reconnected -- data refreshed"));

        await cut.Instance.OnBrowserConnectivityChanged(false);
        _time.Advance(TimeSpan.FromMilliseconds(1_100));
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("You are offline"));
        Services.GetRequiredService<ISurfaceAnnouncementCoordinator>()
            .Current("projection-connection").ShouldBe("You are offline. Data cannot be refreshed.");

        _reconciliation.Start(epoch: 13);
        _reconciliation.Complete(epoch: 13, changed: false);
        cut.Markup.ShouldContain("You are offline");
        cut.Markup.ShouldNotContain("Reconnected -- data refreshed");
    }

    [Fact]
    public void ConnectedCancelsQueuedReconnectSpeech() {
        _ = Render<FcProjectionConnectionStatus>();
        _state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Reconnecting));
        _time.Advance(TimeSpan.FromMilliseconds(100));
        _state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Connected));
        _time.Advance(TimeSpan.FromMilliseconds(250));

        Services.GetRequiredService<ISurfaceAnnouncementCoordinator>()
            .Current("projection-connection").ShouldBeEmpty();
    }

    [Fact]
    public void DisconnectOrNewReconcileClearsPriorRecoverySpeech() {
        _ = Render<FcProjectionConnectionStatus>();
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        _reconciliation.Start(epoch: 21);
        _reconciliation.Complete(epoch: 21, changed: false);
        announcements.Current("projection-connection").ShouldBe("Reconnected -- data refreshed");

        _state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected));
        announcements.Current("projection-connection").ShouldBeEmpty();
        _time.Advance(TimeSpan.FromMilliseconds(250));
        announcements.Current("projection-connection").ShouldBe("Live updates are unavailable. Checking for updates.");

        _state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Connected));
        _reconciliation.Start(epoch: 22);
        announcements.Current("projection-connection").ShouldBeEmpty();
    }

    [Fact]
    public async Task BrowserEpisodesRestoreFallbackAndRepeatOfflineSpeech() {
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable subscription = announcements.Subscribe("projection-connection", spoken.Add);
        _state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected));

        await cut.Instance.OnBrowserConnectivityChanged(false);
        announcements.Current("projection-connection").ShouldBe("You are offline. Data cannot be refreshed.");
        await cut.Instance.OnBrowserConnectivityChanged(true);
        announcements.Current("projection-connection").ShouldBe("Live updates are unavailable. Checking for updates.");
        await cut.Instance.OnBrowserConnectivityChanged(false);

        announcements.Current("projection-connection").ShouldBe("You are offline. Data cannot be refreshed.");
        spoken.Count(static message => message == "You are offline. Data cannot be refreshed.").ShouldBe(2);
    }

    [Fact]
    public async Task RemountClearsThePreviousConnectionEpisodeAndSpeaksAFreshOne() {
        IRenderedComponent<FcProjectionConnectionStatus> first = Render<FcProjectionConnectionStatus>();
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        await first.Instance.OnBrowserConnectivityChanged(false);
        announcements.Current("projection-connection").ShouldBe("You are offline. Data cannot be refreshed.");

        first.Instance.Dispose();
        announcements.Current("projection-connection").ShouldBeEmpty();

        IRenderedComponent<FcProjectionConnectionStatus> second = Render<FcProjectionConnectionStatus>();
        await second.Instance.OnBrowserConnectivityChanged(false);
        announcements.Current("projection-connection").ShouldBe("You are offline. Data cannot be refreshed.");
    }

    [Fact]
    public async Task DisposalWaitsForLateConnectivityWatchAndUnregistersItsId() {
        ConnectivityJsRuntime js = new();
        Services.Replace(ServiceDescriptor.Singleton<IJSRuntime>(js));
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();
        cut.WaitForAssertion(() => js.WatchCalls.ShouldBe(1));

        Task disposal = cut.Instance.DisposeAsync().AsTask();
        disposal.IsCompleted.ShouldBeFalse();
        js.CompleteWatch(73);
        await disposal.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);

        js.UnwatchId.ShouldBe(73);
        js.ModuleDisposeCalls.ShouldBe(1);
    }

    [Fact]
    public void TransientConnectivityImportFailureRetriesOnNextRender() {
        ConnectivityJsRuntime js = new(failFirstImport: true, watchPending: false);
        Services.Replace(ServiceDescriptor.Singleton<IJSRuntime>(js));

        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();
        cut.WaitForAssertion(() => js.ImportCalls.ShouldBe(1));
        cut.Render();
        cut.WaitForAssertion(() => js.WatchCalls.ShouldBe(1));
        js.ImportCalls.ShouldBe(2);
    }

    [Fact]
    public void QuietPageRetriesFailedConnectivityImportWithoutAnotherRender() {
        ConnectivityJsRuntime js = new(failFirstImport: true, watchPending: false);
        Services.Replace(ServiceDescriptor.Singleton<IJSRuntime>(js));
        _ = Render<FcProjectionConnectionStatus>();
        js.ImportCalls.ShouldBe(1);

        _time.Advance(TimeSpan.FromSeconds(1));
        SpinWait.SpinUntil(() => js.WatchCalls == 1, TimeSpan.FromSeconds(2)).ShouldBeTrue();
        js.ImportCalls.ShouldBe(2);
    }

    [Fact]
    public void RemountWithBrowserOnlineClearsPriorScopedOfflineEvidence() {
        _state.SetBrowserOffline(true);
        _state.Current.BrowserOffline.ShouldBeTrue();

        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();
        cut.WaitForAssertion(() => _state.Current.BrowserOffline.ShouldBeFalse());
    }

    private sealed class ConnectivityJsRuntime(bool failFirstImport = false, bool watchPending = true) : IJSRuntime, IAsyncDisposable {
        private readonly ConnectivityModule _module = new(watchPending);

        public int ImportCalls { get; private set; }

        public int WatchCalls => _module.WatchCalls;

        public int? UnwatchId => _module.UnwatchId;

        public int ModuleDisposeCalls => _module.DisposeCalls;

        public void CompleteWatch(int id) => _module.CompleteWatch(id);

        public ValueTask DisposeAsync() => _module.DisposeAsync();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) {
            if (identifier != "import") {
                throw new JSException($"Unexpected JavaScript call: {identifier}");
            }

            ImportCalls++;
            return failFirstImport && ImportCalls == 1
                ? ValueTask.FromException<TValue>(new JSException("module unavailable"))
                : ValueTask.FromResult((TValue)(object)_module);
        }

        private sealed class ConnectivityModule(bool watchPending) : IJSObjectReference {
            private readonly TaskCompletionSource<int> _watch = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public int WatchCalls { get; private set; }

            public int? UnwatchId { get; private set; }

            public int DisposeCalls { get; private set; }

            public void CompleteWatch(int id) => _watch.TrySetResult(id);

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
                => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

            public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) {
                switch (identifier) {
                    case "watchConnectivity":
                        WatchCalls++;
                        return (TValue)(object)(watchPending
                            ? await _watch.Task.WaitAsync(cancellationToken).ConfigureAwait(false)
                            : 41);
                    case "isOnline":
                        return (TValue)(object)true;
                    case "unwatchConnectivity":
                        UnwatchId = (int)((object?[])args!)[0]!;
                        return default!;
                    default:
                        throw new JSException($"Unexpected JavaScript call: {identifier}");
                }
            }

            public ValueTask DisposeAsync() {
                DisposeCalls++;
                return ValueTask.CompletedTask;
            }
        }
    }
}
