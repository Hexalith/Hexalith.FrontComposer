using Hexalith.FrontComposer.Contracts.Attributes;

namespace Counter.Domain;

/// <summary>
/// 3 non-derivable fields → CompactInline density (Story 2-2 AC10).
/// </summary>
[Command]
[BoundedContext("Counter")]
public class BatchIncrementCommand {
    public string MessageId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public int Amount { get; set; } = 1;
    public string Note { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
}
