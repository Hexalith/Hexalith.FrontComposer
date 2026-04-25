using Bunit;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Components.Lifecycle;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Lifecycle;

public sealed class FcLifecycleWrapperDisconnectedTests : LifecycleWrapperTestBase {
    [Fact]
    public void Syncing_DisconnectedProjectionConnection_EscalatesImmediatelyWithExactCopy() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, _) = RenderWrapperWithFakeTime();
        IProjectionConnectionState state = Services.GetRequiredService<IProjectionConnectionState>();

        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, FakeTime.GetUtcNow()));
        state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected, "Closed"));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("Connection lost -- unable to confirm sync status");
            cut.Markup.ShouldNotContain("fc-lifecycle-pulse");
        });
    }

    [Fact]
    public void TerminalConfirmed_IgnoresLaterProjectionConnectionLoss() {
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, _) = RenderWrapperWithFakeTime();
        IProjectionConnectionState state = Services.GetRequiredService<IProjectionConnectionState>();

        push(TransitionAt(CommandLifecycleState.Syncing, CommandLifecycleState.Confirmed, FakeTime.GetUtcNow()));
        state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected, "Closed"));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("Submission confirmed");
            cut.Markup.ShouldNotContain("Connection lost -- unable to confirm sync status");
        });
    }

    [Fact]
    public void DisconnectedSyncing_DoesNotAutoConfirm_OnReconnect() {
        // P16 / T6: a Syncing lifecycle escalated to ActionPrompt by disconnect must NOT
        // transition to Confirmed when the projection hub reconnects. Reconnection alone is
        // not evidence the command succeeded — Story 5-5 owns idempotent terminal reconciliation.
        (IRenderedComponent<FcLifecycleWrapper> cut, Action<CommandLifecycleTransition> push, _) = RenderWrapperWithFakeTime();
        IProjectionConnectionState state = Services.GetRequiredService<IProjectionConnectionState>();

        push(TransitionAt(CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, FakeTime.GetUtcNow()));
        state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected, "Closed"));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Connection lost -- unable to confirm sync status"));

        state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Connected));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldNotContain("Submission confirmed");
        });
    }
}
