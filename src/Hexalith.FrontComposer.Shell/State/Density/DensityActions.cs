using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.Density;

/// <summary>
/// Story 3-1 legacy action — retained as a backward-compatible entry point so Story 3-4's command
/// palette can dispatch a coarse "set density" without forking action types. Its reducer assigns the
/// incoming level to BOTH <c>UserPreference</c> and <c>EffectiveDensity</c> (see
/// <see cref="DensityReducers.ReduceDensityChanged"/>); Story 3-4 migrates callers to
/// <see cref="UserPreferenceChangedAction"/> when it lands.
/// </summary>
/// <param name="CorrelationId">Correlation identifier for tracing.</param>
/// <param name="NewDensity">The new density level to apply.</param>
public sealed record DensityChangedAction(string CorrelationId, DensityLevel NewDensity);

/// <summary>
/// Dispatched when the user selects a density radio in <c>FcSettingsDialog</c> (Story 3-3 D3).
/// Producer calls <c>DensityPrecedence.Resolve(...)</c> BEFORE dispatching and carries
/// <paramref name="NewEffective"/> in the payload — reducers assign pre-resolved values only
/// (ADR-039 purity invariant).
/// </summary>
/// <param name="CorrelationId">ULID correlation identifier for tracing.</param>
/// <param name="NewPreference">The user's explicit density choice.</param>
/// <param name="NewEffective">Pre-resolved effective density = <c>Resolve(NewPreference, options.DefaultDensity, surface, tier)</c>.</param>
public sealed record UserPreferenceChangedAction(
    string CorrelationId,
    DensityLevel NewPreference,
    DensityLevel NewEffective);

/// <summary>
/// Dispatched when the user clicks "Restore defaults" in <c>FcSettingsDialog</c> (Story 3-3 D13).
/// Clears the stored user preference; the resolver falls through to deployment default / factory
/// hybrid for the new <paramref name="NewEffective"/>.
/// </summary>
/// <param name="CorrelationId">ULID correlation identifier for tracing.</param>
/// <param name="NewEffective">Pre-resolved effective density with <c>UserPreference = null</c>.</param>
public sealed record UserPreferenceClearedAction(
    string CorrelationId,
    DensityLevel NewEffective);

/// <summary>
/// Dispatched by <c>DensityEffects.HandleAppInitialized</c> after loading the persisted user
/// preference (Story 3-3 D3 / D8). Does NOT trigger re-persistence (ADR-038 mirror — hydrate is
/// read-only).
/// </summary>
/// <param name="UserPreference">The hydrated user preference (<see langword="null"/> when the user never chose).</param>
/// <param name="NewEffective">Pre-resolved effective density at the current viewport.</param>
public sealed record DensityHydratedAction(
    DensityLevel? UserPreference,
    DensityLevel NewEffective);

/// <summary>
/// Dispatched by <c>DensityEffects.HandleViewportTierChanged</c> when a viewport transition causes
/// the effective density to change (Story 3-3 D7 / ADR-040). The reducer assigns only
/// <c>EffectiveDensity</c>; <c>UserPreference</c> is preserved so a return to Desktop re-applies the
/// user's choice.
/// </summary>
/// <param name="NewEffective">The re-resolved effective density for the new viewport tier.</param>
public sealed record EffectiveDensityRecomputedAction(DensityLevel NewEffective);
