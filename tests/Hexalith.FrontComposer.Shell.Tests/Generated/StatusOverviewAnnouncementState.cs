using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>Status values in the generated announcement fixture.</summary>
public enum StatusOverviewAnnouncementState
{
    /// <summary>Open work.</summary>
    [ProjectionBadge(BadgeSlot.Info)]
    Open,

    /// <summary>Closed work.</summary>
    [ProjectionBadge(BadgeSlot.Neutral)]
    Closed,
}
