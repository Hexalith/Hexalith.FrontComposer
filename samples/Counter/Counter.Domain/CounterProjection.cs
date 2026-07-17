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
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "HexalithFrontComposer",
        "HFC1002:Unsupported field type",
        Justification = "This sample fixture intentionally exercises an unsupported metadata dictionary.")]
    public Dictionary<string, string> Metadata { get; set; } = [];
}
