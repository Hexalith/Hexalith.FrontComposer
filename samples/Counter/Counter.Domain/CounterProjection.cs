using Hexalith.FrontComposer.Contracts.Attributes;

namespace Counter.Domain;

[Projection]
[BoundedContext("Counter")]
public partial class CounterProjection {
    public string Id { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
}
