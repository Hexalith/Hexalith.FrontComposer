using System.Text;

using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

namespace Hexalith.FrontComposer.Shell.Services.Authorization;

public enum CommandAuthorizationDecisionKind {
    Allowed,
    Denied,
    Pending,
    FailedClosed,
}

public enum CommandAuthorizationReason {
    None,
    NoPolicy,
    Denied,
    Unauthenticated,
    Pending,
    MissingService,
    MissingPolicy,
    StaleTenantContext,
    Canceled,
    HandlerFailed,
    CatalogInconsistent,
}

public enum CommandAuthorizationSurface {
    DirectDispatch,
    GeneratedForm,
    InlineAction,
    CompactInlineAction,
    FullPage,
    EmptyStateCta,
    CommandPalette,
    HomeCapability,
}

public sealed record CommandAuthorizationRequest(
    Type CommandType,
    string? PolicyName,
    object? Command,
    string? BoundedContext,
    string DisplayLabel,
    CommandAuthorizationSurface SourceSurface = CommandAuthorizationSurface.DirectDispatch) {
    private bool PrintMembers(StringBuilder builder) {
        builder.Append("CommandType = ").Append(CommandType.FullName ?? CommandType.Name)
            .Append(", PolicyName = ").Append(PolicyName ?? "<none>")
            .Append(", Command = <redacted>")
            .Append(", BoundedContext = ").Append(BoundedContext ?? "<none>")
            .Append(", DisplayLabel = ").Append(DisplayLabel)
            .Append(", SourceSurface = ").Append(SourceSurface);
        return true;
    }
}

public sealed record CommandAuthorizationResource(
    Type CommandType,
    string PolicyName,
    string? BoundedContext,
    string DisplayLabel,
    CommandAuthorizationSurface SourceSurface,
    TenantContextSnapshot? TenantContext);

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
