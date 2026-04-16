using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

[Command]
[BoundedContext("TestCommands")]
public class ZeroFieldInlineCommand {
    public string MessageId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;
}

[Command]
[BoundedContext("TestCommands")]
public class OneFieldInlineCommand {
    public string MessageId { get; set; } = string.Empty;

    public int Amount { get; set; }
}

[Command]
[BoundedContext("TestCommands")]
public class TwoFieldCompactCommand {
    public string MessageId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Amount { get; set; }
}

[Command]
[BoundedContext("TestCommands")]
public class CompactCommandWithDerivableField {
    public string MessageId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Amount { get; set; }
}

[Command]
[BoundedContext("TestCommands")]
public class FiveFieldFullPageCommand {
    public string MessageId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int InitialValue { get; set; }

    public int MaxValue { get; set; }

    public string Category { get; set; } = string.Empty;
}

[Command]
[BoundedContext("TestCommands")]
[Icon("This.Icon.Definitely.Does.Not.Exist")]
public class IconFallbackInlineCommand {
    public string MessageId { get; set; } = string.Empty;

    public int Amount { get; set; }
}
