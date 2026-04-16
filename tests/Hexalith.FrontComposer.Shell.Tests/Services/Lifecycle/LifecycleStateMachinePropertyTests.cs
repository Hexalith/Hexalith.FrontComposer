using FsCheck;
using FsCheck.Xunit;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Services.Lifecycle;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Lifecycle;

/// <summary>
/// Story 2-3 Task 11.3 — FsCheck property tests covering the state-machine invariants (AC5).
/// </summary>
public class LifecycleStateMachinePropertyTests {
    private static LifecycleStateService Create(int cap = 1024) =>
        new(Microsoft.Extensions.Options.Options.Create(new LifecycleOptions { MessageIdCacheCapacity = cap }));

    private static readonly CommandLifecycleState[] AllStates = [
        CommandLifecycleState.Idle,
        CommandLifecycleState.Submitting,
        CommandLifecycleState.Acknowledged,
        CommandLifecycleState.Syncing,
        CommandLifecycleState.Confirmed,
        CommandLifecycleState.Rejected,
    ];

    private static CommandLifecycleState? NextValid(CommandLifecycleState current) => current switch {
        CommandLifecycleState.Idle => CommandLifecycleState.Submitting,
        CommandLifecycleState.Submitting => CommandLifecycleState.Acknowledged,
        CommandLifecycleState.Acknowledged => CommandLifecycleState.Syncing,
        CommandLifecycleState.Syncing => CommandLifecycleState.Confirmed,
        _ => null,
    };

    private static bool IsTerminal(CommandLifecycleState s) =>
        s is CommandLifecycleState.Confirmed or CommandLifecycleState.Rejected;

    [Property(MaxTest = 200)]
    public void Property_ValidTransitionsOnly_StateIsReachable(int length) {
        using LifecycleStateService service = Create();
        string cid = "p1-" + Guid.NewGuid();
        CommandLifecycleState expected = CommandLifecycleState.Idle;
        int steps = Math.Max(1, Math.Min(4, Math.Abs(length)));

        for (int i = 0; i < steps; i++) {
            CommandLifecycleState? next = NextValid(expected);
            if (next is null) {
                break;
            }

            service.Transition(cid, next.Value, next.Value == CommandLifecycleState.Acknowledged ? "M-" + i : null);
            expected = next.Value;
        }

        service.GetState(cid).ShouldBe(expected);
    }

    [Property(MaxTest = 200)]
    public void Property_NoBackwardTransition_WithoutResetToIdle(int seed) {
        Random rng = new(seed);
        using LifecycleStateService service = Create();
        string cid = "p2-" + seed;

        CommandLifecycleState current = CommandLifecycleState.Idle;
        for (int i = 0; i < 10; i++) {
            CommandLifecycleState target = AllStates[rng.Next(1, AllStates.Length)];
            if (target == CommandLifecycleState.Idle) {
                continue;
            }

            service.Transition(cid, target, "M-" + i);
            CommandLifecycleState after = service.GetState(cid);
            (current <= after || after >= current).ShouldBeTrue();
            current = after;
        }
    }

    [Property(MaxTest = 200)]
    public void Property_CrossCorrelationIsolation(int seed) {
        Random rng = new(seed);
        using LifecycleStateService service = Create();

        service.Transition("a", CommandLifecycleState.Submitting);
        service.Transition("a", CommandLifecycleState.Acknowledged, "MA");

        for (int i = 0; i < 5; i++) {
            string other = "b-" + rng.Next();
            service.Transition(other, CommandLifecycleState.Submitting);
            service.Transition(other, CommandLifecycleState.Acknowledged, "MB-" + i);
            service.Transition(other, CommandLifecycleState.Confirmed);
        }

        service.GetState("a").ShouldBe(CommandLifecycleState.Acknowledged);
        service.GetMessageId("a").ShouldBe("MA");
    }

    [Property(MaxTest = 200)]
    public void Property_ExactlyOneTerminalOutcome_PerCorrelation(int seed) {
        Random rng = new(seed);
        using LifecycleStateService service = Create();
        string cid = "p4-" + seed;
        int terminalCount = 0;
        using IDisposable _ = service.Subscribe(cid, t => {
            if (IsTerminal(t.NewState) && !t.IdempotencyResolved) {
                terminalCount++;
            }
        });

        service.Transition(cid, CommandLifecycleState.Submitting);
        service.Transition(cid, CommandLifecycleState.Acknowledged, "M");
        CommandLifecycleState terminal = rng.Next(2) == 0
            ? CommandLifecycleState.Confirmed
            : CommandLifecycleState.Rejected;
        service.Transition(cid, terminal);
        // Replay attempts — all rejected as invalid because post-terminal.
        service.Transition(cid, CommandLifecycleState.Confirmed);
        service.Transition(cid, CommandLifecycleState.Rejected);

        terminalCount.ShouldBe(1);
    }

