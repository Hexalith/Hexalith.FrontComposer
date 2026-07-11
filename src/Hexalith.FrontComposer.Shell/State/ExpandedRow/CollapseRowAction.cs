namespace Hexalith.FrontComposer.Shell.State.ExpandedRow;

/// <summary>
/// Story 4-5 D4 / D18 / AC2 / AC6 — dispatched in three situations:
/// (a) the user clicks the currently-expanded row's button (toggle-collapse),
/// (b) the generated view's <c>DisposeAsync</c> fires (route change / unmount),
/// (c) a future Epic 5 reconciliation effect chooses to clear an expansion.
/// The PURE reducer removes the entry for the <see cref="ViewKey"/>; idempotent (no-op when absent).
/// </summary>
public sealed record CollapseRowAction {

    /// <summary>Initializes a new instance of the <see cref="CollapseRowAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key (D22 ephemeral form).</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    public CollapseRowAction(string viewKey) => ViewKey = viewKey;

    /// <summary>Gets the stable per-view key (D22 ephemeral form).</summary>
    public string ViewKey {
        get;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new ArgumentException("View key cannot be null, empty, or whitespace.", nameof(value));
            }

            field = value;
        }
    } = string.Empty;
}
