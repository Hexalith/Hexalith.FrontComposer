namespace Hexalith.FrontComposer.Shell.Services.Authorization;

public sealed record CommandAuthorizationDecision {
    private CommandAuthorizationDecision(
        CommandAuthorizationDecisionKind kind,
        CommandAuthorizationReason reason,
        string correlationId) {
        Kind = kind;
        Reason = reason;
        CorrelationId = correlationId;
    }

    public CommandAuthorizationDecisionKind Kind { get; }

    public CommandAuthorizationReason Reason { get; }

    public string CorrelationId { get; }

    public bool IsAllowed => Kind == CommandAuthorizationDecisionKind.Allowed;

    public static CommandAuthorizationDecision Allowed(
        string correlationId,
        CommandAuthorizationReason reason = CommandAuthorizationReason.None)
        => new(CommandAuthorizationDecisionKind.Allowed, reason, correlationId);

    public static CommandAuthorizationDecision Denied(string correlationId)
        => new(CommandAuthorizationDecisionKind.Denied, CommandAuthorizationReason.Denied, correlationId);

    public static CommandAuthorizationDecision Pending(string correlationId)
        => new(CommandAuthorizationDecisionKind.Pending, CommandAuthorizationReason.Pending, correlationId);

    public static CommandAuthorizationDecision Blocked(CommandAuthorizationReason reason, string correlationId)
        => new(CommandAuthorizationDecisionKind.FailedClosed, reason, correlationId);
}