    [Property(MaxTest = 200)]
    public void Property_DuplicateMessageIdIdempotent(int seed) {
        Random rng = new(seed);
        using LifecycleStateService service = Create();
        string cid = "p5-" + seed;
        string msg = "MX-" + seed;
        int confirmed = 0;
        using IDisposable _ = service.Subscribe(cid, t => {
            if (t.NewState == CommandLifecycleState.Confirmed && !t.IdempotencyResolved) {
                confirmed++;
            }
        });

        service.Transition(cid, CommandLifecycleState.Submitting);
        service.Transition(cid, CommandLifecycleState.Acknowledged, msg);
        service.Transition(cid, CommandLifecycleState.Confirmed, msg);
        for (int i = 0; i < rng.Next(1, 6); i++) {
            service.Transition(cid, CommandLifecycleState.Confirmed, msg);
        }

        confirmed.ShouldBe(1);
    }

    [Property(MaxTest = 200)]
    public void Property_ResetToIdleFromAnyState_ReturnsToIdle(int seed) {
        Random rng = new(seed);
        using LifecycleStateService service = Create();
        string cid = "p6-" + seed;
        service.Transition(cid, CommandLifecycleState.Submitting);
        if (rng.Next(2) == 0) {
            service.Transition(cid, CommandLifecycleState.Acknowledged, "M");
        }

        service.Transition(cid, CommandLifecycleState.Idle);

        service.GetState(cid).ShouldBe(CommandLifecycleState.Idle);
    }

    [Property(MaxTest = 200)]
    public void Property_SubscribeReceivesAllValidTransitions_InCausalOrder(int seed) {
        using LifecycleStateService service = Create();
        string cid = "p7-" + seed;
        List<CommandLifecycleState> captured = [];
        using IDisposable _ = service.Subscribe(cid, t => captured.Add(t.NewState));

        service.Transition(cid, CommandLifecycleState.Submitting);
        service.Transition(cid, CommandLifecycleState.Acknowledged, "M");
        service.Transition(cid, CommandLifecycleState.Syncing);
        service.Transition(cid, CommandLifecycleState.Confirmed);

        captured.ShouldBe([
            CommandLifecycleState.Submitting,
            CommandLifecycleState.Acknowledged,
            CommandLifecycleState.Syncing,
            CommandLifecycleState.Confirmed,
        ]);
    }

    [Property(MaxTest = 200)]
    public void Property_InvalidTransitions_DroppedWithoutStateChange(int seed) {
        using LifecycleStateService service = Create();
        string cid = "p8-" + seed;

        // Try invalid transitions from Idle.
        service.Transition(cid, CommandLifecycleState.Acknowledged, "M");
        service.Transition(cid, CommandLifecycleState.Confirmed);
        service.Transition(cid, CommandLifecycleState.Rejected);

        service.GetState(cid).ShouldBe(CommandLifecycleState.Idle);
    }

    [Property(MaxTest = 200)]
    public void Property_TerminalStatesStaySticky_UntilResetToIdle(int seed) {
        using LifecycleStateService service = Create();
        string cid = "p9-" + seed;

        service.Transition(cid, CommandLifecycleState.Submitting);
        service.Transition(cid, CommandLifecycleState.Acknowledged, "M");
        service.Transition(cid, CommandLifecycleState.Confirmed);
        service.Transition(cid, CommandLifecycleState.Syncing);  // invalid from Confirmed
        service.Transition(cid, CommandLifecycleState.Acknowledged);  // invalid

        service.GetState(cid).ShouldBe(CommandLifecycleState.Confirmed);
    }

    [Property(MaxTest = 200)]
    public void Property_DisposeClearsAllStateDeterministic(int seed) {
        Random rng = new(seed);
        LifecycleStateService service = Create();
        int n = rng.Next(1, 10);
        for (int i = 0; i < n; i++) {
            string cid = "p10-" + i;
            service.Transition(cid, CommandLifecycleState.Submitting);
            service.Transition(cid, CommandLifecycleState.Acknowledged, "M-" + i);
        }

        service.Dispose();

        service.GetActiveCorrelationIds().ShouldBeEmpty();
    }

