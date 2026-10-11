using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

[Projection]
[BoundedContext("NoticeDetail")]
[ProjectionRole(ProjectionRole.DetailRecord)]
public partial class NoticeDetailProjection
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
