using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

namespace AdopterProofKit.Domain;

/// <summary>Creates a proof item through the generated command form.</summary>
[Command]
[BoundedContext("Proof")]
public sealed class CreateProofCommand
{
    /// <summary>Gets or sets the framework message identifier.</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Gets or sets the proof label.</summary>
    [Required]
    [Display(Name = "Proof label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets a short public description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets a public category.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the proof sequence.</summary>
    public int Sequence { get; set; }

    /// <summary>Gets or sets a public reference label.</summary>
    public string Reference { get; set; } = string.Empty;
}
