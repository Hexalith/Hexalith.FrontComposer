using Hexalith.FrontComposer.Contracts.Attributes;

namespace Counter.Domain;

[Command]
[CommandTarget(typeof(CounterProjection), CommandTargetResolutionMode.SameAsSource, CommandTargetChangeKind.Update)]
public class IncrementCommand {
    public string MessageId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public int Amount { get; set; } = 1;
}
