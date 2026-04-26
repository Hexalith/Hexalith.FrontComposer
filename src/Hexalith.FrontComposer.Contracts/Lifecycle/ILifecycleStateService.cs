namespace Hexalith.FrontComposer.Contracts.Lifecycle;

/// <summary>
/// Story 2-3: cross-command correlation-keyed index over per-command Fluxor lifecycle slices.
/// Bridges (emitted per <c>[Command]</c>) forward Submitted/Acknowledged/Syncing/Confirmed/Rejected/ResetToIdle
/// actions to <see cref="Transition(string, CommandLifecycleState, string?)"/>, and consumers (e.g., Story 2-4's
/// <c>FcLifecycleWrapper</c>) <see cref="Subscribe(string, System.Action{CommandLifecycleTransition})"/> to a
/// CorrelationId to receive the stream of transitions.
/// </summary>
/// <remarks>
/// <para>
/// Scoped lifetime (ADR-017) — per-circuit in Blazor Server, per-user in WASM. Mis-registering as Singleton
/// throws at construction via the Decision D20 guard.
/// </para>
/// <para>
/// v0.1 is circuit-local: a user who closes a tab mid-command and reopens lands on a fresh circuit with an
/// empty dictionary — <see cref="Subscribe(string, System.Action{CommandLifecycleTransition})"/> will NOT replay
/// the prior transition. Epic 5 (Story 5-4) backs this interface with durable server-side lookup without
/// changing its shape.
/// </para>
/// </remarks>
public interface ILifecycleStateService : IDisposable {
    /// <summary>
    /// Subscribes a callback to transitions for the given <paramref name="correlationId"/>.
    /// On subscribe the callback is invoked once with the current entry's state (replay) if an entry
    /// already exists, then invoked on every subsequent <see cref="Transition(string, CommandLifecycleState, string?)"/>
    /// until the returned <see cref="IDisposable"/> is disposed. Disposal is idempotent.
    /// </summary>
    /// <remarks>
    /// Bespoke callback contract (Decision D7 / ADR-018) — intentionally NOT <c>IObservable&lt;T&gt;</c> to avoid
    /// inviting consumers to pull <c>System.Reactive</c> transitively. Callbacks are invoked outside any
    /// per-entry lock so they may synchronously call back into <see cref="Transition(string, CommandLifecycleState, string?)"/>
    /// for other correlations without deadlock (FsCheck property #15).
    /// </remarks>
    /// <param name="correlationId">The caller-side correlation key (Decision D1 — string, not Guid).</param>
    /// <param name="onTransition">Callback invoked synchronously when a transition occurs.</param>
    /// <returns>An <see cref="IDisposable"/> that stops further invocations when disposed.</returns>
    IDisposable Subscribe(string correlationId, Action<CommandLifecycleTransition> onTransition);

    /// <summary>
    /// Returns the current lifecycle state for <paramref name="correlationId"/>, or
    /// <see cref="CommandLifecycleState.Idle"/> if unknown (never throws).
    /// </summary>
    /// <param name="correlationId">The caller-side correlation key.</param>
    /// <returns>The current lifecycle state.</returns>
    CommandLifecycleState GetState(string correlationId);

    /// <summary>
    /// Returns the ULID MessageId recorded against <paramref name="correlationId"/>, or
    /// <see langword="null"/> if no MessageId has been associated yet.
    /// </summary>
    /// <param name="correlationId">The caller-side correlation key.</param>
    /// <returns>The ULID MessageId or <see langword="null"/>.</returns>
    string? GetMessageId(string correlationId);

    /// <summary>
    /// Snapshot enumeration of CorrelationIds with a live entry. Intended for debug / diagnostic
    /// surfaces (Hindsight H10 — exposed for Story 9-2 CLI inspection).
    /// </summary>
    /// <returns>A snapshot of active correlation IDs.</returns>
    IEnumerable<string> GetActiveCorrelationIds();

    /// <summary>
    /// Applies a transition to <paramref name="correlationId"/>. Idempotent: duplicate same-state calls are
    /// noops; invalid state-machine edges are dropped with an HFC2004 log (FR30 "≤1 outcome" invariant).
    /// Cross-CorrelationId MessageId collisions are logged HFC2005 and treated as fresh submissions (Decision D10).
    /// </summary>
    /// <param name="correlationId">The caller-side correlation key.</param>
    /// <param name="newState">The target lifecycle state.</param>
    /// <param name="messageId">The ULID MessageId (required from <see cref="CommandLifecycleState.Acknowledged"/> onward).</param>
    void Transition(string correlationId, CommandLifecycleState newState, string? messageId = null);

    /// <summary>
    /// Story 5-5 P8 — applies a terminal transition with an explicit idempotency-resolved flag so
    /// callers (e.g. the pending-command resolver handling an
    /// <c>PendingCommandStatus.IdempotentConfirmed</c> outcome) can suppress the duplicate-celebration
    /// UI even when this is the first observed terminal for the correlation. <see langword="true"/>
    /// overrides the auto-computed flag and instructs <see cref="CommandLifecycleTransition.IdempotencyResolved"/>
    /// consumers to render the "already confirmed" path. Implementations that do not need this flag
    /// can simply forward to the three-arg overload.
    /// </summary>
    void Transition(
        string correlationId,
        CommandLifecycleState newState,
        string? messageId,
        bool idempotencyResolved);
}
