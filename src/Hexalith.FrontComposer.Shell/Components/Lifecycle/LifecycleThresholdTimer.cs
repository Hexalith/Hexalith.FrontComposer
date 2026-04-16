namespace Hexalith.FrontComposer.Shell.Components.Lifecycle;

/// <summary>
/// TDD RED-phase stub for Story 2-4. Task 2.4 authors the <see cref="TimeProvider"/>-driven
/// 100 ms polling loop that advances phase at configured thresholds.
/// </summary>
public sealed class LifecycleThresholdTimer : IDisposable {
    private readonly TimeProvider _time;
    private readonly int _pulseThresholdMs;
    private readonly int _stillSyncingThresholdMs;
    private readonly int _timeoutActionThresholdMs;

    public LifecycleThresholdTimer(
        TimeProvider time,
        int pulseThresholdMs,
        int stillSyncingThresholdMs,
        int timeoutActionThresholdMs) {
        _time = time;
        _pulseThresholdMs = pulseThresholdMs;
        _stillSyncingThresholdMs = stillSyncingThresholdMs;
        _timeoutActionThresholdMs = timeoutActionThresholdMs;
    }

    public event Action<LifecycleTimerPhase>? OnPhaseChanged;

    public LifecycleTimerPhase CurrentPhase => throw new NotImplementedException("TDD RED — Story 2-4 Task 2.4");

    public void Start() => throw new NotImplementedException("TDD RED — Story 2-4 Task 2.4");

    public void Reset(DateTimeOffset newAnchor) => throw new NotImplementedException("TDD RED — Story 2-4 Task 2.4");

    public void Stop() => throw new NotImplementedException("TDD RED — Story 2-4 Task 2.4");

    public void Dispose() => throw new NotImplementedException("TDD RED — Story 2-4 Task 2.4");

    private void RaisePhaseChanged(LifecycleTimerPhase phase) => OnPhaseChanged?.Invoke(phase);
}
