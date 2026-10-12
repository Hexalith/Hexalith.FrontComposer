using System.Globalization;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Components.DataGrid;
using Hexalith.FrontComposer.Shell.Services.Announcements;
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
    [Fact]
    public void OlderPendingPageSlowThresholdKeepsCurrentFailureAndNewRequestCanSpeak()
    {
        const string view = "acme:OrdersProjection";
        TaskCompletionSource<object> failed = new();
        TaskCompletionSource<object> olderPending = new();
        _stateValue = LoadedPageReducers.ReduceLoadPage(_stateValue, PageAction(view, failed, _time.GetUtcNow()));
        _stateValue = LoadedPageReducers.ReduceLoadPage(_stateValue,
            new LoadPageAction(view, 20, 20, System.Collections.Immutable.ImmutableDictionary<string, string>.Empty,
                null, false, null, olderPending, CancellationToken.None) { RegisteredAt = _time.GetUtcNow() });
        IRenderedComponent<FcSlowQueryNotice> notice = Render<FcSlowQueryNotice>(p => p
            .Add(c => c.ViewKey, view).Add(c => c.SharedStatusOwner, true));
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable subscription = announcements.Subscribe("projection:" + view, spoken.Add);
        _time.Advance(TimeSpan.FromSeconds(1));
        _stateValue = LoadedPageReducers.ReduceLoadPageFailed(_stateValue,
            new LoadPageFailedAction(view, 0, "safe failure", failed) { Take = 20 });
        notice.Render();
        string failureGroup = "scope:0:page:" + _stateValue.LastResultByKey[view].Identity;
        announcements.Announce("projection:" + view, failureGroup, "failure", "Page failed", terminal: true);

        _time.Advance(TimeSpan.FromSeconds(1));
        notice.WaitForAssertion(() => notice.FindAll("[data-testid='fc-slow-query-notice']").Count.ShouldBe(1));
        announcements.Current("projection:" + view).ShouldBe("Page failed");
        spoken.ShouldBe(["Page failed"]);

        _stateValue = LoadedPageReducers.ReduceLoadPageCancelled(_stateValue,
            new LoadPageCancelledAction(view, 20, olderPending));
        _time.Advance(TimeSpan.FromTicks(1));
        TaskCompletionSource<object> newRequest = new();
        _stateValue = LoadedPageReducers.ReduceLoadPage(_stateValue, PageAction(view, newRequest, _time.GetUtcNow()));
        notice.Render();
        _time.Advance(TimeSpan.FromSeconds(2));
        notice.WaitForAssertion(() => announcements.Current("projection:" + view)
            .ShouldBe("This is taking longer than expected."));
        spoken.ShouldBe(["Page failed", "This is taking longer than expected."]);
        notice.FindAll("[role='status'][aria-live='polite']").Count.ShouldBe(0);
    }

    [Fact]
    public void CoMountedPrimaryCancellationKeepsOneSlowAnnouncement()
    {
        const string view = "acme:OrdersProjection";
        TaskCompletionSource<object> first = new();
        TaskCompletionSource<object> second = new();
        _stateValue = LoadedPageReducers.ReduceLoadPage(_stateValue, PageAction(view, first, _time.GetUtcNow()));
        IRenderedComponent<FcSlowQueryNotice> notice = Render<FcSlowQueryNotice>(p => p.Add(c => c.ViewKey, view));
        _time.Advance(TimeSpan.FromMilliseconds(500));
        _stateValue = LoadedPageReducers.ReduceLoadPage(_stateValue, PageAction(view, second, _time.GetUtcNow()));
        notice.Render();
        _time.Advance(TimeSpan.FromMilliseconds(1500));
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        notice.WaitForAssertion(() => announcements.Current("projection:" + view).ShouldBe("This is taking longer than expected."));
        _stateValue = LoadedPageReducers.ReduceLoadPageCancelled(_stateValue, new LoadPageCancelledAction(view, 0, first));
        notice.Render();
        announcements.Current("projection:" + view).ShouldBeEmpty();
        notice.FindAll("[data-testid='fc-slow-query-notice']").ShouldBeEmpty();
        _time.Advance(TimeSpan.FromMilliseconds(499));
        notice.FindAll("[data-testid='fc-slow-query-notice']").ShouldBeEmpty();
        _time.Advance(TimeSpan.FromMilliseconds(1));
        notice.WaitForAssertion(() => notice.FindAll("[data-testid='fc-slow-query-notice']").Count.ShouldBe(1));
        announcements.Current("projection:" + view).ShouldBeEmpty();
        first.Task.IsCanceled.ShouldBeTrue();
        second.Task.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public void PromotedProviderUsesItsOwnRegistrationDeadline()
    {
        const string view = "acme:OrdersProjection";
        TaskCompletionSource<object> first = new();
        TaskCompletionSource<object> second = new();
        _stateValue = LoadedPageReducers.ReduceLoadPage(_stateValue, PageAction(view, first, _time.GetUtcNow()));
        IRenderedComponent<FcSlowQueryNotice> notice = Render<FcSlowQueryNotice>(p => p.Add(c => c.ViewKey, view));
        _time.Advance(TimeSpan.FromMilliseconds(1500));
        _stateValue = LoadedPageReducers.ReduceLoadPage(_stateValue, PageAction(view, second, _time.GetUtcNow()));
        _stateValue = LoadedPageReducers.ReduceLoadPageCancelled(_stateValue, new LoadPageCancelledAction(view, 0, first));
        notice.Render();
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        _time.Advance(TimeSpan.FromMilliseconds(1999));
        announcements.Current("projection:" + view).ShouldBeEmpty();
        _time.Advance(TimeSpan.FromMilliseconds(1));
        notice.WaitForAssertion(() => announcements.Current("projection:" + view).ShouldBe("This is taking longer than expected."));
    }

    private static LoadPageAction PageAction(string view, TaskCompletionSource<object> completion, DateTimeOffset registeredAt)
        => new(view, 0, 20, System.Collections.Immutable.ImmutableDictionary<string, string>.Empty,
            null, false, null, completion, CancellationToken.None) { RegisteredAt = registeredAt };

    [Fact]
    public void NewlyEarlierRequestReleasesItsPredecessorClaim()
    {
        const string view = "acme:OrdersProjection";
        TaskCompletionSource<object> original = SetPending(view);
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        IRenderedComponent<FcSlowQueryNotice> notice = Render<FcSlowQueryNotice>(p => p.Add(c => c.ViewKey, view));
        _time.Advance(TimeSpan.FromSeconds(2));
        notice.WaitForAssertion(() => announcements.Current("projection:" + view).ShouldBe("This is taking longer than expected."));

        TaskCompletionSource<object> earlier = new();
        _stateValue = _stateValue with {
            PendingCompletionsByKey = _stateValue.PendingCompletionsByKey.Add((view, 20), earlier),
            PendingStartedAtByKey = _stateValue.PendingStartedAtByKey
                .SetItem((view, 0), _time.GetUtcNow())
                .Add((view, 20), _time.GetUtcNow().AddSeconds(-3)),
        };
        notice.Render();
        _stateValue = _stateValue with {
            PendingCompletionsByKey = _stateValue.PendingCompletionsByKey.Remove((view, 20)),
            PendingStartedAtByKey = _stateValue.PendingStartedAtByKey.Remove((view, 20)),
        };
        earlier.SetResult(new object());
        notice.Render();
        announcements.Current("projection:" + view).ShouldBeEmpty();
        original.SetResult(new object());
    }

    [Fact]
    public void CoMountedSlowNoticesRetainSpeechUntilTheLastOwnerLeaves()
    {
        const string view = "acme:OrdersProjection";
        _ = SetPending(view);
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        IRenderedComponent<FcSlowQueryNotice> first = Render<FcSlowQueryNotice>(p => p
            .Add(c => c.ViewKey, view).Add(c => c.SharedStatusOwner, true));
        IRenderedComponent<FcSlowQueryNotice> second = Render<FcSlowQueryNotice>(p => p
            .Add(c => c.ViewKey, view).Add(c => c.SharedStatusOwner, true));
        _time.Advance(TimeSpan.FromSeconds(2));
        first.WaitForAssertion(() => announcements.Current("projection:" + view)
            .ShouldBe("This is taking longer than expected."));
        first.Instance.Dispose();
        announcements.Current("projection:" + view).ShouldBe("This is taking longer than expected.");
        second.Instance.Dispose();
        announcements.Current("projection:" + view).ShouldBeEmpty();
    }

    [Fact]
    public void DirectSlowAndCapNoticesKeepTerminalThenNewRequestUsesOnePersistentStatus()
    {
        const string view = "acme:OrdersProjection";
        TaskCompletionSource<object> oldRequest = SetPending(view);
        IRenderedComponent<FcMaxItemsCapNotice> cap = Render<FcMaxItemsCapNotice>(p => p
            .Add(c => c.ViewKey, view).Add(c => c.ItemsCount, 10000).Add(c => c.AnyRealFilterActive, false)
            .Add(c => c.ResultIdentity, "initial"));
        IRenderedComponent<FcSlowQueryNotice> slow = Render<FcSlowQueryNotice>(p => p.Add(c => c.ViewKey, view));
        cap.FindAll("[role='status'][aria-live='polite']").Count.ShouldBe(1);
        slow.FindAll("[role='status'][aria-live='polite']").ShouldBeEmpty();
        cap.Find("[data-testid='fc-surface-status']").TextContent.ShouldContain("Showing the first 10000 items");
        _time.Advance(TimeSpan.FromMilliseconds(1999));
        cap.Find("[data-testid='fc-surface-status']").TextContent.ShouldNotContain("taking longer");
        _time.Advance(TimeSpan.FromMilliseconds(1));
        cap.Find("[data-testid='fc-surface-status']").TextContent.ShouldContain("Showing the first 10000 items");
        slow.FindAll("[data-testid='fc-slow-query-notice']").Count.ShouldBe(1);
        oldRequest.TrySetResult(new object());
        _ = SetPending(view);
        _stateValue = _stateValue with { PendingStartedAtByKey = _stateValue.PendingStartedAtByKey.SetItem((view, 0), _time.GetUtcNow()) };
        slow.Render();
        _time.Advance(TimeSpan.FromSeconds(2));
        cap.WaitForAssertion(() => cap.Find("[data-testid='fc-surface-status']").TextContent
            .ShouldBe("This is taking longer than expected."));
        cap.FindComponent<Hexalith.FrontComposer.Shell.Components.Rendering.FcSurfaceStatus>().Instance.Dispose();
        cap.Instance.Dispose();
        cap.Dispose();
        slow.WaitForAssertion(() => slow.Find("[data-testid='fc-surface-status']").TextContent
            .ShouldBe("This is taking longer than expected."));
        slow.FindAll("[role='status'][aria-live='polite']").Count.ShouldBe(1);
    }

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
        Services.AddSingleton<ISurfaceAnnouncementCoordinator>(new SurfaceAnnouncementCoordinator(_time));
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

    private TaskCompletionSource<object> SetPending(string viewKey) {
        TaskCompletionSource<object> completion = new();
        _stateValue = _stateValue with {
            PendingCompletionsByKey = _stateValue.PendingCompletionsByKey.SetItem((viewKey, 0), completion),
        };
        return completion;
    }

    [Fact]
    public void DoesNotRender_WhenElapsedIsBelowThreshold() {
        _ = SetPending("acme:OrdersProjection");

        IRenderedComponent<FcSlowQueryNotice> cut = Render<FcSlowQueryNotice>(p => p
            .Add(c => c.ViewKey, "acme:OrdersProjection"));

        cut.FindAll("[data-testid=\"fc-slow-query-notice\"]").Count.ShouldBe(0);
        _time.Advance(TimeSpan.FromMilliseconds(1_999));
        cut.FindAll("[data-testid=\"fc-slow-query-notice\"]").Count.ShouldBe(0);
    }

    [Fact]
    public void Renders_WhenElapsedExceedsThreshold() {
        _ = SetPending("acme:OrdersProjection");

        IRenderedComponent<FcSlowQueryNotice> cut = Render<FcSlowQueryNotice>(p => p
            .Add(c => c.ViewKey, "acme:OrdersProjection"));

        _time.Advance(TimeSpan.FromMilliseconds(2_000));
        cut.WaitForAssertion(() => cut.FindAll("[data-testid=\"fc-slow-query-notice\"]").Count.ShouldBe(1));
    }

    [Fact]
    public void RemountRetainsRegisteredRequestDeadline() {
        _ = SetPending("acme:OrdersProjection");
        _stateValue = _stateValue with {
            PendingStartedAtByKey = _stateValue.PendingStartedAtByKey.SetItem(("acme:OrdersProjection", 0), _time.GetUtcNow()),
        };
        IRenderedComponent<FcSlowQueryNotice> first = Render<FcSlowQueryNotice>(p => p
            .Add(c => c.ViewKey, "acme:OrdersProjection"));
        _time.Advance(TimeSpan.FromMilliseconds(1_500));
        first.Instance.Dispose();
        first.Dispose();
        IRenderedComponent<FcSlowQueryNotice> remounted = Render<FcSlowQueryNotice>(p => p
            .Add(c => c.ViewKey, "acme:OrdersProjection"));
        _time.Advance(TimeSpan.FromMilliseconds(499));
        remounted.FindAll("[data-testid=\"fc-slow-query-notice\"]").ShouldBeEmpty();
        _time.Advance(TimeSpan.FromMilliseconds(1));
        remounted.WaitForAssertion(() => remounted.FindAll("[data-testid=\"fc-slow-query-notice\"]").Count.ShouldBe(1));
    }

    [Fact]
    public void ClearsWhenPendingQuerySettles() {
        TaskCompletionSource<object> completion = SetPending("acme:OrdersProjection");

        IRenderedComponent<FcSlowQueryNotice> cut = Render<FcSlowQueryNotice>(p => p
            .Add(c => c.ViewKey, "acme:OrdersProjection"));

        _time.Advance(TimeSpan.FromMilliseconds(2_000));
        cut.WaitForAssertion(() => cut.FindAll("[data-testid=\"fc-slow-query-notice\"]").Count.ShouldBe(1));
        completion.SetResult(new object());
        _stateValue = _stateValue with { PendingCompletionsByKey = _stateValue.PendingCompletionsByKey.Clear() };
        cut.Render(p => p.Add(c => c.ViewKey, "acme:OrdersProjection"));
        cut.WaitForAssertion(() =>
            cut.FindAll("[data-testid=\"fc-slow-query-notice\"]").Count.ShouldBe(0));
    }

    [Fact]
    public void ConcurrentPagesKeepTheEarliestDeadlineAndClearEachSpokenResult() {
        TaskCompletionSource<object> first = SetPending("acme:OrdersProjection");
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        IRenderedComponent<FcSlowQueryNotice> cut = Render<FcSlowQueryNotice>(p => p
            .Add(c => c.ViewKey, "acme:OrdersProjection"));

        _time.Advance(TimeSpan.FromMilliseconds(1_000));
        TaskCompletionSource<object> second = new();
        _stateValue = _stateValue with {
            PendingCompletionsByKey = _stateValue.PendingCompletionsByKey.SetItem(("acme:OrdersProjection", 500), second),
        };
        cut.Render(p => p.Add(c => c.ViewKey, "acme:OrdersProjection"));

        _time.Advance(TimeSpan.FromMilliseconds(999));
        announcements.Current("projection:acme:OrdersProjection").ShouldBeEmpty();
        _time.Advance(TimeSpan.FromMilliseconds(1));
        cut.WaitForAssertion(() => announcements.Current("projection:acme:OrdersProjection")
            .ShouldBe("This is taking longer than expected."));

        first.SetResult(new object());
        _stateValue = _stateValue with {
            PendingCompletionsByKey = _stateValue.PendingCompletionsByKey.Remove(("acme:OrdersProjection", 0)),
        };
        cut.Render(p => p.Add(c => c.ViewKey, "acme:OrdersProjection"));
        announcements.Current("projection:acme:OrdersProjection").ShouldBeEmpty();
        _time.Advance(TimeSpan.FromMilliseconds(999));
        announcements.Current("projection:acme:OrdersProjection").ShouldBeEmpty();
        _time.Advance(TimeSpan.FromMilliseconds(1));
        cut.WaitForAssertion(() => announcements.Current("projection:acme:OrdersProjection")
            .ShouldBe("This is taking longer than expected."));
    }

    [Fact]
    public void OffscreenPrefetchCannotTakeStatusWhileDisplayedPageIsReady()
    {
        const string view = "acme:OrdersProjection";
        TaskCompletionSource<object> offscreen = new();
        _stateValue = _stateValue with {
            PendingCompletionsByKey = _stateValue.PendingCompletionsByKey.SetItem((view, 40), offscreen),
        };
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        IRenderedComponent<FcSlowQueryNotice> cut = Render<FcSlowQueryNotice>(p => p
            .Add(c => c.ViewKey, view).Add(c => c.VisiblePageSkip, 20));
        _time.Advance(TimeSpan.FromSeconds(3));
        cut.FindAll("[data-testid='fc-slow-query-notice']").ShouldBeEmpty();
        announcements.Current("projection:" + view).ShouldBeEmpty();

        TaskCompletionSource<object> displayed = new();
        _stateValue = _stateValue with {
            PendingCompletionsByKey = _stateValue.PendingCompletionsByKey.SetItem((view, 20), displayed),
            PendingStartedAtByKey = _stateValue.PendingStartedAtByKey.SetItem((view, 20), _time.GetUtcNow()),
        };
        cut.Render(p => p.Add(c => c.ViewKey, view).Add(c => c.VisiblePageSkip, 20));
        _time.Advance(TimeSpan.FromMilliseconds(2_000));
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='fc-slow-query-notice']").Count.ShouldBe(1));
        announcements.Current("projection:" + view).ShouldBe("This is taking longer than expected.");
    }
    [Theory]
    [InlineData(1999)]
    [InlineData(2000)]
    public void ClearingTheViewCancelsItsTimerAndOriginalSurfaceSpeech(int elapsed)
    {
        const string view = "acme:OrdersProjection";
        _ = SetPending(view);
        IRenderedComponent<FcSlowQueryNotice> cut = Render<FcSlowQueryNotice>(p => p.Add(c => c.ViewKey, view));
        _time.Advance(TimeSpan.FromMilliseconds(elapsed));
        if (elapsed == 2000)
        {
            cut.WaitForAssertion(() => cut.FindAll("[data-testid='fc-slow-query-notice']").Count.ShouldBe(1));
        }
        cut.Render(p => p.Add(c => c.ViewKey, string.Empty));
        _time.Advance(TimeSpan.FromSeconds(5));
        cut.FindAll("[data-testid='fc-slow-query-notice']").ShouldBeEmpty();
        Services.GetRequiredService<ISurfaceAnnouncementCoordinator>().Current("projection:" + view).ShouldBeEmpty();
    }

    [Fact]
    public void QueuedSlowTimerAfterDisposalDoesNotPublish()
    {
        var time = new Hexalith.FrontComposer.Shell.Tests.Infrastructure.QueuedTimerTimeProvider();
        Services.AddSingleton<TimeProvider>(time);
        _ = SetPending("acme:OrdersProjection");
        IRenderedComponent<FcSlowQueryNotice> cut = Render<FcSlowQueryNotice>(p => p.Add(c => c.ViewKey, "acme:OrdersProjection"));
        cut.Instance.Dispose();
        foreach (Action callback in time.Callbacks)
        {
            Should.NotThrow(callback);
        }
        Services.GetRequiredService<ISurfaceAnnouncementCoordinator>().Current("projection:acme:OrdersProjection").ShouldBeEmpty();
    }
    [Fact]
    public void DeliveredRequestIdentitySurvivesRemountAndLaterRequestSpeaks()
    {
        const string view = "acme:OrdersProjection";
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable subscription = announcements.Subscribe("projection:" + view, spoken.Add);
        TaskCompletionSource<object> original = SetPending(view);
        _stateValue = _stateValue with { PendingStartedAtByKey = _stateValue.PendingStartedAtByKey.SetItem((view, 0), _time.GetUtcNow()) };
        IRenderedComponent<FcSlowQueryNotice> first = Render<FcSlowQueryNotice>(p => p.Add(c => c.ViewKey, view));
        _time.Advance(TimeSpan.FromSeconds(2));
        first.WaitForAssertion(() => spoken.Count(message => message.Length > 0).ShouldBe(1));
        first.Instance.Dispose();
        first.Dispose();
        IRenderedComponent<FcSlowQueryNotice> remounted = Render<FcSlowQueryNotice>(p => p.Add(c => c.ViewKey, view));
        _time.Advance(TimeSpan.FromSeconds(2));
        spoken.Count(message => message.Length > 0).ShouldBe(1);
        original.SetResult(new object());
        _ = SetPending(view);
        _stateValue = _stateValue with { PendingStartedAtByKey = _stateValue.PendingStartedAtByKey.SetItem((view, 0), _time.GetUtcNow()) };
        remounted.Render();
        _time.Advance(TimeSpan.FromSeconds(2));
        remounted.WaitForAssertion(() => spoken.Count(message => message.Length > 0).ShouldBe(2));
        remounted.Instance.Dispose();
        remounted.Dispose();
        announcements.Current("projection:" + view).ShouldBeEmpty();
    }


}
