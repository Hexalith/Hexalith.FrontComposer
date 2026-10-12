using System.Reflection;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Shell.Services.Announcements;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

public sealed class GeneratedNonGridAnnouncementDisposalTests : GeneratedComponentTestBase
{
    [Fact]
    public void ScopeGenerationRejectsDelayedFirstScopeActionsAfterReturningToSameUser()
    {
        NoticeDetailProjectionState initial = new(false, null, null);
        NoticeDetailProjectionLoadRequestedAction firstRequest = new("first-a", true) {
            ScopeTenantId = "tenant-a", ScopeUserId = "user-a", OriginScopeGeneration = initial.ScopeGeneration,
        };
        NoticeDetailProjectionState first = NoticeDetailProjectionReducers.OnNoticeDetailProjectionLoadRequested(initial, firstRequest);
        NoticeDetailProjectionState secondScope = NoticeDetailProjectionReducers.OnNoticeDetailProjectionScopeChanged(first, new ScopeChangedAction());
        NoticeDetailProjectionState thirdScope = NoticeDetailProjectionReducers.OnNoticeDetailProjectionScopeChanged(secondScope, new ScopeChangedAction());
        thirdScope.ScopeGeneration.ShouldBe(2);
        NoticeDetailProjectionReducers.OnNoticeDetailProjectionLoadRequested(thirdScope, firstRequest).ShouldBeSameAs(thirdScope);
        NoticeDetailProjectionReducers.OnNoticeDetailProjectionLoaded(thirdScope,
            new NoticeDetailProjectionLoadedAction("first-a", [new NoticeDetailProjection { Id = "old-a" }]) {
                ScopeTenantId = "tenant-a", ScopeUserId = "user-a", OriginScopeGeneration = 0,
            }).ShouldBeSameAs(thirdScope);
        NoticeDetailProjectionReducers.OnNoticeDetailProjectionLoadFailed(thirdScope,
            new NoticeDetailProjectionLoadFailedAction("first-a", "old failure") { OriginScopeGeneration = 0 })
            .ShouldBeSameAs(thirdScope);
        NoticeDetailProjectionReducers.OnNoticeDetailProjectionLoadRequested(thirdScope,
            new NoticeDetailProjectionLoadRequestedAction("unstamped-old")).ShouldBeSameAs(thirdScope);

        NoticeDetailProjectionState current = NoticeDetailProjectionReducers.OnNoticeDetailProjectionLoadRequested(thirdScope,
            new NoticeDetailProjectionLoadRequestedAction("second-a", true) {
                ScopeTenantId = "tenant-a", ScopeUserId = "user-a", OriginScopeGeneration = thirdScope.ScopeGeneration,
            });
        current.RequiresScopedRequest.ShouldBeFalse();
        current = NoticeDetailProjectionReducers.OnNoticeDetailProjectionLoaded(current,
            new NoticeDetailProjectionLoadedAction("second-a", [new NoticeDetailProjection { Id = "current-a" }]) {
                ScopeTenantId = "tenant-a", ScopeUserId = "user-a", OriginScopeGeneration = thirdScope.ScopeGeneration,
            });
        current.Items.ShouldNotBeNull().Single().Id.ShouldBe("current-a");
    }
    [Theory]
    [InlineData("tenant-secret backend exception", "Data could not be loaded.")]
    [InlineData("This section is being updated", "This section is being updated")]
    public async Task NonGridFailureCardAndSpeechUseTheSameSafeCopy(string raw, string expected)
    {
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        IRenderedComponent<NoticeDetailProjectionView> cut = Render<NoticeDetailProjectionView>();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        await cut.InvokeAsync(() => dispatcher.Dispatch(new NoticeDetailProjectionLoadRequestedAction("failure", true)));
        await cut.InvokeAsync(() => dispatcher.Dispatch(new NoticeDetailProjectionLoadFailedAction("failure", raw)));
        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='fc-projection-failure']").TextContent.ShouldContain(expected);
            cut.Find("[data-testid='fc-projection-failure']").TextContent.ShouldContain("Retry");
            cut.Find("[data-testid='fc-surface-status']").TextContent.ShouldBe(expected);
            cut.Markup.ShouldNotContain("tenant-secret");
        });
    }

    public GeneratedNonGridAnnouncementDisposalTests() : base(typeof(NoticeDetailProjection).Assembly) { }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisposedNonGridWithdrawsTrackedSpeechWithoutCancellingAnotherOwner(bool failure)
    {
        FakeTimeProvider time = new();
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(time));
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        IRenderedComponent<NoticeDetailProjectionView> cut = Render<NoticeDetailProjectionView>();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        await cut.InvokeAsync(() => dispatcher.Dispatch(new NoticeDetailProjectionLoadRequestedAction("abandoned", true)));
        if (failure)
        {
            await cut.InvokeAsync(() => dispatcher.Dispatch(new NoticeDetailProjectionLoadFailedAction("abandoned", "This section is being updated")));
        }
        string surface = (string)typeof(NoticeDetailProjectionView).GetProperty("AnnouncementSurface", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        cut.Instance.Dispose();
        time.Advance(TimeSpan.FromMilliseconds(250));
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        announcements.Current(surface).ShouldBeEmpty();
        typeof(NoticeDetailProjectionView).GetMethod("AnnounceProjectionState", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(cut.Instance, null);
        time.Advance(TimeSpan.FromMilliseconds(250));
        announcements.Current(surface).ShouldBeEmpty();
        IRenderedComponent<Hexalith.FrontComposer.Shell.Components.Rendering.FcSurfaceStatus> remountedStatus =
            Render<Hexalith.FrontComposer.Shell.Components.Rendering.FcSurfaceStatus>(p => p.Add(c => c.Surface, surface));
        remountedStatus.Find("[data-testid='fc-surface-status']").TextContent.ShouldBeEmpty();
        announcements.Announce(surface, "other-owner", "result", "Other owner remains", immediate: true);
        cut.Instance.Dispose();
        announcements.Current(surface).ShouldBe("Other owner remains");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NonGridDisposalWithdrawsQueuedLoadedAndDeliveredEmptyResults(bool empty)
    {
        FakeTimeProvider time = new();
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(time));
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        IRenderedComponent<NoticeDetailProjectionView> cut = Render<NoticeDetailProjectionView>();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        await cut.InvokeAsync(() => dispatcher.Dispatch(new NoticeDetailProjectionLoadRequestedAction("owned-result", true)));
        await cut.InvokeAsync(() => dispatcher.Dispatch(new NoticeDetailProjectionLoadedAction("owned-result",
            empty ? [] : [new NoticeDetailProjection { Id = "cached-row", Name = "Cached row" }])));
        string surface = (string)typeof(NoticeDetailProjectionView).GetProperty("AnnouncementSurface", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        if (empty) { announcements.Current(surface).ShouldNotBeEmpty(); }
        else {
            time.Advance(TimeSpan.FromMilliseconds(250));
            cut.WaitForAssertion(() => announcements.Current(surface).ShouldContain("loaded.", Case.Insensitive));
            cut.Find("[data-testid='fc-surface-status']").TextContent.ShouldContain("loaded.", Case.Insensitive);
        }
        cut.Instance.Dispose();
        time.Advance(TimeSpan.FromMilliseconds(250));
        announcements.Current(surface).ShouldBeEmpty();
        IRenderedComponent<Hexalith.FrontComposer.Shell.Components.Rendering.FcSurfaceStatus> replacement =
            Render<Hexalith.FrontComposer.Shell.Components.Rendering.FcSurfaceStatus>(p => p.Add(c => c.Surface, surface));
        replacement.Find("[data-testid='fc-surface-status']").TextContent.ShouldBeEmpty();
    }

    [Fact]
    public async Task WithdrawingDeliveredEmptyFeedbackPreservesAnotherOwnersPendingSpeech()
    {
        FakeTimeProvider time = new();
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(time));
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        IRenderedComponent<NoticeDetailProjectionView> cut = Render<NoticeDetailProjectionView>();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        await cut.InvokeAsync(() => dispatcher.Dispatch(new NoticeDetailProjectionLoadRequestedAction("empty-owner", true)));
        await cut.InvokeAsync(() => dispatcher.Dispatch(new NoticeDetailProjectionLoadedAction("empty-owner", [])));
        string surface = (string)typeof(NoticeDetailProjectionView).GetProperty("AnnouncementSurface", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        announcements.Current(surface).ShouldNotBeEmpty();
        announcements.Announce(surface, "other-owner", "current", "Other owner remains");
        cut.Instance.Dispose();
        announcements.Current(surface).ShouldBeEmpty();
        time.Advance(TimeSpan.FromMilliseconds(250));
        announcements.Current(surface).ShouldBe("Other owner remains");
    }
}
