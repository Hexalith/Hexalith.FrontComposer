namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Story 4-4 D6 / AC5 — dispatched from the "Reset to defaults" anchor inside the prioritizer popover.
/// Reducer removes the <c>"__hidden"</c> entry from <c>GridViewSnapshot.Filters</c>; the persistence
/// effect chains <see cref="CaptureGridStateAction"/>.
/// </summary>
public sealed record ResetColumnVisibilityAction {

    /// <summary>Initializes a new instance of the <see cref="ResetColumnVisibilityAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key.</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    public ResetColumnVisibilityAction(string viewKey) => ViewKey = viewKey;

    /// <summary>Gets the stable per-view key.</summary>
    public string ViewKey {
        get;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new System.ArgumentException("View key cannot be null, empty, or whitespace.", nameof(value));
            }

            field = value;
        }
    } = string.Empty;
}
