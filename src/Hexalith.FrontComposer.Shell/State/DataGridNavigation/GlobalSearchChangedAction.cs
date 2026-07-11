using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Story 4-3 D1 / AC4 — global search change dispatched by <c>FcProjectionGlobalSearch</c>.
/// Reducer writes the query under <see cref="ReservedFilterKeys.SearchKey"/>.
/// </summary>
public sealed record GlobalSearchChangedAction {

    /// <summary>Initializes a new instance of the <see cref="GlobalSearchChangedAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key.</param>
    /// <param name="query">The new search query, or <see langword="null"/> to clear.</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    public GlobalSearchChangedAction(string viewKey, string? query) {
        ViewKey = viewKey;
        Query = query;
    }

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

    /// <summary>Gets the new search query, or <see langword="null"/> when the user cleared the input.</summary>
    public string? Query { get; init; }
}
