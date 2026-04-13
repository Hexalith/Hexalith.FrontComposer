namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Result of a command dispatch operation.
/// </summary>
/// <param name="MessageId">Non-empty ULID string generated on command dispatch.</param>
/// <param name="Status">"Accepted" (command queued) or "Rejected" (domain validation failed).</param>
public record CommandResult(string MessageId, string Status);
