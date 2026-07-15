namespace Hexalith.FrontComposer.Shell.State.CapabilityDiscovery;

/// <summary>
/// Tracks the lifecycle of the badge-count seed fetch (Story 3-5 D8 / D15).
/// </summary>
public enum CapabilityDiscoveryHydrationState {
    /// <summary>The seed fetch has not started yet — components render skeletons.</summary>
    Idle,

    /// <summary>The seed fetch is in flight — components keep skeletons up.</summary>
    Seeding,

    /// <summary>The seed fetch has published its initial dictionary — components render real cards.</summary>
    Seeded,
}
