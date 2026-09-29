using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>
/// Story 13.3 VG4-10 — one command per non-text editor family (date picker, enum select, switch) so
/// the rendered field contract is covered beyond text inputs. Nullable enums and <c>bool?</c> are a
/// known deferred generator gap and are intentionally not used here.
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
}
