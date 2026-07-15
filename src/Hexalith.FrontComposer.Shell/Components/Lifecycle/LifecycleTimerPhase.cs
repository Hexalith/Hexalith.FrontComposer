namespace Hexalith.FrontComposer.Shell.Components.Lifecycle;

/// <summary>
/// Threshold phase surfaced by <see cref="LifecycleThresholdTimer"/>. Story 2-4 Decision D4.
/// </summary>
public enum LifecycleTimerPhase {
    /// <summary>Acknowledged observed, elapsed &lt; SyncPulseThresholdMs. No visible pulse.</summary>
    NoPulse,

    /// <summary>Elapsed in [SyncPulseThresholdMs, StillSyncingThresholdMs). Wrapper renders outline pulse.</summary>
    Pulse,

    /// <summary>Elapsed in [StillSyncingThresholdMs, TimeoutActionThresholdMs). "Still syncing…" badge shown.</summary>
    StillSyncing,

    /// <summary>Elapsed &gt;= TimeoutActionThresholdMs. Action-prompt message bar shown.</summary>
    ActionPrompt,

    /// <summary>Terminal state reached (Confirmed / Rejected / Idle reset). Timer stopped.</summary>
    Terminal,
}
