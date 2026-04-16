using Hexalith.FrontComposer.Contracts.Attributes;

namespace Counter.Domain;

/// <summary>
/// 5 non-derivable fields → FullPage density (Story 2-2 AC10). [Icon] attribute demonstrates
/// runtime fallback (Decision D34) when the icon cannot be resolved.
/// </summary>
[Command]
[Icon("Regular.Size20.Settings")]
[BoundedContext("Counter")]
public class ConfigureCounterCommand {
    public string MessageId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int InitialValue { get; set; }
    public int MaxValue { get; set; } = 100;
    public string Category { get; set; } = "General";
}
