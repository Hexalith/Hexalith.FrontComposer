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
    [Fact]
    public void SameEpochConnectionReturnSpeaksCurrentReconnectingOnce()
    {
        _ = Render<FcProjectionConnectionStatus>();
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable subscription = announcements.Subscribe("projection-connection", message => {
            if (message.Length > 0) { spoken.Add(message); }
        });
        _state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Reconnecting));
        _time.Advance(TimeSpan.FromMilliseconds(250));
        _state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected));
        _time.Advance(TimeSpan.FromMilliseconds(250));
        _state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Reconnecting));
        _time.Advance(TimeSpan.FromMilliseconds(250));
        _state.Current.Epoch.ShouldBe(1);
        spoken.ShouldBe(["Reconnecting...", "Live updates are unavailable. Checking for updates.", "Reconnecting..."]);
        announcements.Current("projection-connection").ShouldBe("Reconnecting...");
    }

    [Fact]
    public async Task LegacyBoolCallbackRetainsItsClrSignatureAndUniqueInteropIdentifier()
    {
        System.Reflection.MethodInfo legacy = typeof(FcProjectionConnectionStatus)
            .GetMethod("OnBrowserConnectivityChanged", [typeof(bool)]).ShouldNotBeNull();
        legacy.ReturnType.ShouldBe(typeof(Task));
        legacy.GetParameters().Length.ShouldBe(1);
        var interop = typeof(FcProjectionConnectionStatus).GetMethods()
            .Select(method => (Method: method,
                Attribute: System.Reflection.CustomAttributeExtensions.GetCustomAttribute<JSInvokableAttribute>(method)))
            .Where(entry => entry.Attribute is not null)
            .Select(entry => (entry.Method, Identifier: entry.Attribute!.Identifier ?? entry.Method.Name))
            .ToArray();
        interop.Select(entry => entry.Identifier).Distinct(StringComparer.Ordinal).Count().ShouldBe(interop.Length);
        interop.Single(entry => entry.Method == legacy).Identifier.ShouldBe("OnBrowserConnectivityChanged");
        interop.Single(entry => entry.Identifier == "OnBrowserConnectivityObserved").Method.GetParameters().Length.ShouldBe(2);
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();
        await cut.InvokeAsync(() => ((Task)legacy.Invoke(cut.Instance, [false])!));
        _state.Current.BrowserOffline.ShouldBeTrue();
        Services.GetRequiredService<ISurfaceAnnouncementCoordinator>().Current("projection-connection")
            .ShouldBe("You are offline. Data cannot be refreshed.");
        await cut.InvokeAsync(() => ((Task)legacy.Invoke(cut.Instance, [true])!));
        _state.Current.BrowserOffline.ShouldBeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HeldInitialConnectivityReadCannotReplaceNewerBrowserEvidenceIncludingAba(bool initialOnline)
    {
        ConnectivityJsRuntime js = new(watchPending: false, holdInitialRead: true);
        Services.Replace(ServiceDescriptor.Singleton<IJSRuntime>(js));
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();
        cut.WaitForAssertion(() => js.ReadCalls.ShouldBe(1));
        await cut.InvokeAsync(() => cut.Instance.OnBrowserConnectivityObserved(false, 1));
        if (!initialOnline)
        {
            await cut.InvokeAsync(() => cut.Instance.OnBrowserConnectivityObserved(true, 2));
        }
        js.CompleteInitialRead(initialOnline);
        cut.WaitForAssertion(() => _state.Current.BrowserOffline.ShouldBe(initialOnline));
        await cut.InvokeAsync(() => cut.Instance.OnBrowserConnectivityObserved(!initialOnline, 3));
        // A delayed native callback is also superseded by the newer producer revision.
        await cut.InvokeAsync(() => cut.Instance.OnBrowserConnectivityObserved(initialOnline, 1));
        _state.Current.BrowserOffline.ShouldBe(initialOnline);
        if (initialOnline)
        {
            Services.GetRequiredService<ISurfaceAnnouncementCoordinator>().Current("projection-connection")
                .ShouldBe("You are offline. Data cannot be refreshed.");
        }
        await cut.Instance.DisposeAsync();
    }

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
            AngleSharp.Dom.IElement status = cut.Find("[data-testid='fc-projection-connection-status']");
            AngleSharp.Dom.IElement host = status.Closest(".fc-projection-connection-status-host").ShouldNotBeNull();
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
    public void RenderedDisconnectedAndReconcilingStatusExposeBrowserFallbackMetadata() {
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();

        _state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected));
        cut.WaitForAssertion(() => cut.Find("[data-fc-status-revision]")
            .GetAttribute("data-fc-status-fallback").ShouldBe("true"));

        _state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Connected));
        _reconciliation.Start(epoch: 7);
        cut.WaitForAssertion(() => cut.Find("[data-fc-status-revision]")
            .GetAttribute("data-fc-status-fallback").ShouldBe("true"));
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

    [Fact]
    public async Task CurrentConnectionEpochDecidesRecoveryBeforeQueuedConnectionRender()
    {
        IProjectionConnectionState connection = Substitute.For<IProjectionConnectionState>();
        ProjectionConnectionSnapshot current = new(ProjectionConnectionStatus.Connected, _time.GetUtcNow(), 0, null) { Epoch = 1 };
        connection.Current.Returns(_ => current);
        Action<ProjectionConnectionSnapshot>? callback = null;
        connection.Subscribe(Arg.Any<Action<ProjectionConnectionSnapshot>>(), Arg.Any<bool>())
            .Returns(call => {
                callback = call.Arg<Action<ProjectionConnectionSnapshot>>();
                return Substitute.For<IDisposable>();
            });
        Services.Replace(ServiceDescriptor.Singleton(connection));
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();
        current = current with { Status = ProjectionConnectionStatus.Reconnecting };
        await cut.InvokeAsync(() => callback.ShouldNotBeNull()(current));
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Reconnecting..."));

        // The authoritative connection is already healthy; its renderer callback has not run.
        current = current with { Status = ProjectionConnectionStatus.Connected };
        _reconciliation.Start(20);
        _reconciliation.Complete(20, changed: true);
        cut.WaitForAssertion(() => Services.GetRequiredService<ISurfaceAnnouncementCoordinator>()
            .Current("projection-connection").ShouldBe("Reconnected -- data refreshed"));

        _reconciliation.Start(21);
        current = current with { Status = ProjectionConnectionStatus.Disconnected, Epoch = 2 };
        current = current with { Status = ProjectionConnectionStatus.Connected };
        _reconciliation.Complete(21, changed: true);
        Services.GetRequiredService<ISurfaceAnnouncementCoordinator>()
            .Current("projection-connection").ShouldNotBe("Reconnected -- data refreshed");
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
    public async Task LostWatcherReturnDisposesByBindingBeforeRetry()
    {
        ConnectivityJsRuntime js = new(watchPending: false, timeoutWatch: true);
        Services.Replace(ServiceDescriptor.Singleton<IJSRuntime>(js));
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();
        cut.WaitForAssertion(() => js.WatchCalls.ShouldBe(1));
        DotNetObjectReference<FcProjectionConnectionStatus> reference = js.Reference.ShouldNotBeNull();

        await cut.Instance.DisposeAsync();

        js.BindingUnwatchCalls.ShouldBe(1);
        js.UnwatchId.ShouldBeNull();
        Should.Throw<ObjectDisposedException>(() => reference.Value);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConnectivityInteropTimeoutRetriesOnQuietPageAndStillReceivesOfflineEvidence(bool duringWatch) {
        ConnectivityJsRuntime js = new(watchPending: false, timeoutImport: !duringWatch, timeoutWatch: duringWatch);
        Services.Replace(ServiceDescriptor.Singleton<IJSRuntime>(js));
        _state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected));
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();
        cut.WaitForAssertion(() => js.ImportCalls.ShouldBe(1));
        _time.Advance(TimeSpan.FromMilliseconds(250));
        cut.WaitForAssertion(() => cut.Find("[data-fc-browser-status]").TextContent
            .ShouldBe("Live updates are unavailable. Checking for updates."));
        _time.Advance(TimeSpan.FromSeconds(1));
        cut.WaitForAssertion(() => {
            js.WatchCalls.ShouldBe(duringWatch ? 2 : 1);
            js.ImportCalls.ShouldBe(duringWatch ? 1 : 2);
        });
        await cut.Instance.OnBrowserConnectivityChanged(false);
        _state.Current.BrowserOffline.ShouldBeTrue();
        Services.GetRequiredService<ISurfaceAnnouncementCoordinator>().Current("projection-connection")
            .ShouldBe("You are offline. Data cannot be refreshed.");
        await cut.Instance.DisposeAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConnectivityTeardownTimeoutReleasesTheDotNetReferenceWithoutRestartingWork(bool duringModuleDisposal) {
        ConnectivityJsRuntime js = new(watchPending: false,
            timeoutUnwatch: !duringModuleDisposal, timeoutDispose: duringModuleDisposal);
        Services.Replace(ServiceDescriptor.Singleton<IJSRuntime>(js));
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();
        cut.WaitForAssertion(() => js.WatchCalls.ShouldBe(1));
        DotNetObjectReference<FcProjectionConnectionStatus> reference = js.Reference.ShouldNotBeNull();
        await cut.Instance.DisposeAsync();
        Should.Throw<ObjectDisposedException>(() => reference.Value);
        js.ModuleDisposeCalls.ShouldBe(1);
        _time.Advance(TimeSpan.FromSeconds(2));
        js.ImportCalls.ShouldBe(1);
        js.WatchCalls.ShouldBe(1);
    }

    private sealed class ConnectivityJsRuntime(bool failFirstImport = false, bool watchPending = true,
        bool timeoutImport = false, bool timeoutWatch = false, bool timeoutUnwatch = false, bool timeoutDispose = false,
        bool holdInitialRead = false) : IJSRuntime, IAsyncDisposable {
        private readonly ConnectivityModule _module = new(watchPending, timeoutWatch, timeoutUnwatch, timeoutDispose, holdInitialRead);

        public int ImportCalls { get; private set; }

        public int WatchCalls => _module.WatchCalls;

        public int ReadCalls => _module.ReadCalls;

        public void CompleteInitialRead(bool online) => _module.CompleteInitialRead(online);

        public int? UnwatchId => _module.UnwatchId;

        public int BindingUnwatchCalls => _module.BindingUnwatchCalls;

        public int ModuleDisposeCalls => _module.DisposeCalls;

        public DotNetObjectReference<FcProjectionConnectionStatus>? Reference => _module.Reference;

        public void CompleteWatch(int id) => _module.CompleteWatch(id);

        public ValueTask DisposeAsync() => _module.DisposeAsync();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) {
            if (identifier != "import") {
                throw new JSException($"Unexpected JavaScript call: {identifier}");
            }

            ImportCalls++;
            if (timeoutImport && ImportCalls == 1) {
                return ValueTask.FromException<TValue>(new TaskCanceledException("interop timeout"));
            }
            return failFirstImport && ImportCalls == 1
                ? ValueTask.FromException<TValue>(new JSException("module unavailable"))
                : ValueTask.FromResult((TValue)(object)_module);
        }

        private sealed class ConnectivityModule(bool watchPending, bool timeoutWatch, bool timeoutUnwatch, bool timeoutDispose, bool holdInitialRead) : IJSObjectReference {
            private readonly TaskCompletionSource<int> _watch = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource<bool> _initialRead = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public int WatchCalls { get; private set; }

            public int ReadCalls { get; private set; }

            public void CompleteInitialRead(bool online) => _initialRead.TrySetResult(online);

            public int? UnwatchId { get; private set; }

            public int BindingUnwatchCalls { get; private set; }

            public int DisposeCalls { get; private set; }

            public DotNetObjectReference<FcProjectionConnectionStatus>? Reference { get; private set; }

            public void CompleteWatch(int id) => _watch.TrySetResult(id);

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
                => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

            public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) {
                switch (identifier) {
                    case "watchConnectivity":
                        WatchCalls++;
                        Reference = (DotNetObjectReference<FcProjectionConnectionStatus>)args![0]!;
                        if (timeoutWatch && WatchCalls == 1) {
                            throw new TaskCanceledException("watcher timeout");
                        }
                        return (TValue)(object)(watchPending
                            ? await _watch.Task.WaitAsync(cancellationToken).ConfigureAwait(false)
                            : 41);
                    case "isOnline":
                        ReadCalls++;
                        return (TValue)(object)(holdInitialRead
                            ? await _initialRead.Task.WaitAsync(cancellationToken).ConfigureAwait(false)
                            : true);
                    case "unwatchConnectivity":
                        UnwatchId = (int)((object?[])args!)[0]!;
                        if (timeoutUnwatch) {
                            throw new TaskCanceledException("unwatch timeout");
                        }
                        return default!;
                    case "unwatchConnectivityBinding":
                        BindingUnwatchCalls++;
                        return default!;
                    default:
                        throw new JSException($"Unexpected JavaScript call: {identifier}");
                }
            }

            public ValueTask DisposeAsync() {
                DisposeCalls++;
                return timeoutDispose
                    ? ValueTask.FromException(new TaskCanceledException("module disposal timeout"))
                    : ValueTask.CompletedTask;
            }
        }
    }
    [Fact]
    public void DisposedConnectivityRetryCallbackDoesNotImportAgain()
    {
        var time = new Hexalith.FrontComposer.Shell.Tests.Infrastructure.QueuedTimerTimeProvider();
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(time));
        _ = JSInterop.TryGetInvokeHandler<IJSObjectReference>("import", "./_content/Hexalith.FrontComposer.Shell/js/fc-connectivity.js")!
            .SetException(new JSException("import unavailable"));
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();
        cut.WaitForAssertion(() => time.Callbacks.Count.ShouldBe(1));
        cut.Instance.Dispose();
        int imports = JSInterop.Invocations.Count(invocation => invocation.Identifier == "import");
        Should.NotThrow(() => time.Callbacks[0]());
        JSInterop.Invocations.Count(invocation => invocation.Identifier == "import").ShouldBe(imports);
    }

}
