namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Circuit-local state for Story 5-5 new-item indicators.</summary>
public interface INewItemIndicatorStateService : IDisposable {
    /// <summary>
    /// Subscribes to effective indicator-state mutations for one generated view lane.
    /// </summary>
    /// <param name="viewKey">The canonical view key whose mutations should be observed.</param>
    /// <param name="handler">The callback invoked once after each effective mutation affecting the view.</param>
    /// <returns>A disposable subscription. Disposing it idempotently stops future callbacks.</returns>
    /// <remarks>
    /// The default implementation is inert so existing custom implementations remain source and binary compatible.
    /// Initial state is not replayed; consumers read <see cref="Snapshot(string)"/> during their initial render.
    /// </remarks>
    IDisposable Subscribe(string viewKey, Action handler) {
        ArgumentException.ThrowIfNullOrWhiteSpace(viewKey);
        ArgumentNullException.ThrowIfNull(handler);
        return System.Reactive.Disposables.Disposable.Empty;
    }

    void Add(NewItemIndicatorEntry entry);

    IReadOnlyList<NewItemIndicatorEntry> Snapshot(string viewKey);

    void DismissForFilterChange(string viewKey);

    void DismissMaterialized(string viewKey, string entityKey);

    void Clear(string reason);
}
