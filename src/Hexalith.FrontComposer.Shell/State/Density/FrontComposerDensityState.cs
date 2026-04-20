using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.Density;

/// <summary>
/// Fluxor state record for the application display density (Story 3-3 D2 — rewindow of the Story 3-1
/// single-field <c>(CurrentDensity)</c> shape). Two-field record separates the user's explicit choice
/// from the resolver output so the settings dialog can show "user picked Compact; viewport forced
/// Comfortable" without needing a per-component re-resolve.
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
public record FrontComposerDensityState(
    DensityLevel? UserPreference,
    DensityLevel EffectiveDensity);
