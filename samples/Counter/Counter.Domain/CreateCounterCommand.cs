using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

namespace Counter.Domain;

/// <summary>
/// Creates a counter under an exact key supplied before dispatch.
/// </summary>
[Command]
[CommandTarget(
    typeof(CounterProjection),
    CommandTargetResolutionMode.Provider,
    CommandTargetChangeKind.Create,
    ViewKey = "Counter:Counter.Domain.CounterProjection")]
[BoundedContext("Counter")]
public sealed class CreateCounterCommand
{
    /// <summary>Gets or sets the framework message identifier.</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant identifier.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Gets or sets the exact counter key carried through dispatch.</summary>
    [Required]
    [Display(Name = "Counter key")]
    public string CounterId { get; set; } = string.Empty;

    /// <summary>Gets or sets the initial counter value.</summary>
    public int InitialValue { get; set; }
}
