using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>Exercises generated date editors for date-only model properties.</summary>
[Command]
[BoundedContext("TestCommands")]
public sealed class DateOnlyCommand
{
    /// <summary>Gets or sets the command message identifier.</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Gets or sets the start date.</summary>
    public DateOnly StartsOn { get; set; }

    /// <summary>Gets or sets the optional end date.</summary>
    public DateOnly? EndsOn { get; set; }
}
