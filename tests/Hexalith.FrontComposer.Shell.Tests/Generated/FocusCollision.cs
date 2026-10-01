using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>A command whose name collides with another command after suffix removal.</summary>
[Command]
[BoundedContext("FocusCollision")]
public sealed class FocusCollision
{
    /// <summary>Gets or sets the dispatch identifier.</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Gets or sets the required command input.</summary>
    [Required]
    public string Name { get; set; } = string.Empty;
}
