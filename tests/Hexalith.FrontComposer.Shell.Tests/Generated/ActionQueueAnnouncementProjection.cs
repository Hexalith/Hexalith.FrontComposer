using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>Generated ActionQueue fixture with a state rule and a user badge filter.</summary>
[BoundedContext("Tickets")]
[Projection]
[ProjectionRole(ProjectionRole.ActionQueue, WhenState = "Open")]
public partial class ActionQueueAnnouncementProjection
{
    /// <summary>Stable row identity.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Badge status used by the user filter.</summary>
    public StatusOverviewAnnouncementState Status { get; set; }
}
