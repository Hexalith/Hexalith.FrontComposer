using Hexalith.FrontComposer.Contracts.Attributes;

namespace Counter.Specimens.Domain;

[Projection]
[BoundedContext("Specimens")]
public partial class SpecimenStatusProjection {
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public SpecimenBadgeState Status { get; set; }

    public string Owner { get; set; } = string.Empty;
}

public enum SpecimenBadgeState {
    [ProjectionBadge(BadgeSlot.Neutral)]
    Neutral,

    [ProjectionBadge(BadgeSlot.Info)]
    Info,

    [ProjectionBadge(BadgeSlot.Success)]
    Success,

    [ProjectionBadge(BadgeSlot.Warning)]
    Warning,

    [ProjectionBadge(BadgeSlot.Danger)]
    Danger,

    [ProjectionBadge(BadgeSlot.Accent)]
    Accent,
}
