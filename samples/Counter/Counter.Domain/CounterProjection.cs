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
#pragma warning disable HFC1002 // Story 6-5 sample intentionally exposes one unsupported placeholder for dev-mode discovery.
    public Dictionary<string, string> Metadata { get; set; } = [];
#pragma warning restore HFC1002
}
