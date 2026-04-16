using Hexalith.FrontComposer.Shell.Components.Lifecycle;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Lifecycle;

/// <summary>
/// TDD RED-phase pure-class unit tests for Story 2-4 Task 2.4. Exercises the
/// <see cref="LifecycleThresholdTimer"/> state machine against a controllable
/// <see cref="TimeProvider"/> with no Blazor / DI involved.
/// </summary>
public sealed class LifecycleThresholdTimerTests {
    private static readonly int PulseMs = 300;
    private static readonly int StillSyncingMs = 2_000;
    private static readonly int TimeoutMs = 10_000;

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.4: Phase advances through NoPulse→Pulse→StillSyncing→ActionPrompt as fake time advances past thresholds.")]
    public void Phase_advances_NoPulse_Pulse_StillSyncing_ActionPrompt_as_fake_time_advances_past_thresholds() {
        TestClock clock = new(DateTimeOffset.UtcNow);
        using LifecycleThresholdTimer timer = new(clock, PulseMs, StillSyncingMs, TimeoutMs);
        List<LifecycleTimerPhase> observed = [];
        timer.OnPhaseChanged += observed.Add;

        timer.Start();
        clock.Advance(TimeSpan.FromMilliseconds(PulseMs + 50));
        timer.CurrentPhase.ShouldBe(LifecycleTimerPhase.Pulse);

        clock.Advance(TimeSpan.FromMilliseconds(StillSyncingMs - PulseMs));
        timer.CurrentPhase.ShouldBe(LifecycleTimerPhase.StillSyncing);

        clock.Advance(TimeSpan.FromMilliseconds(TimeoutMs - StillSyncingMs));
        timer.CurrentPhase.ShouldBe(LifecycleTimerPhase.ActionPrompt);

        observed.ShouldBe([LifecycleTimerPhase.NoPulse, LifecycleTimerPhase.Pulse, LifecycleTimerPhase.StillSyncing, LifecycleTimerPhase.ActionPrompt]);
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.4: Reset(newAnchor) rewinds the phase to NoPulse irrespective of prior advances.")]
    public void Reset_with_new_anchor_rewinds_phase_to_NoPulse() {
        TestClock clock = new(DateTimeOffset.UtcNow);
        using LifecycleThresholdTimer timer = new(clock, PulseMs, StillSyncingMs, TimeoutMs);
        timer.Start();
        clock.Advance(TimeSpan.FromMilliseconds(3_000));
        timer.CurrentPhase.ShouldBe(LifecycleTimerPhase.StillSyncing);

        timer.Reset(clock.GetUtcNow());

        timer.CurrentPhase.ShouldBe(LifecycleTimerPhase.NoPulse);
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.4: OnPhaseChanged must fire exactly once per transition (no duplicate emissions).")]
    public void OnPhaseChanged_fires_exactly_once_per_phase_transition_no_duplicates() {
        TestClock clock = new(DateTimeOffset.UtcNow);
        using LifecycleThresholdTimer timer = new(clock, PulseMs, StillSyncingMs, TimeoutMs);
        List<LifecycleTimerPhase> observed = [];
        timer.OnPhaseChanged += observed.Add;

        timer.Start();
        clock.Advance(TimeSpan.FromMilliseconds(PulseMs + 10));
        clock.Advance(TimeSpan.FromMilliseconds(20)); // still in Pulse — no re-emit
        clock.Advance(TimeSpan.FromMilliseconds(50)); // still in Pulse — no re-emit

        observed.Count(p => p == LifecycleTimerPhase.Pulse).ShouldBe(1);
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 2.4: Stop+Dispose cancels the timer so no further OnPhaseChanged events fire.")]
    public void Stop_then_Dispose_cancels_timer_and_no_further_events_fire() {
        TestClock clock = new(DateTimeOffset.UtcNow);
        LifecycleThresholdTimer timer = new(clock, PulseMs, StillSyncingMs, TimeoutMs);
        List<LifecycleTimerPhase> observed = [];
        timer.OnPhaseChanged += observed.Add;

        timer.Start();
        timer.Stop();
        timer.Dispose();
        int beforeAdvance = observed.Count;
        clock.Advance(TimeSpan.FromSeconds(20));

        observed.Count.ShouldBe(beforeAdvance, "no phase events should fire after Stop+Dispose");
    }

    private sealed class TestClock : TimeProvider {
        private DateTimeOffset _now;

        public TestClock(DateTimeOffset now) => _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now = _now.Add(by);
    }
}
