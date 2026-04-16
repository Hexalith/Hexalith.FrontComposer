using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Services.Lifecycle;

using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Lifecycle;

/// <summary>
/// Story 2-3 Task 4.9 + 4.10 + 4.12 — behavioural + duplicate-detection + subscription-contract tests for
/// <see cref="LifecycleStateService"/>.
/// </summary>
public class LifecycleStateServiceTests {
    private static LifecycleStateService Create(int cacheCap = 1024) {
        return new LifecycleStateService(
            Options.Create(new LifecycleOptions { MessageIdCacheCapacity = cacheCap }));
    }

    // ----- Task 4.9 — Behavioural (6 tests) ---------------------------------

    [Fact]
    public void Transition_SubmittingAckSyncingConfirmed_EmitsTransitionsInOrder() {
        using LifecycleStateService service = Create();
        List<CommandLifecycleTransition> captured = [];
        using IDisposable _ = service.Subscribe("c1", captured.Add);

        service.Transition("c1", CommandLifecycleState.Submitting);
        service.Transition("c1", CommandLifecycleState.Acknowledged, "MSGID1");
        service.Transition("c1", CommandLifecycleState.Syncing);
        service.Transition("c1", CommandLifecycleState.Confirmed);

        captured.Select(t => t.NewState).ShouldBe([
            CommandLifecycleState.Submitting,
            CommandLifecycleState.Acknowledged,
            CommandLifecycleState.Syncing,
            CommandLifecycleState.Confirmed,
        ]);
    }

