using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>
/// Story 13.3 VG4-10 — one command per non-text editor family (date picker, enum select, switch) so
/// the rendered field contract is covered beyond text inputs. AA5-01 — the nullable enum and
/// <c>bool?</c> members prove that the generated form compiles: the <c>bool?</c> binds through a
/// non-nullable proxy with validation on the model property, and (VG7-02) the nullable enum binds the
/// nullable model property directly, so its own Fluent field renders its validation message.
/// </summary>
[Command]
[BoundedContext("TestCommands")]
public sealed class FieldContractEditorsCommand {
    public string MessageId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Due date")]
    public DateTime? DueDate { get; set; }

    public FieldContractPriority Priority { get; set; }

    public bool Urgent { get; set; }

    [Required]
    [Display(Name = "Escalation priority")]
    public FieldContractPriority? EscalationPriority { get; set; }

    [Display(Name = "Notify owner")]
    public bool? NotifyOwner { get; set; }
}
