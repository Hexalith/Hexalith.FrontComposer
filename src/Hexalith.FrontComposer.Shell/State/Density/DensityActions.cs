
using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.Density;
/// <summary>
/// Dispatched when the application display density changes.
/// </summary>
/// <param name="CorrelationId">Correlation identifier for tracing.</param>
/// <param name="NewDensity">The new density level to apply.</param>
public record DensityChangedAction(string CorrelationId, DensityLevel NewDensity);
