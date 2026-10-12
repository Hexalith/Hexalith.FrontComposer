using Bunit;

using Hexalith.FrontComposer.Shell.Components.Rendering;
using Hexalith.FrontComposer.Shell.Services.Announcements;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

public sealed class FcSurfaceStatusTests : BunitContext {
    [Fact]
    public void WithdrawingTerminalRestoresOwnedStaleCopySilently()
    {
        SurfaceAnnouncementCoordinator coordinator = new(TimeProvider.System);
        Services.AddSingleton<ISurfaceAnnouncementCoordinator>(coordinator);
        IRenderedComponent<FcSurfaceStatus> cut = Render<FcSurfaceStatus>(p => p.Add(c => c.Surface, "projection:orders"));

        coordinator.Announce("projection:orders", "stale:episode:1", "stale", "Cached rows may be old.", immediate: true);
        coordinator.Announce("projection:orders", "blocked:attempt:1", "blocked", "This action is blocked.", terminal: true);
        cut.WaitForAssertion(() => cut.Find("[role='status']").TextContent.ShouldBe("This action is blocked."));

        coordinator.Cancel("projection:orders", "blocked:attempt:1");
        cut.WaitForAssertion(() => {
            cut.Find("[role='status']").TextContent.ShouldBe("Cached rows may be old.");
            cut.Find("[role='status']").GetAttribute("aria-live").ShouldBe("off");
        });
        coordinator.Announce("projection:orders", "stale:episode:2", "stale", "Another stale episode.", immediate: true);
        cut.WaitForAssertion(() => cut.Find("[role='status']").GetAttribute("aria-live").ShouldBe("polite"));
    }

    [Fact]
    public void ExclusiveOwnersShareOnePersistentNodeAndTransferTheCurrentMessage() {
        SurfaceAnnouncementCoordinator coordinator = new(TimeProvider.System);
        Services.AddSingleton<ISurfaceAnnouncementCoordinator>(coordinator);
        IRenderedComponent<FcSurfaceStatus> first = Render<FcSurfaceStatus>(p => p
            .Add(c => c.Surface, "projection:orders")
            .Add(c => c.ExclusiveOwner, true));
        IRenderedComponent<FcSurfaceStatus> second = Render<FcSurfaceStatus>(p => p
            .Add(c => c.Surface, "projection:orders")
            .Add(c => c.ExclusiveOwner, true));

        first.FindAll("[role='status']").Count.ShouldBe(1);
        second.FindAll("[role='status']").ShouldBeEmpty();
        coordinator.Announce("projection:orders", "load:1", "loaded", "Orders loaded.", terminal: true);
        first.WaitForAssertion(() => first.Find("[role='status']").TextContent.ShouldBe("Orders loaded."));

        first.Instance.Dispose();
        second.WaitForAssertion(() => second.Find("[role='status']").TextContent.ShouldBe("Orders loaded."));
    }

    [Fact]
    public void BrowserManagedStatusShowsServerCopyBeforeWatcherSetup() {
        SurfaceAnnouncementCoordinator coordinator = new(TimeProvider.System);
        Services.AddSingleton<ISurfaceAnnouncementCoordinator>(coordinator);
        IRenderedComponent<FcSurfaceStatus> cut = Render<FcSurfaceStatus>(p => p
            .Add(c => c.Surface, "projection-connection")
            .Add(c => c.BrowserManaged, true));

        coordinator.Announce("projection-connection", "fallback:1", "disconnected",
            "Live updates are unavailable. Checking for updates.", terminal: true);

        cut.WaitForAssertion(() => cut.Find("[data-fc-browser-status]").TextContent
            .ShouldBe("Live updates are unavailable. Checking for updates."));
        cut.FindAll("[role='status']").Count.ShouldBe(1);
    }

