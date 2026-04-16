namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Cascading parameter carrying row-level projection context down to generated command renderers so
/// that <c>ProjectionContextProvider</c> can pre-fill derivable fields (Story 2-2 Decision D27).
/// Host DataGrid rows (Epic 4) cascade this; Story 2-2 renderers tolerate <see langword="null"/>.
/// </summary>
/// <param name="ProjectionTypeFqn">Fully qualified name of the projection type (e.g. <c>Counter.Domain.CounterProjection</c>).</param>
/// <param name="BoundedContext">Bounded context the projection belongs to.</param>
/// <param name="AggregateId">Optional aggregate identifier when the row represents a single entity.</param>
/// <param name="Fields">Per-column values available for derivable-field lookup, keyed by property name.</param>
public sealed record ProjectionContext(
    string ProjectionTypeFqn,
    string BoundedContext,
    string? AggregateId,
    IReadOnlyDictionary<string, object?> Fields);
