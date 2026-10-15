using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>Protected zero-field renderer fixture for submit-time denial accessibility.</summary>
[Command]
[BoundedContext("TestCommands")]
[RequiresPolicy("TestCommands.ApproveInline")]
public sealed class ProtectedZeroFieldInlineCommand
{
    public string MessageId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;
}
