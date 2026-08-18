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

    /// <summary>
    /// Publishes one fresh-row indicator for the entry's <c>(ViewKey, EntityKey)</c> row.
    /// </summary>
    /// <param name="entry">The candidate indicator; its view and entity keys must be non-empty.</param>
    /// <remarks>
    /// <para>
    /// A row is active for exactly as long as it is a member of the implementation's active-entry set,
    /// and the first publication to enter that set owns it. While the row is active, a later call for
    /// the same <c>(ViewKey, EntityKey)</c> is suppressed rather than applied: the incumbent entry's
    /// <see cref="NewItemIndicatorEntry.MessageId"/>, <see cref="NewItemIndicatorEntry.CreatedAt"/>,
    /// and remaining lifetime are left unchanged, and the suppressed call raises no change
    /// notification for that view.
    /// </para>
    /// <para>
    /// The winner is whichever call enters the implementation's critical section first, not the one
    /// carrying the earliest <see cref="NewItemIndicatorEntry.CreatedAt"/>. Producers decide
    /// eligibility and publish outside their own locks, so decision order and arrival order can differ.
    /// </para>
    /// <para>
    /// The row re-opens as soon as the active entry leaves the set — through lifetime expiry,
    /// <see cref="DismissMaterialized(string, string)"/>, <see cref="DismissForFilterChange(string)"/>,
    /// <see cref="Clear(string)"/>, or a tenant/user scope transition. A later call for that row is
    /// then accepted normally and starts a fresh lifetime.
    /// </para>
    /// </remarks>
    void Add(NewItemIndicatorEntry entry);

    IReadOnlyList<NewItemIndicatorEntry> Snapshot(string viewKey);

    void DismissForFilterChange(string viewKey);

    void DismissMaterialized(string viewKey, string entityKey);

    void Clear(string reason);
}
