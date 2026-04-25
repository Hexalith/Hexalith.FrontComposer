using Bunit;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Components.EventStore;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.EventStore;

public sealed class FcProjectionConnectionStatusTests : BunitContext {
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
    private readonly ProjectionConnectionStateService _state;

    public FcProjectionConnectionStatusTests() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddFluentUIComponents();
        Services.AddSingleton<TimeProvider>(_time);
        _state = ActivatorUtilities.CreateInstance<ProjectionConnectionStateService>(Services.BuildServiceProvider());
        Services.AddSingleton<IProjectionConnectionState>(_state);
        IOptionsMonitor<FcShellOptions> options = Substitute.For<IOptionsMonitor<FcShellOptions>>();
        options.CurrentValue.Returns(new FcShellOptions { ProjectionReconnectedNoticeDurationMs = 1_000 });
        Services.AddSingleton(options);
    }

    [Fact]
    public void Disconnected_RendersInlinePoliteStatusCopy() {
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();

        _state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Reconnecting, "TimeoutException"));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("Connection interrupted -- your work is saved. Reconnecting...");
            cut.Find("[data-testid='fc-projection-connection-status']").GetAttribute("role").ShouldBe("status");
            cut.Find("[data-testid='fc-projection-connection-status']").GetAttribute("aria-live").ShouldBe("polite");
            cut.FindAll("[role='dialog']").ShouldBeEmpty();
        });
    }

    [Fact]
    public void Reconnected_RendersBriefConfirmation_ThenAutoClears() {
        IRenderedComponent<FcProjectionConnectionStatus> cut = Render<FcProjectionConnectionStatus>();
        _state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected, "Closed"));
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Connection interrupted"));

        _state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Connected));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Reconnected -- you can continue"));
        _time.Advance(TimeSpan.FromMilliseconds(1_100));
        cut.WaitForAssertion(() => cut.Markup.ShouldNotContain("Reconnected -- you can continue"));
    }
}
