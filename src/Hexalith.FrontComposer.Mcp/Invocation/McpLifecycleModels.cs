using System.Text.Json.Nodes;

using Hexalith.FrontComposer.Contracts.Lifecycle;

namespace Hexalith.FrontComposer.Mcp.Invocation;

internal enum McpTerminalOutcomeKind {
    None,
    Confirmed,
    Rejected,
    IdempotentConfirmed,
    TimedOut,
    NeedsReview,
}

internal sealed record McpLifecycleSubscription(string Tool, string Uri, int RetryAfterMs) {
    public JsonObject ToJson() => new() {
        ["tool"] = Tool,
        ["uri"] = Uri,
        ["retryAfterMs"] = RetryAfterMs,
    };
}

internal sealed record McpCommandAcknowledgement(
    string MessageId,
    string CorrelationId,
    CommandLifecycleState State,
    McpLifecycleSubscription Lifecycle) {
    public JsonObject ToJson() => new() {
        ["state"] = State.ToString(),
        ["messageId"] = MessageId,
        ["correlationId"] = CorrelationId,
        ["lifecycle"] = Lifecycle.ToJson(),
    };
}

internal sealed record McpLifecycleTransitionDto(
    long Sequence,
    CommandLifecycleState State,
    string? MessageId,
    DateTimeOffset ObservedAtUtc,
    bool IdempotencyResolved) {
    public JsonObject ToJson() {
        JsonObject json = new() {
            ["sequence"] = Sequence,
            ["state"] = State.ToString(),
            ["observedAtUtc"] = ObservedAtUtc.ToString("O"),
            ["idempotencyResolved"] = IdempotencyResolved,
        };
        if (!string.IsNullOrWhiteSpace(MessageId)) {
            json["messageId"] = MessageId;
        }

        return json;
    }
}

internal sealed record McpRejectionPayload(
    string ErrorCode,
    string? EntityId,
    string Message,
    string DataImpact,
    string SuggestedAction,
    bool RetryAppropriate,
    string ReasonCategory,
    string? DocsCode = null) {
    public JsonObject ToJson() {
        JsonObject json = new() {
            ["errorCode"] = ErrorCode,
            ["message"] = Message,
            ["dataImpact"] = DataImpact,
            ["suggestedAction"] = SuggestedAction,
            ["retryAppropriate"] = RetryAppropriate,
            ["reasonCategory"] = ReasonCategory,
        };
        if (!string.IsNullOrWhiteSpace(EntityId)) {
            json["entityId"] = EntityId;
        }

        if (!string.IsNullOrWhiteSpace(DocsCode)) {
            json["docsCode"] = DocsCode;
        }

        return json;
    }
}

internal sealed record McpSuccessPayload(string Message, string DataImpact) {
    public JsonObject ToJson() => new() {
        ["message"] = Message,
        ["dataImpact"] = DataImpact,
    };
}

