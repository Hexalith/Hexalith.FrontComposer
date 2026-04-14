
using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.Density;
/// <summary>
/// Fluxor state record for the application display density.
/// Positional syntax enables <c>state with { CurrentDensity = action.NewDensity }</c> in reducers.
/// </summary>
/// <param name="CurrentDensity">The currently active density level.</param>
public record FrontComposerDensityState(DensityLevel CurrentDensity);
