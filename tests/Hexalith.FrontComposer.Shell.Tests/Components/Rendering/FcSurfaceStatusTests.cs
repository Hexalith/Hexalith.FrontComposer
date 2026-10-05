using Bunit;

using Hexalith.FrontComposer.Shell.Components.Rendering;
using Hexalith.FrontComposer.Shell.Services.Announcements;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

public sealed class FcSurfaceStatusTests : BunitContext {
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
