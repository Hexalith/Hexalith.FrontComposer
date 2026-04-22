using System.Collections.Immutable;

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

/// <summary>
/// Per-concern Fluxor state for the Story 3-5 capability-discovery feature (D8 / ADR-046).
/// </summary>
/// <param name="Counts">
/// Mirror of <c>IBadgeCountService.Counts</c> — lets <c>FrontComposerNavigation</c> +
/// <c>FcHomeDirectory</c> read counts via Fluxor's <c>[Inject] IState&lt;T&gt;</c> without each
/// component subscribing to the observable independently.
/// </param>
/// <param name="SeenCapabilities">
/// Persisted seen-set used to suppress the "New" capability badge once the user has visited a
/// bounded context or projection. Hydrated on app init via <c>IStorageService</c> under the
/// L03-fail-closed scope key <c>{tenantId}/{userId}/capability-seen</c>.
/// </param>
/// <param name="HydrationState">Lifecycle marker for the badge-count seed fetch (D15).</param>
public sealed record FrontComposerCapabilityDiscoveryState(
    ImmutableDictionary<Type, int> Counts,
    ImmutableHashSet<string> SeenCapabilities,
    CapabilityDiscoveryHydrationState HydrationState) {
    /// <summary>
    /// Gets the empty initial state — empty counts, empty seen-set, <c>Idle</c> hydration.
    /// </summary>
    public static FrontComposerCapabilityDiscoveryState Empty { get; } = new(
        Counts: ImmutableDictionary<Type, int>.Empty,
        SeenCapabilities: ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal),
        HydrationState: CapabilityDiscoveryHydrationState.Idle);
}
