using Bunit;

using Hexalith.FrontComposer.Shell.Components.Rendering;
using Hexalith.FrontComposer.Shell.Services.Announcements;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

public sealed class FcProjectionStaleNoticeTests : BunitContext {
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
            .Add(c => c.HasFailedRefresh, true));

        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-projection-stale-notice']")
            .TextContent.ShouldBe("Data may be out of date."));
        _time.Advance(TimeSpan.FromMilliseconds(250));
        _announcements.Current("projection:test").ShouldBe("Data could not be loaded.");
    }
}
