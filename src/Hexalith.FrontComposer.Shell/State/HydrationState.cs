namespace Hexalith.FrontComposer.Shell.State;

/// <summary>
/// Represents the shared three-state lifecycle for Shell features that hydrate persisted state.
/// </summary>
public enum HydrationState {
    /// <summary>Hydration has not started.</summary>
    Idle,

    /// <summary>Hydration is in progress.</summary>
    Hydrating,

    /// <summary>Hydration completed successfully or through a feature-specific fail-closed path.</summary>
    Hydrated,
}
