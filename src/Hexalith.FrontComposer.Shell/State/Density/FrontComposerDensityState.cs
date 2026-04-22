using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.Density;

/// <summary>
/// Fluxor state record for the application display density (Story 3-3 D2 / Story 3-6 D19).
/// Two-field record separates the user's explicit choice from the resolver output so the settings
/// dialog can show "user picked Compact; viewport forced Comfortable" without a per-component
/// re-resolve.
/// </summary>
/// <param name="UserPreference">
/// Explicit user choice. <see langword="null"/> means "follow defaults" — the resolver falls through
/// to deployment default and factory hybrid. Persisted to <c>{tenantId}:{userId}:density</c> via
/// <c>DensityEffects.HandleUserPreferenceChanged</c>.
/// </param>
/// <param name="EffectiveDensity">
/// Output of <c>DensityPrecedence.Resolve(...)</c>. Never <see langword="null"/>. Consumed by
/// <c>FcDensityApplier</c>, which projects the value onto the document body density attribute so
/// scoped CSS cascades. NEVER persisted — always recomputed from <see cref="UserPreference"/> +
/// current options + current viewport tier.
/// </param>
/// <param name="HydrationState">
/// Transient three-state hydration marker (Story 3-6 D19). Initial value <see cref="DensityHydrationState.Idle"/>;
/// flips <c>Idle → Hydrating → Hydrated</c> via dedicated reducers. NEVER persisted. Re-hydrate
/// via <c>StorageReadyAction</c> only runs when this is <see cref="DensityHydrationState.Idle"/>.
/// </param>
public record FrontComposerDensityState(
    DensityLevel? UserPreference,
    DensityLevel EffectiveDensity,
    DensityHydrationState HydrationState = DensityHydrationState.Idle);
