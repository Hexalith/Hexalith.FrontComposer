namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Story 4-4 D4 / AC7 — dispatched by the generated view's <c>@onscroll</c> handler after the
/// JS-side throttle. The PURE reducer validates the scroll offset and updates
/// <c>GridViewSnapshot.ScrollTop</c>; <c>ScrollPersistenceEffect</c> debounces and chains
/// <see cref="CaptureGridStateAction"/>.
/// </summary>
public sealed record ScrollCapturedAction {

    /// <summary>Initializes a new instance of the <see cref="ScrollCapturedAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key.</param>
    /// <param name="scrollTop">Non-negative finite scroll offset (defence-in-depth over <c>GridViewSnapshot.ScrollTop</c>'s own validator).</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <paramref name="scrollTop"/> is NaN, infinity, or negative.</exception>
    public ScrollCapturedAction(string viewKey, double scrollTop) {
        ViewKey = viewKey;
        ScrollTop = scrollTop;
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

    /// <summary>Gets the captured scroll offset.</summary>
    public double ScrollTop {
        get;
        init {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) {
                throw new System.ArgumentOutOfRangeException(nameof(value), value, "ScrollTop must be a non-negative finite value.");
            }

            field = value;
        }
    }
}
