// ATDD RED PHASE — Story 3-3 Task 10.4 (D2, D3; AC1, AC4, AC5)
// Fails at compile until Task 2.1 (state shape rewindow: UserPreference + EffectiveDensity) and
// Task 2.3 (new actions) land. Asserts D3: reducer assigns from action payload only — NO DI,
// the resolver runs at the action producer.

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.Density;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.Density;

/// <summary>
/// Story 3-3 Task 10.4 — pure reducer tests for the rewindowed
/// <see cref="FrontComposerDensityState"/> = <c>(UserPreference, EffectiveDensity)</c>.
/// Each reducer takes a payload that already carries the pre-resolved <c>NewEffective</c>.
/// </summary>
public sealed class DensityReducersTests
{
    [Fact]
    public void UserPreferenceChanged_AssignsBothFields()
    {
        FrontComposerDensityState state = new(UserPreference: null, EffectiveDensity: DensityLevel.Comfortable);
        UserPreferenceChangedAction action = new("c1", DensityLevel.Compact, DensityLevel.Compact);

        FrontComposerDensityState result = DensityReducers.ReduceUserPreferenceChanged(state, action);

        result.UserPreference.ShouldBe(DensityLevel.Compact);
        result.EffectiveDensity.ShouldBe(DensityLevel.Compact);
    }

    [Fact]
    public void UserPreferenceCleared_NullsPreferenceAndAssignsEffective()
    {
        FrontComposerDensityState state = new(UserPreference: DensityLevel.Compact, EffectiveDensity: DensityLevel.Compact);
        UserPreferenceClearedAction action = new("c1", DensityLevel.Comfortable);

        FrontComposerDensityState result = DensityReducers.ReduceUserPreferenceCleared(state, action);

        result.UserPreference.ShouldBeNull("Cleared must null the user preference (D8 + D13).");
        result.EffectiveDensity.ShouldBe(DensityLevel.Comfortable);
    }

    [Theory]
    [InlineData(DensityLevel.Compact)]
    [InlineData(DensityLevel.Comfortable)]
    [InlineData(DensityLevel.Roomy)]
    public void DensityHydrated_AssignsBothFieldsFromPayload(DensityLevel hydrated)
    {
        FrontComposerDensityState state = new(UserPreference: null, EffectiveDensity: DensityLevel.Comfortable);
        DensityHydratedAction action = new(UserPreference: hydrated, NewEffective: hydrated);

        FrontComposerDensityState result = DensityReducers.ReduceDensityHydrated(state, action);

        result.UserPreference.ShouldBe(hydrated);
        result.EffectiveDensity.ShouldBe(hydrated);
    }

    [Fact]
    public void EffectiveDensityRecomputed_PreservesUserPreference()
    {
        // ADR-040 — viewport-driven recompute MUST NOT clear UserPreference; only EffectiveDensity changes.
        FrontComposerDensityState state = new(UserPreference: DensityLevel.Compact, EffectiveDensity: DensityLevel.Compact);
        EffectiveDensityRecomputedAction action = new(NewEffective: DensityLevel.Comfortable);

        FrontComposerDensityState result = DensityReducers.ReduceEffectiveDensityRecomputed(state, action);

        result.UserPreference.ShouldBe(DensityLevel.Compact, "ADR-040 — UserPreference must survive viewport-forced recompute.");
        result.EffectiveDensity.ShouldBe(DensityLevel.Comfortable);
    }

    [Fact]
    public void DensityChangedAction_LegacyEntry_AssignsBothFieldsFromNewDensity()
    {
        // Story 3-1 legacy DensityChangedAction is RETAINED (Task 2.4) so Story 3-4 command-palette
        // can dispatch it directly. Its reducer treats the payload as both the preference and the
        // effective density (no resolver call inside reducer per D3).
        FrontComposerDensityState state = new(UserPreference: null, EffectiveDensity: DensityLevel.Comfortable);
        DensityChangedAction action = new("c1", DensityLevel.Roomy);

        FrontComposerDensityState result = DensityReducers.ReduceDensityChanged(state, action);

        result.UserPreference.ShouldBe(DensityLevel.Roomy);
        result.EffectiveDensity.ShouldBe(DensityLevel.Roomy);
    }
}
