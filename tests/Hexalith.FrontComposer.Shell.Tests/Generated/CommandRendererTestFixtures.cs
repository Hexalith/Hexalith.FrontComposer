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
[RequiresPolicy("TestCommands.ApproveInline")]
public class ProtectedOneFieldInlineCommand {
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
[RequiresPolicy("TestCommands.ApproveCompact")]
public class ProtectedTwoFieldCompactCommand {
    public string MessageId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Amount { get; set; }
}

[Command]
[BoundedContext("TestCommands")]
public class FourFieldCompactCommand {
    public string MessageId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Amount { get; set; }

    public int Priority { get; set; }
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
[RequiresPolicy("TestCommands.ApproveFullPage")]
public class ProtectedFiveFieldFullPageCommand {
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

[Command]
[BoundedContext("TestCommands")]
[Destructive(ConfirmationTitle = "Delete this widget?", ConfirmationBody = "The widget cannot be restored.")]
public class DeleteWidgetCommand {
    public string MessageId { get; set; } = string.Empty;

    public string WidgetId { get; set; } = string.Empty;
}

[Command]
[BoundedContext("TestCommands")]
[CommandTarget(
    typeof(Counter.Domain.CounterProjection),
    CommandTargetResolutionMode.SameAsSource,
    CommandTargetChangeKind.Update,
    ExpectedStatus = "Approved")]
public class SameSourceTargetCommand {
    public string MessageId { get; set; } = string.Empty;

    public int Amount { get; set; }
}

[Command]
[BoundedContext("TestCommands")]
[CommandTarget(
    typeof(Counter.Domain.CounterProjection),
    CommandTargetResolutionMode.Provider,
    CommandTargetChangeKind.Create,
    ViewKey = "Counter:Counter.Domain.CounterProjection")]
public class ProviderTargetCommand {
    public string MessageId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

[Command]
[BoundedContext("TestCommands")]
[CommandTarget(
    typeof(Counter.Domain.CounterProjection),
    CommandTargetResolutionMode.Provider,
    CommandTargetChangeKind.Delete,
    ViewKey = "Counter:Counter.Domain.CounterProjection")]
public class DeleteProviderTargetCommand {
    public string MessageId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

[Command]
[BoundedContext("TestCommands")]
[CommandTarget(
    typeof(Counter.Domain.CounterProjection),
    CommandTargetResolutionMode.Provider,
    CommandTargetChangeKind.Create,
    ViewKey = "Counter:Counter.Domain.CounterProjection")]
public class BlockingCloneProviderTargetCommand {
    private string _messageId = string.Empty;

    public static Action? MessageIdRead { get; set; }

    public string MessageId {
        get {
            MessageIdRead?.Invoke();
            return _messageId;
        }

        set => _messageId = value;
    }

    public string Name { get; set; } = string.Empty;
}

[Command]
[BoundedContext("TestCommands")]
[CommandTarget(
    typeof(Counter.Domain.CounterProjection),
    CommandTargetResolutionMode.Provider,
    CommandTargetChangeKind.Update,
    ViewKey = "Counter:Counter.Domain.CounterProjection",
    ExpectedStatus = "Approved")]
public class ExpectedStatusProviderTargetCommand {
    public string MessageId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

[Command]
[BoundedContext("TestCommands")]
[CommandTarget(
    typeof(Counter.Domain.CounterProjection),
    CommandTargetResolutionMode.Provider,
    CommandTargetChangeKind.StatusMove)]
public class StatusMoveProviderTargetCommand {
    public string MessageId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}
