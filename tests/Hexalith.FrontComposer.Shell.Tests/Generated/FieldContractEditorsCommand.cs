using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>
/// Story 13.3 VG4-10 — one command per non-text editor family (date picker, enum select, switch) so
/// the rendered field contract is covered beyond text inputs. AA5-01 — the nullable enum and
/// <c>bool?</c> members prove that the generated form compiles and binds through non-nullable
/// proxies while validation stays on the model properties.
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