internal sealed record McpTerminalOutcome(
    McpTerminalOutcomeKind Kind,
    McpRejectionPayload? Rejection = null,
    McpSuccessPayload? SuccessPayload = null) {
    public static McpTerminalOutcome Success()
        => new(
            McpTerminalOutcomeKind.Confirmed,
            SuccessPayload: new McpSuccessPayload(
                "Command completed: the requested change was applied.",
                "The change has been applied."));

    public static McpTerminalOutcome IdempotentSuccess(string? entityLabel) {
        string label = string.IsNullOrWhiteSpace(entityLabel) ? "item" : entityLabel.Trim();
        return new McpTerminalOutcome(
            McpTerminalOutcomeKind.IdempotentConfirmed,
            SuccessPayload: new McpSuccessPayload(
                $"This {label} was already updated (by another caller). No action needed.",
                "No further changes are required."));
    }

    public static McpTerminalOutcome GenericRejection()
        => new(
            McpTerminalOutcomeKind.Rejected,
            Rejection: new McpRejectionPayload(
                "COMMAND_REJECTED",
                EntityId: null,
                Message: "Command failed: the command was rejected by domain rules. No changes were applied.",
                DataImpact: "No changes were applied.",
                SuggestedAction: "abort",
                RetryAppropriate: false,
                ReasonCategory: "domain_conflict",
                DocsCode: "HFC-MCP-COMMAND-REJECTED"));

    // D2.1 (option a): synthetic terminals carry a structured remediation payload so the
    // agent-visible envelope satisfies AC6's structured-rejection contract. state="Rejected"
    // is preserved (no cross-package CommandLifecycleState change); outcome.category remains the
    // authoritative disambiguator between domain rejection and bounded failure category.
    public static McpTerminalOutcome TimedOut()
        => new(
            McpTerminalOutcomeKind.TimedOut,
            Rejection: new McpRejectionPayload(
                "LIFECYCLE_TIMED_OUT",
                EntityId: null,
                Message: "Command outcome unconfirmed: the lifecycle timed out before the framework observed a terminal state. Data impact is unknown.",
                DataImpact: "Outcome is unconfirmed at the framework boundary.",
                SuggestedAction: "retry",
                RetryAppropriate: true,
                ReasonCategory: "timed_out",
                DocsCode: "HFC-MCP-LIFECYCLE-TIMED-OUT"));

    public static McpTerminalOutcome NeedsReview()
        => new(
            McpTerminalOutcomeKind.NeedsReview,
            Rejection: new McpRejectionPayload(
                "LIFECYCLE_NEEDS_REVIEW",
                EntityId: null,
                Message: "Command outcome unconfirmed: the lifecycle handle was retained for review after eviction. Data impact is unknown.",
                DataImpact: "Outcome is unconfirmed at the framework boundary.",
                SuggestedAction: "poll",
                RetryAppropriate: true,
                ReasonCategory: "needs_review",
                DocsCode: "HFC-MCP-LIFECYCLE-NEEDS-REVIEW"));

    public JsonObject ToJson() {
        JsonObject json = new() {
            ["category"] = ToCategory(Kind),
            ["retryAppropriate"] = Kind is not McpTerminalOutcomeKind.Confirmed and not McpTerminalOutcomeKind.IdempotentConfirmed,
        };
        if (Rejection is not null) {
            json["rejection"] = Rejection.ToJson();
        }

        if (SuccessPayload is not null) {
            json["success"] = SuccessPayload.ToJson();
        }

        return json;
    }

    private static string ToCategory(McpTerminalOutcomeKind kind)
        => kind switch {
            McpTerminalOutcomeKind.Confirmed => "confirmed",
            McpTerminalOutcomeKind.Rejected => "rejected",
            McpTerminalOutcomeKind.IdempotentConfirmed => "idempotent_confirmed",
            McpTerminalOutcomeKind.TimedOut => "timed_out",
            McpTerminalOutcomeKind.NeedsReview => "needs_review",
            _ => "none",
        };
}

internal sealed record McpLifecycleSnapshot(
    string MessageId,
    string CorrelationId,
    CommandLifecycleState State,
    bool Terminal,
    McpTerminalOutcome? Outcome,
    IReadOnlyList<McpLifecycleTransitionDto> Transitions,
    int RetryAfterMs,
    int MaxLongPollMs,
    bool HistoryTruncated) {
    public JsonObject ToJson() {
        // Sequence is monotonically assigned under the entry's _gate, so insertion order already
        // reflects sort order; the explicit OrderBy is a defensive guarantee for AC21.
        JsonArray transitions = [];
        foreach (McpLifecycleTransitionDto transition in Transitions.OrderBy(t => t.Sequence)) {
            transitions.Add(transition.ToJson());
        }

        JsonObject retry = new() {
            ["retryAfterMs"] = RetryAfterMs,
            ["maxLongPollMs"] = MaxLongPollMs,
        };

        JsonObject json = new() {
            ["state"] = State.ToString(),
            ["terminal"] = Terminal,
            ["messageId"] = MessageId,
            ["correlationId"] = CorrelationId,
            ["retry"] = retry,
            ["transitions"] = transitions,
            ["historyTruncated"] = HistoryTruncated,
        };
        if (Outcome is not null) {
            json["outcome"] = Outcome.ToJson();
        }

        return json;
    }
}
