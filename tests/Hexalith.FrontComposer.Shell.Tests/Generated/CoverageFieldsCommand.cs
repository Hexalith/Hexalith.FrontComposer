using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>Exercises text, numeric, and unsupported editors on a generated command form.</summary>
[Command]
[BoundedContext("TestCommands")]
public sealed class CoverageFieldsCommand
{
    /// <summary>Gets or sets the command message identifier.</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the quantity subject to range validation.</summary>
    [Range(1, 20, ErrorMessage = "Choose a quantity from 1 through 20.")]
    public int Quantity { get; set; }

    /// <summary>Gets or sets metadata that the form cannot edit.</summary>
    [SuppressMessage("HexalithFrontComposer", "HFC1002:Unsupported field type", Justification = "This fixture exercises the generated unsupported editor.")]
    public Dictionary<string, string> Metadata { get; set; } = [];
}
