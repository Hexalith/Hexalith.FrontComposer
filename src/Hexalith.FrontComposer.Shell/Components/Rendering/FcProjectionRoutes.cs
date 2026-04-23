namespace Hexalith.FrontComposer.Shell.Components.Rendering;

/// <summary>
/// Story 4-1 T4.3 / D11 / H2 — indirection helper for StatusOverview click-through
/// navigation URLs. Kept in the Shell assembly so Story 4.3 can change the filter
/// query-string encoding (e.g., <c>?filter=status:X</c> → <c>?q=eq(status,X)</c>)
/// in one place without forcing every generated StatusOverview view to re-emit.
/// </summary>
public static class FcProjectionRoutes {
    /// <summary>
    /// Returns the navigation URL that Story 4.3 will resolve into a filtered-DataGrid
    /// destination. Signature round 4 (per Winston) — <see cref="Enum"/> rather than
    /// <see cref="object"/> so <c>[Flags]</c> combined values and nullable-enum unboxing
    /// mishaps fail at compile time. The status value passes through
    /// <see cref="Uri.EscapeDataString(string)"/> so an enum whose <c>ToString()</c> contains a space
    /// (e.g., <c>Pending, Submitted</c> from a <c>[Flags]</c> combination) does not
    /// corrupt the URL.
    /// </summary>
    /// <param name="bcRoute">The bounded-context route prefix (e.g. <c>"/orders"</c>). Must not be <see langword="null"/>.</param>
    /// <param name="statusValue">The enum status value to filter on. Must not be <see langword="null"/>.</param>
    /// <returns>The navigation URL.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bcRoute"/> or <paramref name="statusValue"/> is null.</exception>
    public static string StatusFilter(string bcRoute, Enum statusValue) {
        ArgumentNullException.ThrowIfNull(bcRoute);
        ArgumentNullException.ThrowIfNull(statusValue);

        return bcRoute + "?filter=status:" + Uri.EscapeDataString(statusValue.ToString());
    }
}
