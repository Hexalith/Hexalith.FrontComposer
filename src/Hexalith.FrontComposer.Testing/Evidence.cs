using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

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
public static class RedactedEvidenceFormatter {
    private static readonly JsonSerializerOptions RedactedJsonOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private static readonly string[] SensitiveKeyFragments = ["token", "secret", "password"];

    /// <summary>
    /// Serializes an object to a bounded, redacted diagnostic string.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The active test-host options.</param>
    /// <returns>A bounded and redacted diagnostic payload.</returns>
    public static string Format(object? value, FrontComposerTestOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        string redacted = value is null
            ? "<null>"
            : RedactNode(JsonSerializer.SerializeToNode(value))?.ToJsonString(RedactedJsonOptions) ?? "null";

        // Replace configured tenant/user identifiers across the whole payload, including JSON
        // property names (for example dictionary keys), so the identifiers cannot leak through
        // object keys that structural node traversal never inspects as values.
        redacted = RedactConfiguredValues(redacted, options);

        if (redacted.Length <= options.MaxDiagnosticPayloadCharacters) {
            return redacted;
        }

        return redacted[..options.MaxDiagnosticPayloadCharacters] + "...<truncated>";
    }

    private static JsonNode? RedactNode(JsonNode? node, string? propertyName = null) {
        if (node is null) {
            return null;
        }

        if (IsSensitiveKey(propertyName)) {
            return JsonValue.Create("<redacted>");
        }

        if (node is JsonObject obj) {
            foreach (string key in obj.Select(property => property.Key).ToArray()) {
                JsonNode? current = obj[key];
                JsonNode? redacted = RedactNode(current, key);
                if (!ReferenceEquals(current, redacted)) {
                    obj[key] = redacted;
                }
            }

            return obj;
        }

        if (node is JsonArray array) {
            for (int i = 0; i < array.Count; i++) {
                JsonNode? current = array[i];
                JsonNode? redacted = RedactNode(current);
                if (!ReferenceEquals(current, redacted)) {
                    array[i] = redacted;
                }
            }

            return array;
        }

        return node;
    }

    private static bool IsSensitiveKey(string? key)
        => key is not null && SensitiveKeyFragments.Any(fragment => key.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    private static string RedactConfiguredValues(string value, FrontComposerTestOptions options)
        => value
            .Replace(options.TestTenantId, "<tenant>", StringComparison.Ordinal)
            .Replace(options.TestUserId, "<user>", StringComparison.Ordinal);
}
