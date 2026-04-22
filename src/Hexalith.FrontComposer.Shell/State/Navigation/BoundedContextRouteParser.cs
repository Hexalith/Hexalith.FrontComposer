namespace Hexalith.FrontComposer.Shell.State.Navigation;

/// <summary>
/// Parses the bounded-context segment from the current shell route so contextual palette scoring
/// can follow browser navigation without persisting the value.
/// </summary>
public static class BoundedContextRouteParser
{
    /// <summary>
    /// Extracts the bounded-context route segment from a URI or path.
    /// </summary>
    /// <param name="uriOrPath">The absolute URI or app-relative path.</param>
    /// <returns>
    /// The bounded-context segment when present; otherwise <see langword="null"/> for home and
    /// other non-context routes.
    /// </returns>
    public static string? Parse(string? uriOrPath)
    {
        if (string.IsNullOrWhiteSpace(uriOrPath))
        {
            return null;
        }

        string path = ExtractPath(uriOrPath);
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        int suffixIndex = path.IndexOfAny(['?', '#']);
        if (suffixIndex >= 0)
        {
            path = path[..suffixIndex];
        }

        string[] segments = [.. path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        if (segments.Length == 0)
        {
            return null;
        }

        // Normalise to lowercase so PascalCase vs kebab-case URLs for the same BC don't churn the
        // navigation-state reducer via Ordinal comparison (edge-hunter DN8 finding).
        // Accept 2-segment /domain/{bc} landing routes so persist/restore stays symmetric — previous
        // ≥3 check silently dropped restorations of BC landing pages.
        if (string.Equals(segments[0], "domain", StringComparison.OrdinalIgnoreCase))
        {
            return segments.Length >= 2 ? segments[1].ToLowerInvariant() : null;
        }

        return segments.Length >= 2 ? segments[0].ToLowerInvariant() : null;
    }

    private static string ExtractPath(string uriOrPath)
        => Uri.TryCreate(uriOrPath, UriKind.Absolute, out Uri? uri)
            ? uri.AbsolutePath
            : uriOrPath;
}