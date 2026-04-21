using System.Globalization;
using System.Text;

namespace Hexalith.FrontComposer.Shell.Routing;

/// <summary>
/// Pure helpers for the canonical command-route URL contract (Story 3-4 D21).
/// </summary>
/// <remarks>
/// Lives under <c>Routing/</c> rather than co-located with <c>ShortcutBinding</c> because kebab
/// routing is a command-routing concern, NOT a keyboard-shortcut concern (D21 post-elicitation
/// rationale).
/// </remarks>
public static class CommandRouteBuilder
{
    /// <summary>
    /// Converts a PascalCase identifier into kebab-case (lowercase, hyphen-separated).
    /// </summary>
    /// <remarks>
    /// Inserts a hyphen before each uppercase letter that follows a lowercase letter or digit; runs
    /// of consecutive uppercase letters stay together until a lowercase letter appears (so
    /// <c>"XMLParser"</c> becomes <c>"xml-parser"</c>). Fully qualified type names are reduced to
    /// their final segment before kebab-casing.
    /// </remarks>
    /// <param name="pascalCase">The input identifier (e.g., <c>"SubmitOrderCommand"</c>).</param>
    /// <returns>The kebab-cased form (e.g., <c>"submit-order-command"</c>).</returns>
    public static string KebabCase(string pascalCase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pascalCase);

        StringBuilder builder = new(pascalCase.Length + 8);
        for (int i = 0; i < pascalCase.Length; i++)
        {
            char ch = pascalCase[i];
            if (ch == '.')
            {
                // Drop namespace separators — only the type name segment is the URL slug.
                builder.Clear();
                continue;
            }

            if (char.IsUpper(ch))
            {
                bool prevIsLower = i > 0 && char.IsLower(pascalCase[i - 1]);
                bool prevIsDigit = i > 0 && char.IsDigit(pascalCase[i - 1]);
                bool prevIsUpper = i > 0 && char.IsUpper(pascalCase[i - 1]);
                bool nextIsLower = i + 1 < pascalCase.Length && char.IsLower(pascalCase[i + 1]);
                bool boundary = prevIsLower || prevIsDigit || (prevIsUpper && nextIsLower);
                if (boundary && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(char.ToLower(ch, CultureInfo.InvariantCulture));
            }
            else
            {
                builder.Append(char.ToLower(ch, CultureInfo.InvariantCulture));
            }
        }

        string kebab = builder.ToString();
        if (string.IsNullOrWhiteSpace(kebab))
        {
            throw new ArgumentException($"KebabCase produced an empty slug from '{pascalCase}'.", nameof(pascalCase));
        }

        return kebab;
    }

    /// <summary>
    /// Builds the canonical command route URL.
    /// </summary>
    /// <param name="boundedContext">The bounded-context name.</param>
    /// <param name="commandTypeName">The fully qualified command type name.</param>
    /// <returns>A URL of the form <c>/domain/{kebab-bc}/{kebab-cmd}</c>.</returns>
    public static string BuildRoute(string boundedContext, string commandTypeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(boundedContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandTypeName);
        return $"/domain/{KebabCase(boundedContext)}/{KebabCase(commandTypeName)}";
    }

    /// <summary>
    /// Returns whether the URL is a safe internal navigation target (Story 3-4 D10 hydrate-time
    /// route-safety filter).
    /// </summary>
    /// <param name="url">Candidate URL string.</param>
    /// <returns><see langword="true"/> when the URL is a safe internal route; <see langword="false"/> otherwise.</returns>
    public static bool IsInternalRoute(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        // P8 (2026-04-21 pass-4): reject control characters (CR/LF/tab) that can enable header
        // injection if the URL is later serialised into a Set-Cookie / Location header. Must run
        // BEFORE Uri.UnescapeDataString so an encoded \r\n (e.g. "%0D%0A") is not decoded into a
        // real newline and then passed downstream.
        if (url.AsSpan().IndexOfAny('\r', '\n', '\t') >= 0)
        {
            return false;
        }

        // P7 (2026-04-21 pass-4): decode percent-encoded scheme tokens before the filter so
        // "/redirect?next=%68ttps://evil.com" (where %68 == 'h') does not slip through the literal
        // substring check below.
        string decoded;
        try
        {
            decoded = Uri.UnescapeDataString(url);
        }
        catch (UriFormatException)
        {
            // Malformed percent encoding — treat as untrusted.
            return false;
        }

        if (ContainsEmbeddedScheme(decoded))
        {
            return false;
        }

        // Re-check control chars on the decoded form as well (belt + braces).
        if (decoded.AsSpan().IndexOfAny('\r', '\n', '\t') >= 0)
        {
            return false;
        }

        // Reject Windows-style backslash and absolute URLs (any scheme).
        if (url.Contains('\\', StringComparison.Ordinal) || decoded.Contains('\\', StringComparison.Ordinal))
        {
            return false;
        }

        // Must start with a single slash.
        if (!url.StartsWith('/'))
        {
            return false;
        }

        // Reject protocol-relative // prefix.
        return !url.StartsWith("//", StringComparison.Ordinal);
    }

    // Open-redirect defence (D10 tampered-URL filter): reject URLs containing any of the known
    // dangerous scheme tokens, INCLUDING in query or fragment (e.g., /redirect?next=https://evil).
    // Accepting scheme tokens in query/fragment would let the palette re-hydrate a tampered recent
    // entry that navigates the user off-origin via a legitimate-looking internal path.
    private static bool ContainsEmbeddedScheme(string url)
        => url.Contains("http:", StringComparison.OrdinalIgnoreCase)
            || url.Contains("https:", StringComparison.OrdinalIgnoreCase)
            || url.Contains("javascript:", StringComparison.OrdinalIgnoreCase)
            || url.Contains("data:", StringComparison.OrdinalIgnoreCase)
            || url.Contains("mailto:", StringComparison.OrdinalIgnoreCase);
}
