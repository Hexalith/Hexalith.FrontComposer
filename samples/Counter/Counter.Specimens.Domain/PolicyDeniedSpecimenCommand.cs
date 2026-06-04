using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

namespace Counter.Specimens.Domain;

[Command]
[RequiresPolicy("Specimens.PolicyDenied")]
[Display(Name = "Policy Denied Specimen")]
[BoundedContext("Specimens")]
public class PolicyDeniedSpecimenCommand {
    public string MessageId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Record Id")]
    public string RecordId { get; set; } = "FC-AUTH-DENY";

    public string Reason { get; set; } = "QA policy denied coverage";
}
