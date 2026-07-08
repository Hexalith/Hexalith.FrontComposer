using System.Text.Json.Serialization;

namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Result of a command dispatch operation.
/// </summary>
/// <param name="MessageId">Non-empty ULID string generated on command dispatch.</param>
/// <param name="Status"><see cref="CommandResultStatus.Accepted"/> (command queued) or <see cref="CommandResultStatus.Rejected"/> (domain validation failed).</param>
/// <param name="CorrelationId">Optional server correlation identifier returned by EventStore.</param>
/// <param name="Location">Optional status-resource location returned by EventStore.</param>
/// <param name="RetryAfter">Optional server retry hint returned by EventStore.</param>
public record CommandResult(
    [property: JsonPropertyName("messageId")]
    string MessageId,
    [property: JsonPropertyName("status")]
    string Status,
    [property: JsonPropertyName("correlationId")]
    string? CorrelationId = null,
    [property: JsonPropertyName("location")]
    Uri? Location = null,
    [property: JsonPropertyName("retryAfter")]
    TimeSpan? RetryAfter = null);
