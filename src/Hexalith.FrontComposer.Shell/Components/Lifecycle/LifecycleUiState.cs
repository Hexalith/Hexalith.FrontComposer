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
/// Derived UI state for <see cref="FcLifecycleWrapper"/>. Story 2-4 Decision D6 / Story 2-5 D3 — immutable
/// record mapped from (CommandLifecycleTransition, LifecycleTimerPhase) via <see cref="From"/>. Story 2-5
/// D17 converted this to init-only properties so the <c>IsIdempotent</c> addition is append-safe without
/// breaking call-site positional construction.
/// </summary>
/// <remarks>
/// <para>State transition table (Story 2-4 AC1–AC7 + Story 2-5 AC1/AC2):</para>
/// <list type="bullet">
///   <item><description>(Idle, *) → no announcement, no pulse, no message bar (AC1 quiet).</description></item>
///   <item><description>(Submitting, *) → polite "Submitting…" announcement; no pulse.</description></item>
///   <item><description>(Acknowledged, NoPulse) → silent; timer started.</description></item>
///   <item><description>(Syncing, Pulse) → outline pulse CSS class applied (visual only, no announcement — Sally 2026-04-16).</description></item>
///   <item><description>(Syncing, StillSyncing) → polite "Still syncing…" badge + polite announcement (AC4).</description></item>
///   <item><description>(Syncing, ActionPrompt) → assertive warning message bar + "Start over" button (AC5).</description></item>
///   <item><description>(Confirmed, <see cref="IsIdempotent"/>=false) → polite success message bar (auto-dismiss via <see cref="ConfirmedDismissAt"/>) (AC6).</description></item>
///   <item><description>(Confirmed, <see cref="IsIdempotent"/>=true) → polite Info message bar — "This was already confirmed — no action needed." — Story 2-5 AC2.</description></item>
///   <item><description>(Rejected, *) → assertive danger message bar (D4 domain-language title), no auto-dismiss (AC7 + Story 2-5 D17).</description></item>
/// </list>
/// </remarks>
public sealed record LifecycleUiState {
    /// <summary>The latest lifecycle state observed from <see cref="CommandLifecycleTransition"/>.</summary>
    public CommandLifecycleState Current { get; init; }

    /// <summary>The timer-driven phase (threshold bucket).</summary>
    public LifecycleTimerPhase TimerPhase { get; init; }

    /// <summary>The ULID MessageId (nullable; present from Acknowledged onward).</summary>
    public string? MessageId { get; init; }

    /// <summary>True when Story 2-3 D10 detected a duplicate MessageId resolution.</summary>
    public bool IdempotencyResolved { get; init; }

    /// <summary>The monotonic anchor (Story 2-3 D15) used by the threshold timer.</summary>
    public DateTimeOffset LastTransitionAt { get; init; }

    /// <summary>Optional domain-specific rejection copy; null falls back to localized generic.</summary>
    public string? RejectionMessage { get; init; }

    /// <summary>Optional domain-specific rejection TITLE (Story 2-5 D4); null falls back to "Submission rejected" (Story 2-4 localized).</summary>
    public string? RejectionTitle { get; init; }

    /// <summary>When Confirmed, the wall-clock cutoff for auto-dismiss; otherwise null.</summary>
    public DateTimeOffset? ConfirmedDismissAt { get; init; }

    /// <summary>
    /// Story 2-5 D3 — true when the wrapper should render the Info-severity idempotent message bar
    /// instead of the standard Confirmed Success bar. Derived from
    /// <c>transition.IdempotencyResolved &amp;&amp; transition.NewState == Confirmed</c>.
    /// </summary>
    public bool IsIdempotent { get; init; }

    /// <summary>
    /// Story 2-5 D3 — wall-clock cutoff for the idempotent Info bar auto-dismiss. Scheduled at
    /// <see cref="FcShellOptions.IdempotentInfoToastDurationMs"/> after
    /// <see cref="LastTransitionAt"/>. Null outside the idempotent-Confirmed branch.
    /// </summary>
    public DateTimeOffset? IdempotentDismissAt { get; init; }

    /// <summary>An Idle-state singleton with default values. Used when no transition has been observed yet.</summary>
    public static readonly LifecycleUiState Idle = new() {
        Current = CommandLifecycleState.Idle,
        TimerPhase = LifecycleTimerPhase.NoPulse,
    };

    /// <summary>
    /// Pure mapper from (<paramref name="transition"/>, <paramref name="phase"/>) to a
    /// derived <see cref="LifecycleUiState"/>. No side effects.
    /// </summary>
    /// <param name="transition">The latest lifecycle transition delivered by the service.</param>
    /// <param name="phase">The current threshold phase from <see cref="LifecycleThresholdTimer"/>.</param>
    /// <param name="rejectionMessage">The optional <c>RejectionMessage</c> parameter of the wrapper (Story 2-5 populates).</param>
    /// <param name="rejectionTitle">The optional <c>RejectionTitle</c> parameter (Story 2-5 D4 domain-language title).</param>
    /// <returns>A new <see cref="LifecycleUiState"/>.</returns>
    public static LifecycleUiState From(
        CommandLifecycleTransition transition,
        LifecycleTimerPhase phase,
        string? rejectionMessage = null,
        string? rejectionTitle = null) {
        ArgumentNullException.ThrowIfNull(transition);

        // Terminal states ignore the timer phase — always Terminal in the UI state.
        LifecycleTimerPhase effectivePhase = transition.NewState is CommandLifecycleState.Confirmed
                                                or CommandLifecycleState.Rejected
            ? LifecycleTimerPhase.Terminal
            : phase;

        bool isIdempotent = transition.IdempotencyResolved
            && transition.NewState == CommandLifecycleState.Confirmed;

        return new LifecycleUiState {
            Current = transition.NewState,
            TimerPhase = effectivePhase,
            MessageId = transition.MessageId,
            IdempotencyResolved = transition.IdempotencyResolved,
            LastTransitionAt = transition.LastTransitionAt,
            RejectionMessage = rejectionMessage,
            RejectionTitle = rejectionTitle,
            IsIdempotent = isIdempotent,
        };
    }
}