    [Fact]
    public void BrowserManagedStatusLeavesNativeTextUntouchedAfterWatcherAttaches()
    {
        SurfaceAnnouncementCoordinator coordinator = new(TimeProvider.System);
        Services.AddSingleton<ISurfaceAnnouncementCoordinator>(coordinator);
        IRenderedComponent<FcSurfaceStatus> cut = Render<FcSurfaceStatus>(p => p
            .Add(c => c.Surface, "projection-connection")
            .Add(c => c.BrowserManaged, true));
        coordinator.Announce("projection-connection", "fallback:1", "disconnected", "Checking for updates.", terminal: true);
        cut.WaitForAssertion(() => cut.Find("[data-fc-browser-status]").TextContent.ShouldBe("Checking for updates."));

        cut.Render(p => p
            .Add(c => c.Surface, "projection-connection")
            .Add(c => c.BrowserManaged, true)
            .Add(c => c.BrowserReady, true));
        coordinator.Announce("projection-connection", "fallback:2", "reconnecting", "Reconnecting.", terminal: true);
        cut.WaitForAssertion(() => cut.Find("[data-fc-status-message]").GetAttribute("data-fc-status-message")
            .ShouldBe("Reconnecting."));
        cut.Find("[data-fc-browser-status]").TextContent.ShouldBe("Checking for updates.");
    }

    [Fact]
    public async Task QueuedOldSurfaceCallbackCannotReplaceReboundStatus() {
        ISurfaceAnnouncementCoordinator coordinator = Substitute.For<ISurfaceAnnouncementCoordinator>();
        var oldHandlers = new List<Action<string, long>>();
        coordinator.SubscribeWithReplay("old", Arg.Do<Action<string, long>>(handler => oldHandlers.Add(handler)))
            .Returns(call => { call.Arg<Action<string, long>>()(string.Empty, 0); return Substitute.For<IDisposable>(); });
        coordinator.SubscribeWithReplay("new", Arg.Any<Action<string, long>>())
            .Returns(call => { call.Arg<Action<string, long>>()("New surface state", 1); return Substitute.For<IDisposable>(); });
        Services.AddSingleton(coordinator);

        IRenderedComponent<FcSurfaceStatus> cut = Render<FcSurfaceStatus>(p => p.Add(c => c.Surface, "old"));
        cut.Render(p => p.Add(c => c.Surface, "new"));
        cut.Find("[data-testid='fc-surface-status']").TextContent.ShouldBe("New surface state");

        await cut.InvokeAsync(() => oldHandlers[0]("Obsolete surface state", 2));
        cut.Find("[data-testid='fc-surface-status']").TextContent.ShouldBe("New surface state");

        cut.Render(p => p.Add(c => c.Surface, "old"));
        cut.Find("[data-testid='fc-surface-status']").TextContent.ShouldBe(string.Empty);
        await cut.InvokeAsync(() => oldHandlers[0]("Obsolete first binding state", 3));
        cut.Find("[data-testid='fc-surface-status']").TextContent.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task OlderDeliveryCannotReplaceNewerTerminalRevision() {
        ISurfaceAnnouncementCoordinator coordinator = Substitute.For<ISurfaceAnnouncementCoordinator>();
        Action<string, long>? deliver = null;
        coordinator.SubscribeWithReplay("command", Arg.Any<Action<string, long>>())
            .Returns(call => {
                deliver = call.Arg<Action<string, long>>();
                deliver(string.Empty, 0);
                return Substitute.For<IDisposable>();
            });
        Services.AddSingleton(coordinator);
        IRenderedComponent<FcSurfaceStatus> cut = Render<FcSurfaceStatus>(p => p.Add(c => c.Surface, "command"));

        await cut.InvokeAsync(() => deliver!("Command confirmed.", 3));
        await cut.InvokeAsync(() => deliver!("Command accepted.", 2));

        cut.Find("[data-testid='fc-surface-status']").TextContent.ShouldBe("Command confirmed.");
    }
}
