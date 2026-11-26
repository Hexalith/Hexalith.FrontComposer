using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>Exercises multiple enum initialization paths in a generated form.</summary>
[Command]
[BoundedContext("TestCommands")]
public sealed class NonzeroPriorityCommand
{
    /// <summary>Gets or sets the command message identifier.</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Gets or sets a priority with no zero-valued member.</summary>
    public NonzeroPriority Priority { get; set; }

    /// <summary>Gets or sets a priority with an explicit constructor default.</summary>
    public NonzeroPriority Preset { get; set; } = NonzeroPriority.Manual;

    /// <summary>Gets or sets an optional priority.</summary>
    public NonzeroPriority? Optional { get; set; }

    /// <summary>Gets or sets a priority whose zero member is valid.</summary>
    public FieldContractPriority WithZero { get; set; }
}
