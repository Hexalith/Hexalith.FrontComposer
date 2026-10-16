using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

namespace Counter.Specimens.Domain;

/// <summary>Generated zero-editor command confined to the opt-in specimen domain.</summary>
[Command]
[BoundedContext("Specimens")]
[RequiresPolicy("Specimens.Accessibility")]
[Display(Name = "Run accessibility specimen")]
public sealed class AccessibilityZeroFieldCommand
{
    /// <summary>Gets or sets the framework-derived message identity.</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Gets or sets the framework-derived tenant identity.</summary>
    public string TenantId { get; set; } = string.Empty;
}
