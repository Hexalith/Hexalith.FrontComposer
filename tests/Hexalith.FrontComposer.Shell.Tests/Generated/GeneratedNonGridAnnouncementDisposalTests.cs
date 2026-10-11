using System.Reflection;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Shell.Services.Announcements;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

public sealed class GeneratedNonGridAnnouncementDisposalTests : GeneratedComponentTestBase
{
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
