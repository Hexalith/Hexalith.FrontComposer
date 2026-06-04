using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

namespace Counter.Specimens.Domain;

[Command]
[Destructive(
    ConfirmationTitle = "Purge specimen record?",
    ConfirmationBody = "This specimen record is used by visual and accessibility evidence.")]
[Display(Name = "Purge Specimen Record")]
[BoundedContext("Specimens")]
public class PurgeSpecimenRecordCommand {
    public string MessageId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Record Id")]
    public string RecordId { get; set; } = "FC-1002";

    public string Reason { get; set; } = "QA destructive confirmation coverage";
}
