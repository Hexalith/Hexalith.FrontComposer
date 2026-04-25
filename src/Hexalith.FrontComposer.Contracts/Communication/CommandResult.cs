namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Result of a command dispatch operation.
/// </summary>
/// <param name="MessageId">Non-empty ULID string generated on command dispatch.</param>
/// <param name="Status">"Accepted" (command queued) or "Rejected" (domain validation failed).</param>
/// <param name="CorrelationId">Optional server correlation identifier returned by EventStore.</param>
/// <param name="Location">Optional status-resource location returned by EventStore.</param>
/// <param name="RetryAfter">Optional server retry hint returned by EventStore.</param>
public record CommandResult(
    string MessageId,
    string Status,
    string? CorrelationId = null,
    Uri? Location = null,
    TimeSpan? RetryAfter = null);
