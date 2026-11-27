using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

namespace Counter.Specimens.Domain;

/// <summary>Exercises required and nullable generated native time editors.</summary>
[Command]
[BoundedContext("Specimens")]
[Display(Name = "Submit native times")]
public sealed class NativeTimeSpecimenCommand
{
    /// <summary>Gets or sets the framework-derived message identity.</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Gets or sets the framework-derived tenant identity.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Gets or sets the required time.</summary>
    [Required]
    public TimeOnly RequiredTime { get; set; } = new(8, 15);

    /// <summary>Gets or sets the optional time.</summary>
    public TimeOnly? OptionalTime { get; set; } = new(17, 30);
}
