using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

namespace Counter.Specimens.Domain;

[Command]
[RequiresPolicy("Specimens.PolicyAllowed")]
[Display(Name = "Policy Allowed Specimen")]
[BoundedContext("Specimens")]
public class PolicyAllowedSpecimenCommand {
    public string MessageId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Record Id")]
    public string RecordId { get; set; } = "FC-AUTH-ALLOW";

    public string Reason { get; set; } = "QA policy allowed coverage";
}
