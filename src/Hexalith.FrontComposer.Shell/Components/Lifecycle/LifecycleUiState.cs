using Hexalith.FrontComposer.Contracts.Lifecycle;

namespace Hexalith.FrontComposer.Shell.Components.Lifecycle;

/// <summary>
/// TDD RED-phase stub for Story 2-4. Task 2.3 replaces with the full (CurrentState, TimerPhase)
/// mapping and rendered-element derivation.
/// </summary>
public sealed record LifecycleUiState(
    CommandLifecycleState Current,
    LifecycleTimerPhase TimerPhase,
    string? MessageId,
    DateTimeOffset LastTransitionAt) {
    public static LifecycleUiState From(CommandLifecycleTransition transition, LifecycleTimerPhase phase)
        => throw new NotImplementedException("TDD RED — Story 2-4 Task 2.3");
}

/// <summary>
/// Threshold phase surfaced by <see cref="LifecycleThresholdTimer"/>.
/// TDD RED stub — Task 2.4 authors the phase state machine.
/// </summary>
public enum LifecycleTimerPhase {
    NoPulse,
    Pulse,
    StillSyncing,
    ActionPrompt,
    Terminal,
}
