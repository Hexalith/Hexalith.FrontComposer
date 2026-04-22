using Fluxor;

namespace Hexalith.FrontComposer.Shell.State.Navigation;

/// <summary>
/// Per-circuit scope-readiness observer (Story 3-6 D13 / D20 / ADR-049). Inspects
/// <c>IUserContextAccessor</c> + <c>FrontComposerNavigationState.StorageReady</c>, dispatches
/// <see cref="StorageReadyAction"/> exactly once on the first empty → authenticated transition,
/// and short-circuits subsequent calls.
/// </summary>
/// <remarks>
/// The gate is scoped so Fluxor's concurrent effect handler invocations observe the same
/// dispatched/not-dispatched state. An internal <c>Interlocked</c> tiebreaker ensures exactly-once
/// dispatch even when two observed actions fire nearly simultaneously before the reducer flips
/// <see cref="FrontComposerNavigationState.StorageReady"/>.
/// </remarks>
public interface IScopeReadinessGate {
    /// <summary>
    /// Evaluates scope and dispatches <see cref="StorageReadyAction"/> iff the transition is
    /// a first-time empty-to-authenticated move within this circuit. No-op afterwards.
    /// </summary>
    /// <param name="dispatcher">The Fluxor dispatcher to emit <see cref="StorageReadyAction"/> on.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EvaluateAsync(IDispatcher dispatcher, CancellationToken cancellationToken = default);
}
