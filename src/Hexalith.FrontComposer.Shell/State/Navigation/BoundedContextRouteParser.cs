namespace Hexalith.FrontComposer.Shell.State.Navigation;

/// <summary>
/// Parses the bounded-context segment from the current shell route so contextual palette scoring
/// can follow browser navigation without persisting the value.
/// </summary>
public static class BoundedContextRouteParser {
    /// <summary>
    /// Extracts the bounded-context route segment from a URI or path.
    /// </summary>
    /// <param name="uriOrPath">The absolute URI or app-relative path.</param>
    /// <returns>
    /// The bounded-context segment when present; otherwise <see langword="null"/> for home and
    /// other non-context routes.
    /// </returns>
    public static string? Parse(string? uriOrPath)
        => TryParse(uriOrPath, out string? boundedContext) ? boundedContext : null;

    /// <summary>Distinguishes an invalid path from a valid route with no bounded context.</summary>
    internal static bool TryParse(string? uriOrPath, out string? boundedContext) {
        boundedContext = null;
        if (string.IsNullOrWhiteSpace(uriOrPath)) {
            return true;
        }

        string input = uriOrPath.TrimStart();
        if (input.Length >= 2 && input[0] is ('/' or '\\') && input[1] is ('/' or '\\')) {
            return false;
        }

        // A rooted app path is already a path. On Unix, parsing it as an absolute URI
        // can produce a file URI and escape the percent signs a second time.
        string path = !input.StartsWith('/') && Uri.TryCreate(input, UriKind.Absolute, out Uri? uri)
            ? uri.AbsolutePath
            : input;

        int suffixIndex = path.IndexOfAny(['?', '#']);
        if (suffixIndex >= 0) {
            path = path[..suffixIndex];
        }

        string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length < 2) {
            return true;
        }

        // A domain landing already belongs to its context. A generated command URL must
        // include both its context and command before it can influence palette scoring.
        string segment = segments[0];
        if (segment.Equals("domain", StringComparison.OrdinalIgnoreCase)) {
            segment = segments[1];
        }
        else if (segment.Equals("commands", StringComparison.OrdinalIgnoreCase)) {
            if (segments.Length < 3) {
                return true;
            }

            segment = segments[1];
        }

        try {
            string decoded = Uri.UnescapeDataString(segment);
            if (decoded.AsSpan().IndexOfAny('/', '\\') >= 0) {
                return false;
            }

            // Stable casing avoids state churn between equivalent route spellings.
            boundedContext = decoded.ToLowerInvariant();
            return true;
        }
        catch (UriFormatException) {
            return false;
        }
    }
}
