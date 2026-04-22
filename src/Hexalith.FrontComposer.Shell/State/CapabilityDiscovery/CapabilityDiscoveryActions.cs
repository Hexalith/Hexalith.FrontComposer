using System.Collections.Immutable;

namespace Hexalith.FrontComposer.Shell.State.CapabilityDiscovery;

/// <summary>
/// Dispatched by <c>CapabilityDiscoveryEffects.HandleAppInitialized</c> after the initial
/// <c>BadgeCountService.InitializeAsync</c> fan-out completes (Story 3-5 D4 / D8).
/// Carries the seeded counts dictionary; reducer assigns it wholesale and flips
/// <see cref="FrontComposerCapabilityDiscoveryState.HydrationState"/> to <c>Seeded</c>.
/// </summary>
/// <param name="Counts">The seeded per-projection-type counts.</param>
public sealed record BadgeCountsSeededAction(ImmutableDictionary<Type, int> Counts);

/// <summary>
/// Dispatched by <c>CapabilityDiscoveryEffects</c> when an individual badge count is published
/// by <c>IBadgeCountService.CountChanged</c> (Story 3-5 D8 / D14). Reducer applies a single-key
/// update; <see cref="FrontComposerCapabilityDiscoveryState.HydrationState"/> is unchanged.
/// </summary>
/// <param name="ProjectionType">The projection runtime type whose count changed.</param>
/// <param name="NewCount">The new count value.</param>
public sealed record BadgeCountChangedAction(Type ProjectionType, int NewCount);

/// <summary>
/// Dispatched by <c>FrontComposerNavigation</c> and <c>FcHomeDirectory</c> on the FIRST click of
/// any nav entry / card carrying the "New" capability badge (Story 3-5 D9 / D11 / D13).
/// Reducer adds the id to <see cref="FrontComposerCapabilityDiscoveryState.SeenCapabilities"/>
/// (idempotent on re-dispatch); the persist effect writes the updated set to storage under the
/// fail-closed scope key.
/// </summary>
/// <param name="CapabilityId">The stable capability id (<c>bc:{BC}</c> or <c>proj:{BC}:{Type}</c>).</param>
public sealed record CapabilityVisitedAction(string CapabilityId);

/// <summary>
/// Dispatched by <c>CapabilityDiscoveryEffects.HandleAppInitialized</c> after the seen-set blob
/// is read from storage (Story 3-5 D9 / AC11). The dispatch happens unconditionally — when the
/// scope is invalid (L03 fail-closed) the payload is
/// <see cref="ImmutableHashSet{T}.Empty"/> so the rendering pipeline unblocks.
/// </summary>
/// <param name="SeenCapabilities">The hydrated seen-set (or empty).</param>
public sealed record SeenCapabilitiesHydratedAction(ImmutableHashSet<string> SeenCapabilities);