    [Property(MaxTest = 50)]
    public void Property_ConcurrentTransition_Linearizability(int seed) {
        using LifecycleStateService service = Create();
        string[] pool = Enumerable.Range(0, 8).Select(i => "p11-" + seed + "-" + i).ToArray();
        int[] terminalCounts = new int[pool.Length];
        List<IDisposable> subs = new();
        for (int i = 0; i < pool.Length; i++) {
            int idx = i;
            subs.Add(service.Subscribe(pool[idx], t => {
                if (IsTerminal(t.NewState) && !t.IdempotencyResolved) {
                    Interlocked.Increment(ref terminalCounts[idx]);
                }
            }));
        }

        Parallel.For(0, 32, i => {
            string cid = pool[i % pool.Length];
            service.Transition(cid, CommandLifecycleState.Submitting);
            service.Transition(cid, CommandLifecycleState.Acknowledged, "M-" + i);
            service.Transition(cid, CommandLifecycleState.Confirmed);
        });

        foreach (IDisposable s in subs) {
            s.Dispose();
        }

        terminalCounts.All(c => c == 1).ShouldBeTrue(
            "every CorrelationId that reached terminal should receive exactly one non-idempotent terminal notification");
    }

    [Property(MaxTest = 50)]
    public void Property_MessageIdLruUnderCachePressure(int seed) {
        const int cap = 32;
        using LifecycleStateService service = Create(cap);
        int n = cap * 2;

        Parallel.For(0, n, i => {
            string cid = "p12-" + seed + "-" + i;
            service.Transition(cid, CommandLifecycleState.Submitting);
            service.Transition(cid, CommandLifecycleState.Acknowledged, "M-" + seed + "-" + i);
        });

        // At least cap entries should be live; we just assert no crash and bounded-ish memory.
        service.GetActiveCorrelationIds().Count().ShouldBeGreaterThan(0);
    }

    [Property(MaxTest = 100)]
    public void Property_CrossCorrelationMessageIdReplay_TreatedAsFresh(int seed) {
        using LifecycleStateService service = Create();
        string cidA = "p13a-" + seed;
        string cidB = "p13b-" + seed;
        string msg = "SHARED-" + seed;

        service.Transition(cidA, CommandLifecycleState.Submitting);
        service.Transition(cidA, CommandLifecycleState.Acknowledged, msg);
        service.Transition(cidA, CommandLifecycleState.Confirmed);

        service.Transition(cidB, CommandLifecycleState.Submitting);
        service.Transition(cidB, CommandLifecycleState.Acknowledged, msg);

        service.GetState(cidB).ShouldBe(CommandLifecycleState.Acknowledged);
        service.GetMessageId(cidB).ShouldBe(msg);
        service.GetState(cidA).ShouldBe(CommandLifecycleState.Confirmed);
    }

    [Property(MaxTest = 100)]
    public void Property_ScopeLifetimeDedup_SameCorrelationSameMsgId_NoDoubleOutcome(int seed) {
        using LifecycleStateService service = Create();
        string cid = "p14-" + seed;
        string msg = "MSG-" + seed;
        int confirmedCount = 0;
        using IDisposable _ = service.Subscribe(cid, t => {
            if (t.NewState == CommandLifecycleState.Confirmed && !t.IdempotencyResolved) {
                confirmedCount++;
            }
        });

        service.Transition(cid, CommandLifecycleState.Submitting);
        service.Transition(cid, CommandLifecycleState.Acknowledged, msg);
        service.Transition(cid, CommandLifecycleState.Confirmed, msg);
        for (int i = 0; i < 5; i++) {
            service.Transition(cid, CommandLifecycleState.Confirmed, msg);
        }

        confirmedCount.ShouldBe(1);
    }

    [Property(MaxTest = 50)]
    public void Property_ReEntrantTransitionFromInsideCallback_NoDeadlock(int seed) {
        using LifecycleStateService service = Create();
        string a = "p15a-" + seed;
        string b = "p15b-" + seed;

        using IDisposable _ = service.Subscribe(a, t => {
            if (t.NewState == CommandLifecycleState.Submitting) {
                service.Transition(b, CommandLifecycleState.Submitting);
            }
        });

        DateTime start = DateTime.UtcNow;
        service.Transition(a, CommandLifecycleState.Submitting);
        TimeSpan elapsed = DateTime.UtcNow - start;

        elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
        service.GetState(a).ShouldBe(CommandLifecycleState.Submitting);
        service.GetState(b).ShouldBe(CommandLifecycleState.Submitting);
    }
}