    [Fact]
    public void Subscribe_AfterTerminal_ReplaysTerminalOnceOnSubscribe() {
        using LifecycleStateService service = Create();
        service.Transition("c2", CommandLifecycleState.Submitting);
        service.Transition("c2", CommandLifecycleState.Acknowledged, "M");
        service.Transition("c2", CommandLifecycleState.Confirmed);

        List<CommandLifecycleTransition> captured = [];
        using IDisposable _ = service.Subscribe("c2", captured.Add);

        captured.Count.ShouldBe(1);
        captured[0].NewState.ShouldBe(CommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Transition_InvalidStateMachineEdge_DropsAndDoesNotNotify() {
        using LifecycleStateService service = Create();
        List<CommandLifecycleTransition> captured = [];
        using IDisposable _ = service.Subscribe("c3", captured.Add);

        // Idle -> Confirmed is invalid.
        service.Transition("c3", CommandLifecycleState.Confirmed);

        captured.ShouldBeEmpty();
        service.GetState("c3").ShouldBe(CommandLifecycleState.Idle);
    }

    [Fact]
    public void GetState_UnknownCorrelationId_ReturnsIdle() {
        using LifecycleStateService service = Create();

        service.GetState("missing").ShouldBe(CommandLifecycleState.Idle);
    }

    [Fact]
    public void Dispose_WhileSubscriberActive_DoesNotThrow_ClearsAllState() {
        LifecycleStateService service = Create();
        service.Transition("c4", CommandLifecycleState.Submitting);
        using IDisposable _ = service.Subscribe("c4", _ => { });

        Should.NotThrow(() => service.Dispose());

        service.GetActiveCorrelationIds().ShouldBeEmpty();
    }

    [Fact]
    public void GetActiveCorrelationIds_ReturnsSnapshot_LiveAfterTransitions() {
        using LifecycleStateService service = Create();
        service.Transition("a", CommandLifecycleState.Submitting);
        service.Transition("b", CommandLifecycleState.Submitting);
        service.Transition("c", CommandLifecycleState.Submitting);

        IDisposable sub = service.Subscribe("a", _ => { });
        sub.Dispose();

        IEnumerable<string> ids = service.GetActiveCorrelationIds();
        ids.ShouldBe(["a", "b", "c"], ignoreOrder: true);
    }

    // ----- Task 4.10 — Duplicate detection (4 tests) ------------------------

    [Fact]
    public void Transition_SameMessageIdSameCorrelation_NoReEmit() {
        using LifecycleStateService service = Create();
        List<CommandLifecycleTransition> captured = [];
        using IDisposable _ = service.Subscribe("c5", captured.Add);

        service.Transition("c5", CommandLifecycleState.Submitting);
        service.Transition("c5", CommandLifecycleState.Acknowledged, "M");
        service.Transition("c5", CommandLifecycleState.Confirmed, "M");

        // Replay (same CorrelationId + same MessageId): dropped because Confirmed→Confirmed is invalid.
        service.Transition("c5", CommandLifecycleState.Confirmed, "M");

        captured.Count(t => t.NewState == CommandLifecycleState.Confirmed).ShouldBe(1);
    }

    [Fact]
    public void Transition_SameMessageIdNewCorrelation_TreatsAsFreshEntry() {
        using LifecycleStateService service = Create();
        service.Transition("c6", CommandLifecycleState.Submitting);
        service.Transition("c6", CommandLifecycleState.Acknowledged, "MSG");
        service.Transition("c6", CommandLifecycleState.Confirmed);

        // New correlation reusing the same MessageId is treated as fresh.
        List<CommandLifecycleTransition> captured = [];
        using IDisposable _ = service.Subscribe("c7", captured.Add);
        service.Transition("c7", CommandLifecycleState.Submitting);
        service.Transition("c7", CommandLifecycleState.Acknowledged, "MSG");

        captured.Any(t => t.NewState == CommandLifecycleState.Submitting).ShouldBeTrue();
        captured.Any(t => t.NewState == CommandLifecycleState.Acknowledged && t.MessageId == "MSG").ShouldBeTrue();
    }

    [Fact]
    public void Transition_DuplicateMessageIdAfterTerminal_IsTreatedAsFreshOnNewCorrelation() {
        using LifecycleStateService service = Create();
        service.Transition("c8", CommandLifecycleState.Submitting);
        service.Transition("c8", CommandLifecycleState.Acknowledged, "DUPE");
        service.Transition("c8", CommandLifecycleState.Confirmed);

        // CorrelationId c8 was terminal. A new CorrelationId with the same MessageId is fresh.
        service.Transition("c9", CommandLifecycleState.Submitting);
        service.Transition("c9", CommandLifecycleState.Acknowledged, "DUPE");

        service.GetState("c9").ShouldBe(CommandLifecycleState.Acknowledged);
        service.GetMessageId("c9").ShouldBe("DUPE");
    }

    [Fact]
    public void Transition_MessageIdCacheCap_DeterministicLruBoundary() {
        const int cap = 8;
        using LifecycleStateService service = Create(cacheCap: cap);
        string[] messageIds = Enumerable.Range(0, cap + 1).Select(i => $"M{i}").ToArray();

        foreach (string id in messageIds) {
            string correlationId = "corr-" + id;
            service.Transition(correlationId, CommandLifecycleState.Submitting);
            service.Transition(correlationId, CommandLifecycleState.Acknowledged, id);
        }

        // Oldest "M0" was evicted when the (cap+1)'th was inserted. A fresh correlation that reuses "M0"
        // is NOT treated as a duplicate — the service no longer remembers it.
        using LifecycleStateService fresh = Create(cacheCap: cap);
        List<CommandLifecycleTransition> captured = [];
        using IDisposable _ = fresh.Subscribe("new", captured.Add);
        fresh.Transition("new", CommandLifecycleState.Submitting);
        fresh.Transition("new", CommandLifecycleState.Acknowledged, "M0");

        captured.Any(t => t.MessageId == "M0").ShouldBeTrue();
    }

    // ----- Task 4.12 — Subscription contract (4 tests) ----------------------

    [Fact]
    public void Subscribe_AfterTransition_ReplaysCurrentStateImmediately_Once() {
        using LifecycleStateService service = Create();
        service.Transition("c10", CommandLifecycleState.Submitting);

        List<CommandLifecycleTransition> captured = [];
        using IDisposable _ = service.Subscribe("c10", captured.Add);

        captured.Count.ShouldBe(1);
        captured[0].NewState.ShouldBe(CommandLifecycleState.Submitting);
    }

    [Fact]
    public void MultipleSubscribersSameCorrelation_AllReceiveTransitions_SetEquality() {
        using LifecycleStateService service = Create();
        List<CommandLifecycleState> a = [];
        List<CommandLifecycleState> b = [];
        using IDisposable _1 = service.Subscribe("c11", t => a.Add(t.NewState));
        using IDisposable _2 = service.Subscribe("c11", t => b.Add(t.NewState));

        service.Transition("c11", CommandLifecycleState.Submitting);
        service.Transition("c11", CommandLifecycleState.Acknowledged, "M");
        service.Transition("c11", CommandLifecycleState.Confirmed);

        a.ToHashSet().ShouldBe(b.ToHashSet());
    }

    [Fact]
    public void Unsubscribe_StopsReceivingTransitions() {
        using LifecycleStateService service = Create();
        List<CommandLifecycleTransition> captured = [];
        IDisposable sub = service.Subscribe("c12", captured.Add);

        service.Transition("c12", CommandLifecycleState.Submitting);
        sub.Dispose();
        service.Transition("c12", CommandLifecycleState.Acknowledged, "M");

        captured.Count.ShouldBe(1);
        captured[0].NewState.ShouldBe(CommandLifecycleState.Submitting);
    }

    [Fact]
    public void MultipleSubscribers_EachReceiveTerminalExactlyOnce() {
        using LifecycleStateService service = Create();
        int aCount = 0, bCount = 0, cCount = 0;
        using IDisposable s1 = service.Subscribe("c13", t => { if (t.NewState == CommandLifecycleState.Confirmed) aCount++; });
        using IDisposable s2 = service.Subscribe("c13", t => { if (t.NewState == CommandLifecycleState.Confirmed) bCount++; });
        using IDisposable s3 = service.Subscribe("c13", t => { if (t.NewState == CommandLifecycleState.Confirmed) cCount++; });

        service.Transition("c13", CommandLifecycleState.Submitting);
        service.Transition("c13", CommandLifecycleState.Acknowledged, "M");
        service.Transition("c13", CommandLifecycleState.Confirmed);

        aCount.ShouldBe(1);
        bCount.ShouldBe(1);
        cCount.ShouldBe(1);
    }
}
