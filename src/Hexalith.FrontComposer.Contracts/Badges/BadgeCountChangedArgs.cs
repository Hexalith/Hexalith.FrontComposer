namespace Hexalith.FrontComposer.Contracts.Badges;

/// <summary>
/// Event payload describing a single badge-count change (Story 3-4 ADR-044, Story 3-5 producer).
/// </summary>
/// <param name="ProjectionType">The projection runtime type whose count changed.</param>
/// <param name="NewCount">The new total count of actionable items.</param>
public sealed record BadgeCountChangedArgs(Type ProjectionType, int NewCount);
