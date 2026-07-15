namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Circuit-local state for Story 5-5 new-item indicators.</summary>
public interface INewItemIndicatorStateService : IDisposable {
    void Add(NewItemIndicatorEntry entry);

    IReadOnlyList<NewItemIndicatorEntry> Snapshot(string viewKey);

    void DismissForFilterChange(string viewKey);

    void DismissMaterialized(string viewKey, string entityKey);

    void Clear(string reason);
}
