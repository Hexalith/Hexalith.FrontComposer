using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

namespace AdopterProofKit.Domain;

/// <summary>A generated projection surface with no private tenant data.</summary>
[Projection]
[BoundedContext("Proof")]
public sealed partial class ProofProjection
{
    /// <summary>Gets or sets the public proof item identifier.</summary>
    [Display(Name = "Proof item")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the public proof item label.</summary>
    [Display(Name = "Label")]
    public string Label { get; set; } = string.Empty;
}
