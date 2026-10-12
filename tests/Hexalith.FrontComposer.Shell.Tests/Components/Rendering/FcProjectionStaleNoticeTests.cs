using Bunit;

using Hexalith.FrontComposer.Shell.Components.Rendering;
using Hexalith.FrontComposer.Shell.Services.Announcements;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

public sealed class FcProjectionStaleNoticeTests : BunitContext {
    [Fact]
    public void CoMountedNoticesEmitDistinctStableBrowserOwnerIds()
    {
        IRenderedComponent<FcProjectionStaleNotice> first = Render<FcProjectionStaleNotice>(p => p
            .Add(c => c.Surface, "projection:test").Add(c => c.HasCachedRows, true)
            .Add(c => c.SharedStatusOwner, true));
        IRenderedComponent<FcProjectionStaleNotice> second = Render<FcProjectionStaleNotice>(p => p
            .Add(c => c.Surface, "projection:test").Add(c => c.HasCachedRows, true)
            .Add(c => c.SharedStatusOwner, true));
        string firstOwner = first.Find("[data-fc-browser-stale]").GetAttribute("data-fc-stale-owner").ShouldNotBeNull();
        string secondOwner = second.Find("[data-fc-browser-stale]").GetAttribute("data-fc-stale-owner").ShouldNotBeNull();
        firstOwner.ShouldNotBeNullOrWhiteSpace();
        secondOwner.ShouldNotBeNullOrWhiteSpace();
        firstOwner.ShouldNotBe(secondOwner);

        first.Render();
        second.Render();
        first.Find("[data-fc-browser-stale]").GetAttribute("data-fc-stale-owner").ShouldBe(firstOwner);
        second.Find("[data-fc-browser-stale]").GetAttribute("data-fc-stale-owner").ShouldBe(secondOwner);
    }

    [Fact]
    public void ScopeRemountSpeaksCurrentStaleStateWithUnchangedConnectionSnapshot()
    {
        List<string> spoken = [];
        using IDisposable subscription = _announcements.Subscribe("projection:test", spoken.Add);
        _connection.SetBrowserOffline(true);
        IRenderedComponent<FcProjectionStaleNotice> first = Render<FcProjectionStaleNotice>(p => p
            .Add(c => c.Surface, "projection:test").Add(c => c.HasCachedRows, true)
            .Add(c => c.ScopeGeneration, 1));
        _time.Advance(TimeSpan.FromMilliseconds(250));
        spoken.Count(message => message == "Data may be out of date.").ShouldBe(1);
        first.Instance.Dispose();
        first.Dispose();

        IRenderedComponent<FcProjectionStaleNotice> second = Render<FcProjectionStaleNotice>(p => p
            .Add(c => c.Surface, "projection:test").Add(c => c.HasCachedRows, true)
            .Add(c => c.ScopeGeneration, 2));
        _time.Advance(TimeSpan.FromMilliseconds(250));
        second.WaitForAssertion(() => spoken.Count(message => message == "Data may be out of date.").ShouldBe(2));
    }

    [Fact]
    public void CoMountedStaleNoticesShareOneEpisodeAndLastOwnerClearsIt()
    {
        List<string> spoken = [];
        using IDisposable subscription = _announcements.Subscribe("projection:test", spoken.Add);
        IRenderedComponent<FcProjectionStaleNotice> first = Render<FcProjectionStaleNotice>(p => p
            .Add(c => c.Surface, "projection:test").Add(c => c.HasCachedRows, true).Add(c => c.SharedStatusOwner, true));
        IRenderedComponent<FcProjectionStaleNotice> second = Render<FcProjectionStaleNotice>(p => p
            .Add(c => c.Surface, "projection:test").Add(c => c.HasCachedRows, true).Add(c => c.SharedStatusOwner, true));

        _connection.SetBrowserOffline(true);
        _time.Advance(TimeSpan.FromMilliseconds(250));
        first.WaitForAssertion(() => spoken.Count(message => message == "Data may be out of date.").ShouldBe(1));
        first.Instance.Dispose();
        _announcements.Current("projection:test").ShouldBe("Data may be out of date.");
        second.Instance.Dispose();
        _announcements.Current("projection:test").ShouldBeEmpty();
    }

