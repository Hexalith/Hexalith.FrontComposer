using System.Text;

using Hexalith.FrontComposer.Contracts.Routing;

namespace Hexalith.FrontComposer.Shell.Routing;

/// <summary>
/// Pure helpers for the canonical command-route URL contract (Story 3-4 D21).
/// </summary>
/// <remarks>
/// Lives under <c>Routing/</c> rather than co-located with <c>ShortcutBinding</c> because kebab
/// routing is a command-routing concern, NOT a keyboard-shortcut concern (D21 post-elicitation
/// rationale).
/// </remarks>
public static class CommandRouteBuilder {
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
    /// <exception cref="ArgumentException">The identifier contains whitespace, empty segments, or invalid Unicode.</exception>
    public static string KebabCase(string pascalCase) {
        ArgumentException.ThrowIfNullOrWhiteSpace(pascalCase);
        if (pascalCase.StartsWith('.') || pascalCase.EndsWith('.')
            || pascalCase.Contains("..", StringComparison.Ordinal) || pascalCase.Any(char.IsWhiteSpace)) {
            throw new ArgumentException("The identifier must have nonempty segments without whitespace.", nameof(pascalCase));
        }

        int typeStart = pascalCase.LastIndexOf('.') + 1;
        StringBuilder builder = new(pascalCase.Length - typeStart);
        Span<char> encodedRune = stackalloc char[2];
        Rune previous = default;
        int index = typeStart;
        while (index < pascalCase.Length) {
            if (!Rune.TryGetRuneAt(pascalCase, index, out Rune current)) {
                throw new ArgumentException("The identifier contains invalid Unicode.", nameof(pascalCase));
            }

            index += current.Utf16SequenceLength;
            if (Rune.IsUpper(current)
                && (Rune.IsLower(previous) || Rune.IsDigit(previous)
                    || (Rune.IsUpper(previous) && index < pascalCase.Length
                        && Rune.TryGetRuneAt(pascalCase, index, out Rune next) && Rune.IsLower(next)))) {
                _ = builder.Append('-');
            }

            int length = Rune.ToLowerInvariant(current).EncodeToUtf16(encodedRune);
            _ = builder.Append(encodedRune[..length]);
            previous = current;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Builds the canonical command route URL.
    /// </summary>
    /// <param name="boundedContext">The bounded-context name.</param>
    /// <param name="commandTypeName">The fully qualified command type name.</param>
    /// <returns>A URL of the form <c>/commands/{BoundedContext}/{CommandTypeName}</c>.</returns>
    public static string BuildRoute(string? boundedContext, string commandTypeName) {
        return GeneratedCommandRoute.Build(boundedContext, commandTypeName);
    }

    /// <summary>
    /// Returns whether the URL is a safe internal navigation target (Story 3-4 D10 hydrate-time
    /// route-safety filter).
    /// </summary>
    /// <param name="url">Candidate URL string.</param>
    /// <returns><see langword="true"/> when the URL is a safe internal route; <see langword="false"/> otherwise.</returns>
    public static bool IsInternalRoute(string? url) {
        if (string.IsNullOrWhiteSpace(url)) {
            return false;
        }

        // P8 (2026-04-21 pass-4): reject control characters (CR/LF/tab) that can enable header
        // injection if the URL is later serialised into a Set-Cookie / Location header. Must run
        // BEFORE Uri.UnescapeDataString so an encoded \r\n (e.g. "%0D%0A") is not decoded into a
        // real newline and then passed downstream.
        if (url.AsSpan().IndexOfAny('\r', '\n', '\t') >= 0) {
            return false;
        }

        // P7 (2026-04-21 pass-4): decode percent-encoded scheme tokens before the filter so
        // "/redirect?next=%68ttps://evil.com" (where %68 == 'h') does not slip through the literal
        // substring check below.
        string decoded;
        string decodedPath;
        try {
            decoded = Uri.UnescapeDataString(url);
            int pathEnd = url.AsSpan().IndexOfAny('?', '#');
            decodedPath = pathEnd < 0 ? decoded : Uri.UnescapeDataString(url[..pathEnd]);
        }
        catch (UriFormatException) {
            // Malformed percent encoding — treat as untrusted.
            return false;
        }

        if (ContainsEmbeddedScheme(decoded)) {
            return false;
        }

        // Re-check control chars on the decoded form as well (belt + braces).
        if (decoded.AsSpan().IndexOfAny('\r', '\n', '\t') >= 0) {
            return false;
        }

        // Reject Windows-style backslash and absolute URLs (any scheme).
        if (url.Contains('\\', StringComparison.Ordinal) || decoded.Contains('\\', StringComparison.Ordinal)) {
            return false;
        }

        // Reject Unicode lookalikes for slash and backslash in the path.
        // Query and fragment values can legitimately contain these characters as text.
        return url.StartsWith('/')
            && !decodedPath.StartsWith("//", StringComparison.Ordinal)
            && decodedPath.AsSpan().IndexOfAny("\u2044\u2215\uFF0F\uFF3C\uFE68") < 0;
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
