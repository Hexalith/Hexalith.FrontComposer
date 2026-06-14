using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>
/// Story 2-3 AC3 specimen — bounded context for the badge end-to-end render pin.
/// </summary>
[BoundedContext("Badge")]
public sealed class BadgeDomain {
}

/// <summary>
/// Story 2-3 AC3 specimen — review state whose members carry <see cref="ProjectionBadgeAttribute"/>
/// slot annotations, driving the generated grid badge column under test.
/// </summary>
public enum ReviewState {
    /// <summary>Pending review — warning slot.</summary>
    [ProjectionBadge(BadgeSlot.Warning)]
    Pending,

    /// <summary>Approved — success slot.</summary>
    [ProjectionBadge(BadgeSlot.Success)]
    Approved,
}

/// <summary>
/// Story 2-3 AC3 specimen — minimal projection with a <c>[ProjectionBadge]</c>-annotated status
/// enum column, used to pin the generated-grid → <c>FcStatusBadge</c> aria-label flow end-to-end.
/// </summary>
[Projection]
[BoundedContext("Badge")]
public partial class BadgeProjection {
    /// <summary>Gets or sets the display name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the review status rendered as a badge.</summary>
    public ReviewState Status { get; set; }
}