    [Fact]
    public void FailureCategoryChangesInsideOneDisconnectDoNotRepeatStaleSpeech()
    {
        List<string> spoken = [];
        using IDisposable subscription = _announcements.Subscribe("projection:test", spoken.Add);
        IRenderedComponent<FcProjectionStaleNotice> cut = Render<FcProjectionStaleNotice>(p => p
            .Add(c => c.Surface, "projection:test").Add(c => c.HasCachedRows, true));
        _connection.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected, FailureCategory: "Closed"));
        _time.Advance(TimeSpan.FromMilliseconds(250));
        cut.WaitForAssertion(() => spoken.Count(message => message == "Data may be out of date.").ShouldBe(1));

        _connection.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected, FailureCategory: "RestartTimeout"));
        _connection.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected, FailureCategory: "RestartFailed"));
        cut.Render();
        _time.Advance(TimeSpan.FromMilliseconds(250));
        spoken.Count(message => message == "Data may be out of date.").ShouldBe(1);
    }

    [Fact]
    public void OwnerMountedDuringSecondOfflineEpisodeSharesExistingSpeech()
    {
        List<string> spoken = [];
        using IDisposable subscription = _announcements.Subscribe("projection:test", spoken.Add);
        IRenderedComponent<FcProjectionStaleNotice> first = Render<FcProjectionStaleNotice>(p => p
            .Add(c => c.Surface, "projection:test").Add(c => c.HasCachedRows, true)
            .Add(c => c.SharedStatusOwner, true));

        _connection.SetBrowserOffline(true);
        _time.Advance(TimeSpan.FromMilliseconds(250));
        first.WaitForAssertion(() => spoken.Count(message => message == "Data may be out of date.").ShouldBe(1));
        _connection.SetBrowserOffline(false);
        first.WaitForAssertion(() => _announcements.Current("projection:test").ShouldBeEmpty());
        _connection.SetBrowserOffline(true);
        _time.Advance(TimeSpan.FromMilliseconds(250));
        first.WaitForAssertion(() => spoken.Count(message => message == "Data may be out of date.").ShouldBe(2));

        IRenderedComponent<FcProjectionStaleNotice> second = Render<FcProjectionStaleNotice>(p => p
            .Add(c => c.Surface, "projection:test").Add(c => c.HasCachedRows, true)
            .Add(c => c.SharedStatusOwner, true));
        _time.Advance(TimeSpan.FromMilliseconds(250));
        spoken.Count(message => message == "Data may be out of date.").ShouldBe(2);
        first.Instance.Dispose();
        _announcements.Current("projection:test").ShouldBe("Data may be out of date.");
        second.Instance.Dispose();
        _announcements.Current("projection:test").ShouldBeEmpty();
    }

    [Fact]
    public void OwnerMountedDuringSecondFailedRefreshSharesExistingSpeech()
    {
        List<string> spoken = [];
        using IDisposable subscription = _announcements.Subscribe("projection:test", spoken.Add);
        IRenderedComponent<FcProjectionStaleNotice> first = Render<FcProjectionStaleNotice>(p => p
            .Add(c => c.Surface, "projection:test").Add(c => c.HasCachedRows, true)
            .Add(c => c.HasFailedRefresh, true).Add(c => c.SharedStatusOwner, true));
        _time.Advance(TimeSpan.FromMilliseconds(250));
        spoken.Count(message => message == "Data may be out of date.").ShouldBe(1);
        first.Render(p => p.Add(c => c.HasFailedRefresh, false));
        _announcements.Current("projection:test").ShouldBeEmpty();
        first.Render(p => p.Add(c => c.HasFailedRefresh, true));
        _time.Advance(TimeSpan.FromMilliseconds(250));
        spoken.Count(message => message == "Data may be out of date.").ShouldBe(2);

        IRenderedComponent<FcProjectionStaleNotice> second = Render<FcProjectionStaleNotice>(p => p
            .Add(c => c.Surface, "projection:test").Add(c => c.HasCachedRows, true)
            .Add(c => c.HasFailedRefresh, true).Add(c => c.SharedStatusOwner, true));
        _time.Advance(TimeSpan.FromMilliseconds(250));
        spoken.Count(message => message == "Data may be out of date.").ShouldBe(2);
        first.Instance.Dispose();
        _announcements.Current("projection:test").ShouldBe("Data may be out of date.");
        second.Instance.Dispose();
        _announcements.Current("projection:test").ShouldBeEmpty();
    }

    private readonly FakeTimeProvider _time = new();
    private readonly ProjectionConnectionStateService _connection;
    private readonly SurfaceAnnouncementCoordinator _announcements;

    public FcProjectionStaleNoticeTests() {
        Services.AddLocalization();
        Services.AddSingleton<TimeProvider>(_time);
        _connection = new ProjectionConnectionStateService(_time, NullLogger<ProjectionConnectionStateService>.Instance);
        _announcements = new SurfaceAnnouncementCoordinator(_time);
        Services.AddSingleton<IProjectionConnectionState>(_connection);
        Services.AddSingleton<ISurfaceAnnouncementCoordinator>(_announcements);
    }

    [Fact]
    public void CachedRowsBecomeVisiblyStaleAndAnnounceOnceAfterTheWindow() {
        IRenderedComponent<FcProjectionStaleNotice> cut = Render<FcProjectionStaleNotice>(p => p
            .Add(c => c.Surface, "projection:test")
            .Add(c => c.HasCachedRows, true));

        _connection.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Reconnecting));
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-projection-stale-notice']")
            .GetAttribute("aria-live").ShouldBe("off"));
        _announcements.Current("projection:test").ShouldBeEmpty();

        _time.Advance(TimeSpan.FromMilliseconds(250));
        cut.WaitForAssertion(() => _announcements.Current("projection:test")
            .ShouldBe("Data may be out of date."));

        _connection.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Connected));
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='fc-projection-stale-notice']")
            .ShouldBeEmpty());
        cut.WaitForAssertion(() => _announcements.Current("projection:test").ShouldBeEmpty());
    }

    [Fact]
    public void ReconnectAttemptsKeepOneStaleEpisodeUntilRecoveryOrNewFailureEvidence()
    {
        List<string> updates = [];
        using IDisposable subscription = _announcements.Subscribe("projection:test", updates.Add);
        IRenderedComponent<FcProjectionStaleNotice> cut = Render<FcProjectionStaleNotice>(p => p
            .Add(c => c.Surface, "projection:test").Add(c => c.HasCachedRows, true));
        _connection.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Reconnecting, FailureCategory: "transport"));
        _time.Advance(TimeSpan.FromMilliseconds(250));
        cut.WaitForAssertion(() => _announcements.Current("projection:test").ShouldBe("Data may be out of date."));
        for (int attempt = 0; attempt < 4; attempt++)
        {
            _time.Advance(TimeSpan.FromSeconds(1));
            _connection.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Reconnecting, FailureCategory: "transport"));
            _time.Advance(TimeSpan.FromMilliseconds(250));
        }
        updates.Count(static message => message == "Data may be out of date.").ShouldBe(1);

        _connection.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Connected));
        cut.WaitForAssertion(() => _announcements.Current("projection:test").ShouldBeEmpty());
        _connection.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Reconnecting, FailureCategory: "transport"));
        _time.Advance(TimeSpan.FromMilliseconds(250));
        cut.WaitForAssertion(() => updates.Count(static message => message == "Data may be out of date.").ShouldBe(2));
    }

    [Fact]
    public void NoCachedRowsDoNotShowStaleDataCopy() {
        IRenderedComponent<FcProjectionStaleNotice> cut = Render<FcProjectionStaleNotice>(p => p
            .Add(c => c.Surface, "projection:test")
            .Add(c => c.HasCachedRows, false));

        _connection.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected));
        _time.Advance(TimeSpan.FromMilliseconds(250));

        cut.FindAll("[data-testid='fc-projection-stale-notice']").ShouldBeEmpty();
        _announcements.Current("projection:test").ShouldBeEmpty();
    }

    [Fact]
    public void BrowserOfflineLabelsCachedRowsBeforeTransportChanges() {
        IRenderedComponent<FcProjectionStaleNotice> cut = Render<FcProjectionStaleNotice>(p => p
            .Add(c => c.Surface, "projection:test")
            .Add(c => c.HasCachedRows, true));

        _connection.SetBrowserOffline(true);
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-projection-stale-notice']")
            .TextContent.ShouldBe("Data may be out of date."));
        _time.Advance(TimeSpan.FromMilliseconds(250));
        cut.WaitForAssertion(() => _announcements.Current("projection:test").ShouldBe("Data may be out of date."));

        _connection.SetBrowserOffline(false);
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='fc-projection-stale-notice']").ShouldBeEmpty());
        _announcements.Current("projection:test").ShouldBeEmpty();
    }

    [Fact]
    public void RepeatedOfflineAndFailedRefreshEpisodesEachAnnounceOnce() {
        List<string> spoken = [];
        using IDisposable subscription = _announcements.Subscribe("projection:test", spoken.Add);
        IRenderedComponent<FcProjectionStaleNotice> cut = Render<FcProjectionStaleNotice>(p => p
            .Add(c => c.Surface, "projection:test").Add(c => c.HasCachedRows, true));

        _connection.SetBrowserOffline(true);
        _time.Advance(TimeSpan.FromMilliseconds(250));
        cut.WaitForAssertion(() => spoken.Count(message => message == "Data may be out of date.").ShouldBe(1));
        _connection.SetBrowserOffline(false);
        cut.WaitForAssertion(() => _announcements.Current("projection:test").ShouldBeEmpty());
        _connection.SetBrowserOffline(true);
        _time.Advance(TimeSpan.FromMilliseconds(250));
        cut.WaitForAssertion(() => spoken.Count(message => message == "Data may be out of date.").ShouldBe(2));
        _connection.SetBrowserOffline(false);
        cut.Render(p => p.Add(c => c.Surface, "projection:test").Add(c => c.HasCachedRows, true)
            .Add(c => c.HasFailedRefresh, true));
        _time.Advance(TimeSpan.FromMilliseconds(250));
        cut.WaitForAssertion(() => spoken.Count(message => message == "Data may be out of date.").ShouldBe(3));
        cut.Render(p => p.Add(c => c.Surface, "projection:test").Add(c => c.HasCachedRows, true)
            .Add(c => c.HasFailedRefresh, false));
        _announcements.Current("projection:test").ShouldBeEmpty();
        cut.Render(p => p.Add(c => c.Surface, "projection:test").Add(c => c.HasCachedRows, true)
            .Add(c => c.HasFailedRefresh, true));
        _time.Advance(TimeSpan.FromMilliseconds(250));
        cut.WaitForAssertion(() => spoken.Count(message => message == "Data may be out of date.").ShouldBe(4));
    }

    [Fact]
    public void DirectStaleNoticeKeepsOnePoliteNodeAcrossDistinctEpisodes()
    {
        IRenderedComponent<FcProjectionStaleNotice> cut = Render<FcProjectionStaleNotice>(p => p
            .Add(c => c.Surface, "projection:test").Add(c => c.HasCachedRows, true));
        cut.FindAll("[role='status'][aria-live='polite']").Count.ShouldBe(1);
        _connection.SetBrowserOffline(true);
        _time.Advance(TimeSpan.FromMilliseconds(250));
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-surface-status']")
            .TextContent.ShouldBe("Data may be out of date."));
        _connection.SetBrowserOffline(false);
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-surface-status']").TextContent.ShouldBeEmpty());
        _connection.SetBrowserOffline(true);
        _time.Advance(TimeSpan.FromMilliseconds(250));
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-surface-status']")
            .TextContent.ShouldBe("Data may be out of date."));
        cut.Render(p => p.Add(c => c.SharedStatusOwner, true));
        cut.FindAll("[data-testid='fc-surface-status']").ShouldBeEmpty();
    }

    [Fact]
    public void TerminalQueryFailureSpeechRemainsWhenTheStaleGroupQueues() {
        _announcements.Announce(
            "projection:test",
            "load:failed",
            "failure",
            "Data could not be loaded.",
            terminal: true);
        IRenderedComponent<FcProjectionStaleNotice> cut = Render<FcProjectionStaleNotice>(p => p
            .Add(c => c.Surface, "projection:test")
            .Add(c => c.HasCachedRows, true)
            .Add(c => c.HasFailedRefresh, true)
            .Add(c => c.QueryFailureGroup, "load:failed")
            .Add(c => c.QueryFailureIdentity, "failure"));

        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-projection-stale-notice']")
            .TextContent.ShouldBe("Data may be out of date."));
        _time.Advance(TimeSpan.FromMilliseconds(250));
        _announcements.Current("projection:test").ShouldBe("Data could not be loaded.");
    }
    [Theory]
    [InlineData("Orders loaded.", false)]
    [InlineData("No orders available.", true)]
    public void EarlierNonFailureSpeechYieldsToTheStaleWarning(string earlier, bool terminal) {
        List<string> spoken = [];
        using IDisposable subscription = _announcements.Subscribe("projection:test", spoken.Add);
        _announcements.Announce("projection:test", "earlier", "result", earlier, terminal: terminal, immediate: true);
        IRenderedComponent<FcProjectionStaleNotice> cut = Render<FcProjectionStaleNotice>(p => p
            .Add(c => c.Surface, "projection:test")
            .Add(c => c.HasCachedRows, true)
            .Add(c => c.HasFailedRefresh, true)
            .Add(c => c.QueryFailureGroup, "current-failure")
            .Add(c => c.QueryFailureIdentity, "failure"));

        cut.Find("[data-testid='fc-projection-stale-notice']").TextContent.ShouldBe("Data may be out of date.");
        _time.Advance(TimeSpan.FromMilliseconds(249));
        _announcements.Current("projection:test").ShouldBe(earlier);
        _time.Advance(TimeSpan.FromMilliseconds(1));
        _announcements.Current("projection:test").ShouldBe("Data may be out of date.");
        cut.Render();
        _time.Advance(TimeSpan.FromMilliseconds(250));
        spoken.Count(message => message == "Data may be out of date.").ShouldBe(1);
    }

    [Theory]
    [InlineData("en", "This section is being updated")]
    [InlineData("fr", "Cette section est en cours de mise à jour")]
    public void CurrentClassifiedFailureKeepsItsLocalizedSpeechAcrossRenders(string culture, string failure) {
        System.Globalization.CultureInfo prior = System.Globalization.CultureInfo.CurrentUICulture;
        try {
            System.Globalization.CultureInfo.CurrentUICulture = new(culture);
            _announcements.Announce("projection:test", "page:17", "failure", failure, terminal: true);
            IRenderedComponent<FcProjectionStaleNotice> cut = Render<FcProjectionStaleNotice>(p => p
                .Add(c => c.Surface, "projection:test")
                .Add(c => c.HasCachedRows, true)
                .Add(c => c.HasFailedRefresh, true)
                .Add(c => c.QueryFailureGroup, "page:17")
                .Add(c => c.QueryFailureIdentity, "failure"));

            cut.Find("[data-testid='fc-projection-stale-notice']").ShouldNotBeNull();
            cut.Render();
            _time.Advance(TimeSpan.FromMilliseconds(500));
            _announcements.Current("projection:test").ShouldBe(failure);
        }
        finally {
            System.Globalization.CultureInfo.CurrentUICulture = prior;
        }
    }

    [Fact]
    public async Task CapturedConnectionCallbackCannotRepublishAfterDisposal()
    {
        var connection = NSubstitute.Substitute.For<IProjectionConnectionState>();
        Action<ProjectionConnectionSnapshot>? callback = null;
        connection.Subscribe(NSubstitute.Arg.Do<Action<ProjectionConnectionSnapshot>>(value => callback = value))
            .Returns(NSubstitute.Substitute.For<IDisposable>());
        connection.Current.Returns(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Connected, DateTimeOffset.MinValue, 0, null));
        Services.AddSingleton(connection);
        IRenderedComponent<FcProjectionStaleNotice> cut = Render<FcProjectionStaleNotice>(p => p
            .Add(c => c.Surface, "projection:test").Add(c => c.HasCachedRows, true));
        cut.Instance.Dispose();
        ProjectionConnectionSnapshot disconnected = new(ProjectionConnectionStatus.Disconnected, DateTimeOffset.MinValue, 0, null);
        connection.Current.Returns(disconnected);
        await cut.InvokeAsync(() => callback.ShouldNotBeNull().Invoke(disconnected)).ConfigureAwait(true);
        _time.Advance(TimeSpan.FromMilliseconds(250));
        _announcements.Current("projection:test").ShouldBeEmpty();
    }
    [Fact]
    public void RemountedOwnerAndEqualInputSurfaceRebindSpeakFreshOutages()
    {
        List<string> spoken = [];
        using IDisposable subscription = _announcements.Subscribe("projection:test", spoken.Add);
        _connection.SetBrowserOffline(true);
        IRenderedComponent<FcProjectionStaleNotice> first = Render<FcProjectionStaleNotice>(p => p.Add(c => c.Surface, "projection:test").Add(c => c.HasCachedRows, true));
        _time.Advance(TimeSpan.FromMilliseconds(250));
        first.WaitForAssertion(() => spoken.Count(message => message.Length > 0).ShouldBe(1));
        first.Instance.Dispose();
        first.Dispose();
        _announcements.Current("projection:test").ShouldBeEmpty();
        _connection.SetBrowserOffline(false);
        _connection.SetBrowserOffline(true);
        IRenderedComponent<FcProjectionStaleNotice> remounted = Render<FcProjectionStaleNotice>(p => p.Add(c => c.Surface, "projection:test").Add(c => c.HasCachedRows, true));
        _time.Advance(TimeSpan.FromMilliseconds(250));
        remounted.WaitForAssertion(() => spoken.Count(message => message.Length > 0).ShouldBe(2));
        remounted.Render(p => p.Add(c => c.Surface, "projection:replacement").Add(c => c.HasCachedRows, true));
        _announcements.Current("projection:test").ShouldBeEmpty();
        _time.Advance(TimeSpan.FromMilliseconds(250));
        remounted.WaitForAssertion(() => _announcements.Current("projection:replacement").ShouldBe("Data may be out of date."));
        _announcements.Announce("projection:replacement", "unrelated", "result", "Keep unrelated speech", immediate: true);
        remounted.Instance.Dispose();
        remounted.Dispose();
        _announcements.Current("projection:replacement").ShouldBe("Keep unrelated speech");
    }


}
