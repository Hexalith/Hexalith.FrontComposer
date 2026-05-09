using Hexalith.FrontComposer.Contracts.Attributes;

namespace IdeParity.Counter;

/// <summary>
/// Deterministic projection used by the Story 9-3 IDE parity matrix. Generated outputs
/// (<c>IdeParity.Counter.IdeParityCounterProjection.g.razor.cs</c>, the Fluxor feature, and
/// the registration shim) are pinned by IDE-MUST-001..006 evidence rows; adopters can open
/// this file in any supported IDE to verify generated-source navigation, completion, hover,
/// and diagnostics.
/// </summary>
[BoundedContext("Counter", DisplayLabel = "Counter")]
[Projection]
public partial class IdeParityCounterProjection
{
    /// <summary>Stable identifier surfaced in generated DataGrid columns.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Counter value. Drives the deterministic Fluxor reducer fixture.</summary>
    public int Count { get; set; }

    /// <summary>Status string that exercises generated badge rendering and symbol search.</summary>
    public string Status { get; set; } = string.Empty;
}
