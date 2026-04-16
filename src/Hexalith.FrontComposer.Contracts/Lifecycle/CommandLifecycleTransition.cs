namespace Hexalith.FrontComposer.Contracts.Lifecycle;

/// <summary>
/// A single state transition observed by <see cref="ILifecycleStateService"/>. Delivered to subscribers
/// registered via <see cref="ILifecycleStateService.Subscribe(string, System.Action{CommandLifecycleTransition})"/>.
/// </summary>
/// <param name="CorrelationId">Caller-side correlation key (Decision D1 — string, not Guid).</param>
/// <param name="PreviousState">The state the entry was in before this transition.</param>
/// <param name="NewState">The state the entry is in after this transition.</param>
/// <param name="MessageId">The ULID MessageId (nullable; present from <see cref="CommandLifecycleState.Acknowledged"/> onward).</param>
/// <param name="TimestampUtc">When this transition was produced (<c>TimeProvider.GetUtcNow()</c> at <c>Transition()</c> call).</param>
/// <param name="LastTransitionAt">
/// Monotonic anchor for Story 2-4 progressive-visibility thresholds (300 ms pulse / 2 s text / 10 s prompt).
/// Equals <paramref name="TimestampUtc"/> on fresh transitions; carried forward from the originating
/// transition during idempotent replay so 2-4's timers anchor to real command elapsed time, not wall-clock
/// from subscribe (Decision D15 / Sally Story C).
/// </param>
/// <param name="IdempotencyResolved">
/// <see langword="true"/> when the transition resolved from duplicate-MessageId detection (Decision D10).
/// Observers (e.g., <c>FcLifecycleWrapper</c>) surface "already done" messaging instead of celebrating
/// a new success.
/// </param>
public sealed record CommandLifecycleTransition(
    string CorrelationId,
    CommandLifecycleState PreviousState,
    CommandLifecycleState NewState,
    string? MessageId,
    DateTimeOffset TimestampUtc,
    DateTimeOffset LastTransitionAt,
    bool IdempotencyResolved);
