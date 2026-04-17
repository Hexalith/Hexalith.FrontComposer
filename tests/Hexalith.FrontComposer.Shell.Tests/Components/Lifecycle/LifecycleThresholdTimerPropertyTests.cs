using FsCheck;
using FsCheck.Xunit;

using Hexalith.FrontComposer.Shell.Components.Lifecycle;

using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Lifecycle;

/// <summary>
/// Story 2-4 Task 5.2b — FsCheck properties for <see cref="LifecycleThresholdTimer"/> invariants.
/// </summary>
public sealed class LifecycleThresholdTimerPropertyTests {
    private const int PulseMs = 300;
    private const int StillMs = 2_000;
    private const int TimeoutMs = 10_000;

    /// <summary>Pure bucket function matching <see cref="LifecycleThresholdTimer"/> (non-terminal path).</summary>
    private static LifecycleTimerPhase ExpectedPhaseFromElapsedMs(double elapsedMs) {
        if (elapsedMs < PulseMs) {
            return LifecycleTimerPhase.NoPulse;
        }

        if (elapsedMs < StillMs) {
            return LifecycleTimerPhase.Pulse;
        }

        if (elapsedMs < TimeoutMs) {
            return LifecycleTimerPhase.StillSyncing;
        }

        return LifecycleTimerPhase.ActionPrompt;
    }

    [Property(MaxTest = 150)]
    public void Phase_monotonic_under_arbitrary_tick_schedule(uint seed) {
        var rng = new Random((int)(seed % int.MaxValue));
        var time = new FakeTimeProvider();
        using var timer = new LifecycleThresholdTimer(time, PulseMs, StillMs, TimeoutMs);
        timer.Start();
        LifecycleTimerPhase last = timer.CurrentPhase;
        for (int i = 0; i < 40; i++) {
            time.Advance(TimeSpan.FromMilliseconds(rng.Next(0, 800)));
            LifecycleTimerPhase cur = timer.CurrentPhase;
            ((int)cur).ShouldBeGreaterThanOrEqualTo((int)last);
            last = cur;
        }
    }

    [Property(MaxTest = 120)]
    public void Reset_with_newer_anchor_is_idempotent_under_tick_ordering(NonNegativeInt strayMs) {
        ArgumentNullException.ThrowIfNull(strayMs);
        var time = new FakeTimeProvider();
        DateTimeOffset t0 = time.GetUtcNow();
        using var timer = new LifecycleThresholdTimer(time, PulseMs, StillMs, TimeoutMs);
        timer.Start();
        timer.Reset(t0.AddMilliseconds(-8_000));
        time.Advance(TimeSpan.FromMilliseconds(1 + (strayMs.Item % 500)));
        DateTimeOffset anchorFinal = time.GetUtcNow().AddMilliseconds(-150);
        timer.Reset(anchorFinal);
        LifecycleTimerPhase phaseAfter = timer.CurrentPhase;
        DateTimeOffset clockAtEnd = time.GetUtcNow();

        var freshTime = new FakeTimeProvider(clockAtEnd);
        using var fresh = new LifecycleThresholdTimer(freshTime, PulseMs, StillMs, TimeoutMs);
        fresh.Start();
        fresh.Reset(anchorFinal);
        phaseAfter.ShouldBe(fresh.CurrentPhase);
    }

    [Property(MaxTest = 200)]
    public void Phase_computation_equals_pure_elapsed_bucket_function(NonNegativeInt totalMs) {
        ArgumentNullException.ThrowIfNull(totalMs);
        var time = new FakeTimeProvider();
        DateTimeOffset t0 = time.GetUtcNow();
        using var timer = new LifecycleThresholdTimer(time, PulseMs, StillMs, TimeoutMs);
        timer.Start();
        timer.Reset(t0);
        int ms = totalMs.Item % 12_000;
        time.Advance(TimeSpan.FromMilliseconds(ms));
        LifecycleTimerPhase expected = ExpectedPhaseFromElapsedMs(ms);
        timer.CurrentPhase.ShouldBe(expected);
    }
}
