using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>Generated StatusOverview fixture for the hidden expanded group path.</summary>
[BoundedContext("Tickets")]
[Projection]
[ProjectionRole(ProjectionRole.StatusOverview)]
public partial class StatusOverviewAnnouncementProjection
{
    /// <summary>Stable row identity.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Current status bucket.</summary>
    public StatusOverviewAnnouncementState Status { get; set; }
}
