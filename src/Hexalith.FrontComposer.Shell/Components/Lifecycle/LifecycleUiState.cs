using Hexalith.FrontComposer.Contracts.Lifecycle;

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

/// <summary>
/// Derived UI state for <see cref="FcLifecycleWrapper"/>. Story 2-4 Decision D6 — immutable record
/// mapped from (CommandLifecycleTransition, LifecycleTimerPhase) via <see cref="From"/>.
/// </summary>
/// <remarks>
/// <para>State transition table (Story 2-4 AC1–AC7):</para>
/// <list type="bullet">
///   <item><description>(Idle, *) → no announcement, no pulse, no message bar (AC1 quiet).</description></item>
///   <item><description>(Submitting, *) → polite "Submitting…" announcement; no pulse.</description></item>
///   <item><description>(Acknowledged, NoPulse) → silent; timer started.</description></item>
///   <item><description>(Syncing, Pulse) → outline pulse CSS class applied (visual only, no announcement — Sally 2026-04-16).</description></item>
///   <item><description>(Syncing, StillSyncing) → polite "Still syncing…" badge + polite announcement (AC4).</description></item>
///   <item><description>(Syncing, ActionPrompt) → assertive warning message bar + "Start over" button (AC5).</description></item>
///   <item><description>(Confirmed, *) → polite success message bar (auto-dismiss via <see cref="ConfirmedDismissAt"/>) (AC6).</description></item>
///   <item><description>(Rejected, *) → assertive danger message bar, no auto-dismiss (AC7).</description></item>
/// </list>
/// </remarks>
/// <param name="Current">The latest lifecycle state observed from <see cref="CommandLifecycleTransition"/>.</param>
/// <param name="TimerPhase">The timer-driven phase (threshold bucket).</param>
/// <param name="MessageId">The ULID MessageId (nullable; present from Acknowledged onward).</param>
/// <param name="IdempotencyResolved">True when Story 2-3 D10 detected a duplicate MessageId resolution.</param>
/// <param name="LastTransitionAt">The monotonic anchor (Story 2-3 D15) used by the threshold timer.</param>
/// <param name="RejectionMessage">Optional domain-specific rejection copy; null falls back to localized generic.</param>
/// <param name="ConfirmedDismissAt">When Confirmed, the wall-clock cutoff for auto-dismiss; otherwise null.</param>
public sealed record LifecycleUiState(
    CommandLifecycleState Current,
    LifecycleTimerPhase TimerPhase,
    string? MessageId,
    bool IdempotencyResolved,
    DateTimeOffset LastTransitionAt,
    string? RejectionMessage,
    DateTimeOffset? ConfirmedDismissAt) {

    /// <summary>An Idle-state singleton with default values. Used when no transition has been observed yet.</summary>
    public static readonly LifecycleUiState Idle = new(
        CommandLifecycleState.Idle,
        LifecycleTimerPhase.NoPulse,
        MessageId: null,
        IdempotencyResolved: false,
        LastTransitionAt: default,
        RejectionMessage: null,
        ConfirmedDismissAt: null);

    /// <summary>
    /// Pure mapper from (<paramref name="transition"/>, <paramref name="phase"/>) to a
    /// derived <see cref="LifecycleUiState"/>. No side effects.
    /// </summary>
    /// <param name="transition">The latest lifecycle transition delivered by the service.</param>
    /// <param name="phase">The current threshold phase from <see cref="LifecycleThresholdTimer"/>.</param>
    /// <param name="rejectionMessage">The optional <c>RejectionMessage</c> parameter of the wrapper (Story 2-5 populates).</param>
    /// <returns>A new <see cref="LifecycleUiState"/>.</returns>
    public static LifecycleUiState From(
        CommandLifecycleTransition transition,
        LifecycleTimerPhase phase,
        string? rejectionMessage = null) {
        ArgumentNullException.ThrowIfNull(transition);

        // Terminal states ignore the timer phase — always Terminal in the UI state.
        LifecycleTimerPhase effectivePhase = transition.NewState is CommandLifecycleState.Confirmed
                                                or CommandLifecycleState.Rejected
            ? LifecycleTimerPhase.Terminal
            : phase;

        return new LifecycleUiState(
            Current: transition.NewState,
            TimerPhase: effectivePhase,
            MessageId: transition.MessageId,
            IdempotencyResolved: transition.IdempotencyResolved,
            LastTransitionAt: transition.LastTransitionAt,
            RejectionMessage: rejectionMessage,
            ConfirmedDismissAt: null);
    }
}
