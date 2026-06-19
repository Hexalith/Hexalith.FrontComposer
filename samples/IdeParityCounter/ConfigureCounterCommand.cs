using Hexalith.FrontComposer.Contracts.Attributes;

namespace IdeParity.Counter;

/// <summary>
/// Deterministic command used by the Story 9-3 IDE parity matrix. The generated form,
/// renderer, and lifecycle bridge artifacts (<c>.CommandRenderer.g.razor.cs</c>,
/// <c>.CommandLifecycleBridge.g.cs</c>) are pinned by IDE-MUST-003..005 evidence rows.
/// </summary>
[Command]
[BoundedContext("Counter", DisplayLabel = "Counter")]
public sealed class ConfigureCounterCommand
{
    /// <summary>Required dispatch identifier.</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Counter target.</summary>
    public string CounterId { get; set; } = string.Empty;

    /// <summary>Increment step applied to the counter on apply.</summary>
    public int Step { get; set; }
}
