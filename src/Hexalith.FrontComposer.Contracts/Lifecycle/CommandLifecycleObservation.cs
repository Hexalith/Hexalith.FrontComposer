namespace Hexalith.FrontComposer.Contracts.Lifecycle;

/// <summary>
/// Carries one typed command lifecycle observation across a dispatch adapter boundary.
/// </summary>
/// <param name="State">The observed lifecycle state.</param>
/// <param name="MessageId">The accepted command ULID when known.</param>
/// <param name="Materiality">Typed terminal materiality evidence.</param>
/// <param name="ObservedAt">The adapter observation time when available.</param>
public sealed record CommandLifecycleObservation(
    CommandLifecycleState State,
    string? MessageId,
    CommandMateriality Materiality,
    DateTimeOffset? ObservedAt = null);
