
using Fluxor;

namespace Hexalith.FrontComposer.Shell.State.Density;
/// <summary>
/// Pure reducers for <see cref="FrontComposerDensityState"/>.
/// </summary>
public static class DensityReducers {
    /// <summary>
    /// Applies a density change to the state.
    /// </summary>
    /// <param name="state">The current density state.</param>
    /// <param name="action">The density changed action.</param>
    /// <returns>A new state with the updated density.</returns>
    [ReducerMethod]
    public static FrontComposerDensityState ReduceDensityChanged(FrontComposerDensityState state, DensityChangedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { CurrentDensity = action.NewDensity };
    }
}
