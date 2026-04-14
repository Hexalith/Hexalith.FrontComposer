
using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

[BoundedContext("Status")]
public sealed class StatusDomain {
}

[Projection]
[BoundedContext("Status")]
public partial class StatusProjection {
    public string? Name { get; set; }

    public bool? IsEnabled { get; set; }
}
