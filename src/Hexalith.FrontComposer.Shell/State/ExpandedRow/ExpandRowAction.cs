namespace Hexalith.FrontComposer.Shell.State.ExpandedRow;

/// <summary>
/// Story 4-5 D2 / D4 / AC2 — dispatched by the generated view's <c>HandleRowClickAsync</c>
/// when the user clicks a not-currently-expanded row's expand button. The PURE reducer
/// REPLACES any existing entry for the <see cref="ViewKey"/>, enforcing the single-expand
/// invariant at the reducer level (the view never dispatches an explicit collapse before
/// expanding a different row).
/// </summary>
/// <remarks>
/// <para>
/// <b>D22 ephemeral-key contract.</b> The <see cref="ViewKey"/> for ephemeral feature
/// dispatches uses the per-component-instance suffix form
/// <c>{boundedContext}:{projectionTypeFqn}:{ComponentInstanceId}</c>, distinct from the
/// persisted view-key consumed by Story 3-6 / 4-3 / 4-4. The reducer treats the suffix
/// as opaque — only equality matters for the dictionary key.
/// </para>
/// </remarks>
public sealed record ExpandRowAction {

    /// <summary>Initializes a new instance of the <see cref="ExpandRowAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key (D22 ephemeral form; non-empty).</param>
    /// <param name="itemKey">The boxed <see cref="object"/> identity returned by the generated view's <c>_itemKeyAccessor(row)</c> (Story 4-4 D5 / D13).</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="itemKey"/> is null.</exception>
    public ExpandRowAction(string viewKey, object itemKey) {
        ViewKey = viewKey;
        ItemKey = itemKey;
    }

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

    /// <summary>Gets the boxed item identity for the row to expand.</summary>
    public object ItemKey {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = default!;
}
