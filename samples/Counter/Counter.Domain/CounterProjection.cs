using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

namespace Counter.Domain;

[Projection]
[BoundedContext("Counter")]
public partial class CounterProjection {
    public string Id { get; set; } = string.Empty;
    public int Count { get; set; }
    [Display(Name = "Last changed")]
    [RelativeTime]
    public DateTimeOffset LastUpdated { get; set; }
}
