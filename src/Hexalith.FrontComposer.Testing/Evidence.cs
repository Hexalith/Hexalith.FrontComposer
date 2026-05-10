using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Lifecycle;

namespace Hexalith.FrontComposer.Testing;

/// <summary>
/// Immutable evidence captured when a generated command is dispatched through the fake command service.
/// </summary>
public sealed record CommandDispatchEvidence(
    string CommandType,
    string? TenantId,
    string? UserId,
    string BoundedContext,
    string CommandName,
    string MessageId,
    string CorrelationId,
    string Status,
    IReadOnlyList<CommandLifecycleState> LifecycleStates,
    DateTimeOffset CapturedAtUtc,
    string RedactedPayload);

/// <summary>
/// Immutable evidence captured when a generated projection page is loaded through the fake page loader.
/// </summary>
public sealed record ProjectionPageEvidence(
    string ProjectionTypeFqn,
    int Skip,
    int Take,
    string? TenantId,
    string? UserId,
    string Mode,
    DateTimeOffset CapturedAtUtc);

/// <summary>
/// Immutable evidence captured when the deterministic fault provider simulates reconnection behavior.
/// </summary>
public sealed record FaultInjectionEvidence(
    string Mode,
    string? TenantId,
    string? UserId,
    string CorrelationId,
    DateTimeOffset CapturedAtUtc);

/// <summary>
/// Redacts bounded evidence for assertion messages, logs, and serialized artifacts.
/// </summary>
public static class RedactedEvidenceFormatter
{
    /// <summary>
    /// Serializes an object to a bounded, redacted diagnostic string.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The active test-host options.</param>
    /// <returns>A bounded and redacted diagnostic payload.</returns>
    public static string Format(object? value, FrontComposerTestOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        string payload = value is null ? "<null>" : JsonSerializer.Serialize(value);
        string redacted = payload
            .Replace(options.TestTenantId, "<tenant>", StringComparison.Ordinal)
            .Replace(options.TestUserId, "<user>", StringComparison.Ordinal);

        redacted = RedactKey(redacted, "token");
        redacted = RedactKey(redacted, "secret");
        redacted = RedactKey(redacted, "password");

        if (redacted.Length <= options.MaxDiagnosticPayloadCharacters)
        {
            return redacted;
        }

        return redacted[..options.MaxDiagnosticPayloadCharacters] + "...<truncated>";
    }

    private static string RedactKey(string value, string key)
    {
        int index = value.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        while (index >= 0)
        {
            int colon = value.IndexOf(':', index);
            if (colon < 0)
            {
                return value;
            }

            int end = value.IndexOf(',', colon);
            if (end < 0)
            {
                end = value.IndexOf('}', colon);
            }

            if (end < 0)
            {
                return value[..(colon + 1)] + "\"<redacted>\"";
            }

            value = value[..(colon + 1)] + "\"<redacted>\"" + value[end..];
            index = value.IndexOf(key, colon + 1, StringComparison.OrdinalIgnoreCase);
        }

        return value;
    }
}
