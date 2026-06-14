using System.Collections.Immutable;

namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Cascading parameter carrying row-level projection context down to generated command renderers so
/// that <c>ProjectionContextProvider</c> can pre-fill derivable fields (Story 2-2 Decision D27).
/// Host DataGrid rows (Epic 4) cascade this; Story 2-2 renderers tolerate <see langword="null"/>.
/// </summary>
public sealed record ProjectionContext {

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionContext"/> record.
    /// </summary>
    /// <param name="projectionTypeFqn">Fully qualified name of the projection type (e.g. <c>Counter.Domain.CounterProjection</c>).</param>
    /// <param name="boundedContext">Bounded context the projection belongs to.</param>
    /// <param name="aggregateId">Optional aggregate identifier when the row represents a single entity.</param>
    /// <param name="fields">Per-column values available for derivable-field lookup, keyed by property name. Must not be null.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="projectionTypeFqn"/> or <paramref name="boundedContext"/> is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fields"/> is null.</exception>
    public ProjectionContext(
        string projectionTypeFqn,
        string boundedContext,
        string? aggregateId,
        IImmutableDictionary<string, object?> fields) {
        ProjectionTypeFqn = projectionTypeFqn;
        BoundedContext = boundedContext;
        AggregateId = aggregateId;
        Fields = fields;
    }

    /// <summary>Gets the fully qualified name of the projection type.</summary>
    public string ProjectionTypeFqn {
        get;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new ArgumentException("Projection type FQN cannot be null, empty, or whitespace.", nameof(value));
            }

            field = value;
        }
    } = string.Empty;

    /// <summary>Gets the bounded context the projection belongs to.</summary>
    public string BoundedContext {
        get;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new ArgumentException("Bounded context cannot be null, empty, or whitespace.", nameof(value));
            }

            field = value;
        }
    } = string.Empty;

    /// <summary>Gets the optional aggregate identifier when the row represents a single entity.</summary>
    public string? AggregateId { get; init; }

    /// <summary>
    /// Gets the per-column values available for derivable-field lookup, keyed by property name.
    /// </summary>
    /// <remarks>
    /// Typed as <see cref="IImmutableDictionary{TKey, TValue}"/> so that snapshots cannot be
    /// mutated by the producer after cascading; consumers can rely on the contents being stable.
    /// </remarks>
    public IImmutableDictionary<string, object?> Fields {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = ImmutableDictionary<string, object?>.Empty;
}
