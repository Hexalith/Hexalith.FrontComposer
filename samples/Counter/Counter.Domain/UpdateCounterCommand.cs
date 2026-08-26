using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

namespace Counter.Domain;

/// <summary>
/// Updates an existing counter under an exact provider-declared target key.
/// </summary>
[Command]
[CommandTarget(
    typeof(CounterProjection),
    CommandTargetResolutionMode.Provider,
    CommandTargetChangeKind.Update,
    ViewKey = "Counter:Counter.Domain.CounterProjection")]
[BoundedContext("Counter")]
public sealed class UpdateCounterCommand
{
    /// <summary>Gets or sets the framework message identifier.</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant identifier.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Gets or sets the exact counter key carried through dispatch.</summary>
    [Required]
    [Display(Name = "Counter key")]
    public string CounterId { get; set; } = string.Empty;

    /// <summary>Gets or sets the value to add to the counter.</summary>
    public int Amount { get; set; } = 1;
}
