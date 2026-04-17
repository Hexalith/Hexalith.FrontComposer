using Hexalith.FrontComposer.Shell.Components.Lifecycle;

using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Lifecycle;

/// <summary>
/// Story 2-4 Task 2.4 — pure-class unit tests for <see cref="LifecycleThresholdTimer"/>.
/// Uses <see cref="FakeTimeProvider"/> so <c>Advance(TimeSpan)</c> drives the internal
/// <see cref="ITimer"/> callbacks deterministically (D5).
/// </summary>
public sealed class LifecycleThresholdTimerTests {
    private const int PulseMs = 300;
    private const int StillSyncingMs = 2_000;
    private const int TimeoutMs = 10_000;

    [Fact]
    public void Phase_advances_NoPulse_Pulse_StillSyncing_ActionPrompt_as_fake_time_advances_past_thresholds() {
        FakeTimeProvider clock = new(DateTimeOffset.UtcNow);
        using LifecycleThresholdTimer timer = new(clock, PulseMs, StillSyncingMs, TimeoutMs);
        List<LifecycleTimerPhase> observed = [];
        timer.OnPhaseChanged += observed.Add;
        timer.Reset(clock.GetUtcNow());
        timer.Start();

        clock.Advance(TimeSpan.FromMilliseconds(PulseMs + 50));
        timer.CurrentPhase.ShouldBe(LifecycleTimerPhase.Pulse);

        clock.Advance(TimeSpan.FromMilliseconds(StillSyncingMs - PulseMs));
        timer.CurrentPhase.ShouldBe(LifecycleTimerPhase.StillSyncing);

        clock.Advance(TimeSpan.FromMilliseconds(TimeoutMs - StillSyncingMs));
        timer.CurrentPhase.ShouldBe(LifecycleTimerPhase.ActionPrompt);

        observed.ShouldContain(LifecycleTimerPhase.Pulse);
        observed.ShouldContain(LifecycleTimerPhase.StillSyncing);
        observed.ShouldContain(LifecycleTimerPhase.ActionPrompt);
    }

    [Fact]
    public void Reset_with_new_anchor_rewinds_phase_to_NoPulse() {
        FakeTimeProvider clock = new(DateTimeOffset.UtcNow);
        using LifecycleThresholdTimer timer = new(clock, PulseMs, StillSyncingMs, TimeoutMs);
        timer.Reset(clock.GetUtcNow());
        timer.Start();
        clock.Advance(TimeSpan.FromMilliseconds(3_000));
        timer.CurrentPhase.ShouldBe(LifecycleTimerPhase.StillSyncing);

        timer.Reset(clock.GetUtcNow());

        timer.CurrentPhase.ShouldBe(LifecycleTimerPhase.NoPulse);
    }

    [Fact]
    public void OnPhaseChanged_fires_exactly_once_per_phase_transition_no_duplicates() {
        FakeTimeProvider clock = new(DateTimeOffset.UtcNow);
        using LifecycleThresholdTimer timer = new(clock, PulseMs, StillSyncingMs, TimeoutMs);
        List<LifecycleTimerPhase> observed = [];
        timer.OnPhaseChanged += observed.Add;
        timer.Reset(clock.GetUtcNow());
        timer.Start();

        clock.Advance(TimeSpan.FromMilliseconds(PulseMs + 10));
        clock.Advance(TimeSpan.FromMilliseconds(20)); // still in Pulse — no re-emit
        clock.Advance(TimeSpan.FromMilliseconds(50)); // still in Pulse — no re-emit

        observed.Count(p => p == LifecycleTimerPhase.Pulse).ShouldBe(1);
    }

    [Fact]
    public void Stop_then_Dispose_cancels_timer_and_no_further_events_fire() {
        FakeTimeProvider clock = new(DateTimeOffset.UtcNow);
        LifecycleThresholdTimer timer = new(clock, PulseMs, StillSyncingMs, TimeoutMs);
        List<LifecycleTimerPhase> observed = [];
        timer.OnPhaseChanged += observed.Add;

        timer.Reset(clock.GetUtcNow());
        timer.Start();
        timer.Stop();
        timer.Dispose();
        int beforeAdvance = observed.Count;
        clock.Advance(TimeSpan.FromSeconds(20));

        observed.Count.ShouldBe(beforeAdvance, "no phase events should fire after Stop+Dispose");
    }
}
