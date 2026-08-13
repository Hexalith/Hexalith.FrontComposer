namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Contains adopter-resolved target intent for one command.
/// </summary>
/// <param name="ViewKey">The canonical generated view or lane key.</param>
/// <param name="EntityKey">The exact target projection entity key.</param>
/// <param name="PriorStatus">The optional status before the declared change.</param>
/// <param name="ExpectedStatus">The optional destination status after the declared change.</param>
public sealed record CommandTargetIdentity(
    string ViewKey,
    string EntityKey,
    string? PriorStatus = null,
    string? ExpectedStatus = null);
