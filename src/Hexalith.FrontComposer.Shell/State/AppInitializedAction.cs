namespace Hexalith.FrontComposer.Shell.State;

/// <summary>
/// Cross-cutting action dispatched by the consuming application after the Fluxor store initializes.
/// Triggers hydration effects for all persisted features (Theme, Density, etc.).
/// </summary>
/// <param name="CorrelationId">Correlation identifier for tracing.</param>
public record AppInitializedAction(string CorrelationId);
