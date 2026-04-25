namespace Hexalith.FrontComposer.Contracts.Badges;

/// <summary>
/// Event payload describing a single badge-count change (Story 3-4 ADR-044, Story 3-5 producer).
/// </summary>
/// <param name="ProjectionType">
/// The projection runtime type whose count changed. MUST NOT be <see langword="null"/> —
/// the primary constructor enforces this via <see cref="ArgumentNullException.ThrowIfNull(object?, string?)"/>.
/// </param>
/// <param name="NewCount">
/// The new total count of actionable items. Producers SHOULD emit a non-negative value;
/// consumers (palette badge cells) clamp negative values to zero defensively.
/// </param>
public sealed record BadgeCountChangedArgs(Type ProjectionType, int NewCount) {
    /// <summary>
    /// The projection runtime type whose count changed (enforced non-null).
    /// </summary>
    public Type ProjectionType { get; init; } = ProjectionType ?? throw new ArgumentNullException(nameof(ProjectionType));
}
