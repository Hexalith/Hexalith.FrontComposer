namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Transient indicator for a confirmed created entity that is relevant to a lane but outside current filters.</summary>
public sealed record NewItemIndicatorEntry(
    string ViewKey,
    string EntityKey,
    string MessageId,
    DateTimeOffset CreatedAt);
