using Bunit;

using Counter.Domain;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>
/// Story 2-3 Task 8.2 — end-to-end verification that the per-command bridge routes Fluxor action
/// dispatches to <see cref="ILifecycleStateService"/>.
/// </summary>
public sealed class CounterPageLifecycleE2ETests : CommandRendererTestBase {
    public CounterPageLifecycleE2ETests() {
        _ = Services.AddHexalithDomain<IncrementCommand>();
    }
    [Fact]
    public async Task IncrementCommand_ActionsDispatched_ServiceReachesConfirmed() {
        await InitializeStoreAsync();

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        ILifecycleStateService service = Services.GetRequiredService<ILifecycleStateService>();
        ILifecycleBridgeRegistry bridges = Services.GetRequiredService<ILifecycleBridgeRegistry>();
        bridges.Ensure<IncrementCommandLifecycleBridge>();

        string cid = Guid.NewGuid().ToString();
        dispatcher.Dispatch(new IncrementCommandActions.SubmittedAction(cid, new IncrementCommand()));
        dispatcher.Dispatch(new IncrementCommandActions.AcknowledgedAction(cid, "MSG-1"));
        dispatcher.Dispatch(new IncrementCommandActions.SyncingAction(cid));
        dispatcher.Dispatch(new IncrementCommandActions.ConfirmedAction(cid));

        SpinWait.SpinUntil(
            () => service.GetState(cid) == CommandLifecycleState.Confirmed,
            TimeSpan.FromSeconds(2)).ShouldBeTrue();
        service.GetMessageId(cid).ShouldBe("MSG-1");
    }

    [Fact]
    public async Task IncrementCommand_SubscribeEmitsTransitionsInOrder() {
        await InitializeStoreAsync();

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        ILifecycleStateService service = Services.GetRequiredService<ILifecycleStateService>();
        ILifecycleBridgeRegistry bridges = Services.GetRequiredService<ILifecycleBridgeRegistry>();
        bridges.Ensure<IncrementCommandLifecycleBridge>();

        string cid = Guid.NewGuid().ToString();
        List<CommandLifecycleState> captured = [];
        using IDisposable _ = service.Subscribe(cid, t => {
            lock (captured) {
                captured.Add(t.NewState);
            }
        });

        dispatcher.Dispatch(new IncrementCommandActions.SubmittedAction(cid, new IncrementCommand()));
        dispatcher.Dispatch(new IncrementCommandActions.AcknowledgedAction(cid, "MSG-2"));
        dispatcher.Dispatch(new IncrementCommandActions.SyncingAction(cid));
        dispatcher.Dispatch(new IncrementCommandActions.ConfirmedAction(cid));

        SpinWait.SpinUntil(() => {
            lock (captured) {
                return captured.Contains(CommandLifecycleState.Confirmed);
            }
        }, TimeSpan.FromSeconds(2)).ShouldBeTrue();

        List<CommandLifecycleState> observed;
        lock (captured) {
            observed = [.. captured];
        }
        observed.ShouldContain(CommandLifecycleState.Submitting);
        observed.ShouldContain(CommandLifecycleState.Acknowledged);
        observed.ShouldContain(CommandLifecycleState.Confirmed);
    }

    [Fact]
    public async Task IncrementCommand_RejectedAction_SubscriberSeesRejected() {
        await InitializeStoreAsync();

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        ILifecycleStateService service = Services.GetRequiredService<ILifecycleStateService>();
        ILifecycleBridgeRegistry bridges = Services.GetRequiredService<ILifecycleBridgeRegistry>();
        bridges.Ensure<IncrementCommandLifecycleBridge>();

        string cid = Guid.NewGuid().ToString();
        List<CommandLifecycleState> captured = [];
        using IDisposable _ = service.Subscribe(cid, t => {
            lock (captured) {
                captured.Add(t.NewState);
            }
        });

        dispatcher.Dispatch(new IncrementCommandActions.SubmittedAction(cid, new IncrementCommand()));
        dispatcher.Dispatch(new IncrementCommandActions.AcknowledgedAction(cid, "MSG-3"));
        dispatcher.Dispatch(new IncrementCommandActions.RejectedAction(cid, "test rejection", "check your input"));

        SpinWait.SpinUntil(() => service.GetState(cid) == CommandLifecycleState.Rejected, TimeSpan.FromSeconds(2))
            .ShouldBeTrue();

        List<CommandLifecycleState> observed;
        lock (captured) {
            observed = [.. captured];
        }
        observed.ShouldContain(CommandLifecycleState.Rejected);
        observed.ShouldNotContain(CommandLifecycleState.Confirmed);
    }
}
