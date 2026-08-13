using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

/// <summary>
/// Story 2-3 Task 5.4 + 5.5 — bridge emitter tests (structure + determinism + snapshot).
/// </summary>
public class LifecycleBridgeEmitterTests {
    private static CommandFluxorModel BuildFluxor(
        string typeName = "IncrementCommand",
        string @namespace = "Counter.Domain") =>
        new(
            typeName,
            @namespace,
            typeName + "LifecycleState",
            typeName + "LifecycleFeature",
            typeName + "Actions",
            typeName + "Reducers",
            @namespace + "." + typeName,
            @namespace + "." + typeName + "LifecycleState");

    [Fact]
    public void Emit_CommandWithStandardActions_ProducesBridgeClassWithSixSubscriptions() {
        string source = CommandLifecycleBridgeEmitter.Emit(BuildFluxor());

        source.ShouldContain("public sealed class IncrementCommandLifecycleBridge : global::System.IDisposable");
        source.ShouldContain(".SubscribeToAction<IncrementCommandActions.SubmittedAction>");
        source.ShouldContain(".SubscribeToAction<IncrementCommandActions.AcknowledgedAction>");
        source.ShouldContain(".SubscribeToAction<IncrementCommandActions.SyncingAction>");
        source.ShouldContain(".SubscribeToAction<IncrementCommandActions.ConfirmedAction>");
        source.ShouldContain(".SubscribeToAction<IncrementCommandActions.RejectedAction>");
        source.ShouldContain(".SubscribeToAction<IncrementCommandActions.ResetToIdleAction>");
    }

    [Fact]
    public void Emit_RejectedAction_ForwardsExactlyOneRejectedTransitionToLifecycleService() {
        string source = CommandLifecycleBridgeEmitter.Emit(BuildFluxor());

        source.ShouldContain("_subscriber.SubscribeToAction<IncrementCommandActions.RejectedAction>(this,");
        source.ShouldContain("a => ForwardAction(a.CorrelationId, global::Hexalith.FrontComposer.Contracts.Lifecycle.CommandLifecycleState.Rejected, null));");
        source.ShouldContain("_dispatcher.Dispatch(new IncrementCommandActions.RejectedAction(transition.CorrelationId");
    }

    [Fact]
    public void Emit_ResolverLifecycleTransitionsDispatchTerminalFluxorActionsWithoutFeedbackLoops() {
        string source = CommandLifecycleBridgeEmitter.Emit(BuildFluxor());

        source.ShouldContain("_service.Subscribe(correlationId, OnLifecycleTransition)");
        source.ShouldContain("_forwardingActions.ContainsKey((transition.CorrelationId, transition.NewState))");
        source.ShouldContain("Dictionary<(string CorrelationId, global::Hexalith.FrontComposer.Contracts.Lifecycle.CommandLifecycleState State), int>");
        source.ShouldContain("_dispatcher.Dispatch(new IncrementCommandActions.ConfirmedAction(transition.CorrelationId))");
        source.ShouldContain("_dispatcher.Dispatch(new IncrementCommandActions.RejectedAction(transition.CorrelationId");
        source.ShouldContain("_lifecycleSubscriptions.Remove(transition.CorrelationId, out terminalSubscription)");
        source.ShouldContain("terminalSubscription?.Dispose();");
        source.ShouldContain("RemoveLifecycleSubscriptionsExcept(action.CorrelationId)");
        source.ShouldContain("|| !_lifecycleSubscriptions.ContainsKey(transition.CorrelationId)) return;");
        source.ShouldContain("subscription?.Dispose();");
    }

    [Fact]
    public void Emit_CommandNamespace_MatchesCommand() {
        string source = CommandLifecycleBridgeEmitter.Emit(BuildFluxor("ConfigureCounterCommand", "Counter.Domain"));

        source.ShouldContain("namespace Counter.Domain;");
        source.ShouldContain("public sealed class ConfigureCounterCommandLifecycleBridge");
    }

    [Fact]
    public void Emit_DeterministicOutput_RunningTwiceProducesIdenticalSource() {
        CommandFluxorModel model = BuildFluxor();

        string first = CommandLifecycleBridgeEmitter.Emit(model);
        string second = CommandLifecycleBridgeEmitter.Emit(model);

        second.ShouldBe(first);
    }

    [Fact]
    public Task Emit_IncrementCommand() => Verify(CommandLifecycleBridgeEmitter.Emit(BuildFluxor()));

    [Fact]
    public Task Emit_ConfigureCounterCommand() =>
        Verify(CommandLifecycleBridgeEmitter.Emit(BuildFluxor("ConfigureCounterCommand", "Counter.Domain")));

    [Fact]
    public Task Emit_NestedNamespace() =>
        Verify(CommandLifecycleBridgeEmitter.Emit(BuildFluxor(
            "BulkIncrementCommand",
            "Counter.Domain.Batch.Operations")));
}
