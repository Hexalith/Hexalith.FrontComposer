using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>A command that shares the lifecycle label of <see cref="FocusCollision"/>.</summary>
[Command]
[BoundedContext("FocusCollision")]
public sealed class FocusCollisionCommand
{
    /// <summary>Gets or sets the dispatch identifier.</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Gets or sets the required command input.</summary>
    [Required]
    public string Name { get; set; } = string.Empty;
}
