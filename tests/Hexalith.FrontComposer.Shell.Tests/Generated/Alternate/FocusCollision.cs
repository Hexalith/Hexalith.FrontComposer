using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Shell.Tests.Generated.Alternate;

/// <summary>A command that shares its short name with one in another namespace.</summary>
[Command]
[BoundedContext("AlternateFocusCollision")]
public sealed class FocusCollision
{
    /// <summary>Gets or sets the dispatch identifier.</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Gets or sets the required command input.</summary>
    [Required]
    public string Name { get; set; } = string.Empty;
}
