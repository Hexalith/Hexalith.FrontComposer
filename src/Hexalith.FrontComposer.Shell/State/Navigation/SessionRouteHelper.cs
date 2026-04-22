using Hexalith.FrontComposer.Shell.Routing;

using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.State.Navigation;

/// <summary>
/// Normalises persisted session routes so they remain internal to the current Blazor base path.
/// </summary>
internal static class SessionRouteHelper {
    /// <summary>
    /// Converts the current browser location to a base-relative internal route suitable for
    /// persistence.
    /// </summary>
    /// <param name="navigation">The active navigation manager.</param>
    /// <returns>The base-relative route, or <see langword="null"/> when the current URI is not a safe internal route.</returns>
    public static string? NormalizeCurrentRoute(NavigationManager navigation) {
        ArgumentNullException.ThrowIfNull(navigation);
        return TryNormalizeBaseRelative(navigation.ToBaseRelativePath(navigation.Uri), out string route)
            ? route
            : null;
    }

    /// <summary>
    /// Validates a persisted route candidate and converts it to a base-relative form usable with
    /// <see cref="NavigationManager.NavigateTo(string, bool)"/>.
    /// </summary>
    /// <param name="candidate">The persisted route candidate.</param>
    /// <param name="navigation">The current navigation manager, required for absolute-URI validation.</param>
    /// <param name="normalizedRoute">The resulting base-relative route when validation succeeds.</param>
    /// <returns><see langword="true"/> when the route is safe and base-relative; otherwise <see langword="false"/>.</returns>
    public static bool TryNormalizePersistedRoute(
        string? candidate,
        NavigationManager? navigation,
        out string normalizedRoute) {
        normalizedRoute = string.Empty;
        if (string.IsNullOrWhiteSpace(candidate)) {
            return false;
        }

        string working = candidate.Trim();
        if (Uri.TryCreate(working, UriKind.Absolute, out Uri? absolute)) {
            if (navigation is null
                || !Uri.TryCreate(navigation.BaseUri, UriKind.Absolute, out Uri? baseUri)
                || !baseUri.IsBaseOf(absolute)) {
                return false;
            }

            try {
                working = navigation.ToBaseRelativePath(absolute.ToString());
            }
            catch (ArgumentException) {
                return false;
            }
        }

        return TryNormalizeBaseRelative(working, out normalizedRoute);
    }

    private static bool TryNormalizeBaseRelative(string? candidate, out string normalizedRoute) {
        normalizedRoute = string.Empty;
        if (string.IsNullOrWhiteSpace(candidate)) {
            return false;
        }

        string working = candidate.Trim();
        if (working.StartsWith("/", StringComparison.Ordinal)) {
            working = working[1..];
        }

        if (working.Length == 0 || !CommandRouteBuilder.IsInternalRoute("/" + working)) {
            return false;
        }

        normalizedRoute = working;
        return true;
    }
}
