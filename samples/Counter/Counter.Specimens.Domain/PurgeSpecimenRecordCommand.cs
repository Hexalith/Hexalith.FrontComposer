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
    [Display(Name = "Record Id", Description = "Record to purge.")]
    [ProjectionFieldGroup("Purge details")]
    public string RecordId { get; set; } = "FC-1002";

    [Display(Description = "Why this purge is required.")]
    [ProjectionFieldGroup("Purge details")]
    public string Reason { get; set; } = "QA destructive confirmation coverage";

    // Story 13.3 VG7-02 — a required nullable enum renders as a Fluent select, so browser evidence
    // covers the invalid state and description of a non-text editor's focusable control.
    [Required]
    [Display(Name = "Retention", Description = "How long the purged record stays recoverable.")]
    public PurgeRetention? Retention { get; set; }
}
