using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

namespace Counter.Specimens.Domain;

[Projection]
[BoundedContext("Specimens")]
public partial class SpecimenFormattingProjection {
    public Guid Id { get; set; }

    [Display(Name = "Total Orders")]
    public decimal TotalOrders { get; set; }

    [Display(Name = "Submitted At")]
    public DateTimeOffset SubmittedAt { get; set; }

    [Display(Name = "Last Sync")]
    [RelativeTime]
    public DateTimeOffset LastSync { get; set; }

    [Display(Name = "Optional Comment")]
    public string? OptionalComment { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "HexalithFrontComposer",
        "HFC1002:Unsupported field type",
        Justification = "This sample fixture intentionally exercises an unsupported collection field.")]
    public string[] Approvers { get; set; } = [];

    [Currency]
    public decimal Budget { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; }

    [Display(Name = "Lifecycle State")]
    public SpecimenLongLifecycleState LifecycleState { get; set; }

    [Display(Name = "Opaque Payload")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "HexalithFrontComposer",
        "HFC1002:Unsupported field type",
        Justification = "This sample fixture intentionally exercises an unsupported dictionary field.")]
    public Dictionary<string, string> OpaquePayload { get; set; } = [];
}

public enum SpecimenLongLifecycleState {
    AwaitingManualReviewAndExtendedGovernanceApproval,
}
