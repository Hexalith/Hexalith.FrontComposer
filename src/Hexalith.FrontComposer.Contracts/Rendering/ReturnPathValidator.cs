namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Validates <see cref="ICommandPageContext.ReturnPath"/> values against the Story 2-2 D32
/// open-redirect defense. A return path is safe when it is a relative URL that cannot be
/// reinterpreted as an absolute URL by the browser or the .NET URI parser, does not escape
/// the application base via path-traversal, and does not smuggle user-display-spoofing
/// Unicode code points.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Uri.IsWellFormedUriString(string?, UriKind)"/> with <see cref="UriKind.Relative"/>
/// alone is INSUFFICIENT — it accepts protocol-relative URLs such as <c>//evil.example</c> which
/// browsers resolve to <c>https://evil.example</c>, percent-encoded slash bypasses such as
/// <c>/%2f/evil.example</c> which some routers re-decode post-validation, and BiDi-override
/// code points that render one target while resolving another.
/// </para>
/// <para>
/// The validator is the single source of truth for return-path validation; renderers and any
/// future page-context implementations MUST funnel through <see cref="IsSafeRelativePath(string?)"/>
/// rather than re-implementing the check inline.
/// </para>
/// </remarks>
public static class ReturnPathValidator {
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="returnPath"/> is safe to navigate to —
    /// i.e. it is a non-empty, well-formed relative URL that cannot be reinterpreted as an
    /// absolute URL, cannot escape the app base via path-traversal, and contains no
    /// display-spoofing Unicode.
    /// </summary>
    /// <param name="returnPath">Candidate return path. <see langword="null"/>, empty, or whitespace returns <see langword="false"/>.</param>
    /// <returns><see langword="true"/> when safe; <see langword="false"/> otherwise.</returns>
    public static bool IsSafeRelativePath(string? returnPath) {
        if (string.IsNullOrWhiteSpace(returnPath)) {
            return false;
        }

        string path = returnPath!;

        // Reject protocol-relative URLs (//host) and backslash variants that some browsers treat
        // as path separators on Windows-derived parsers.
        if (HasProtocolRelativePrefix(path)) {
            return false;
        }

        // Reject any form that the URI parser recognizes as absolute (scheme present).
        if (Uri.TryCreate(path, UriKind.Absolute, out _)) {
            return false;
        }

        // Require a leading slash so the value resolves under the application's base path
        // rather than relative to the current page.
        if (!path.StartsWith("/", StringComparison.Ordinal)) {
            return false;
        }

        // Reject control characters, BiDi overrides, zero-width chars, and BOM — they spoof the
        // rendered vs navigated target without being caught by IsWellFormedUriString.
        foreach (char c in path) {
            if (char.IsControl(c) || IsDisplaySpoofingChar(c)) {
                return false;
            }
        }

        if (!Uri.IsWellFormedUriString(path, UriKind.Relative)) {
            return false;
        }

        // Path-traversal defense: reject paths that contain a `..` segment anywhere. Under a
        // non-root base href, `/../admin` escapes the intended scope.
        if (HasTraversalSegment(path)) {
            return false;
        }

        // Percent-encoding bypass defense: after a single decode, re-assert that the decoded
        // form doesn't contain any of the rejected prefix-escape patterns or path-traversal
        // segments. Example: `/%2f/evil.example` decodes to `//evil.example`.
        string decoded = Uri.UnescapeDataString(path);
        if (!string.Equals(decoded, path, StringComparison.Ordinal)) {
            if (HasProtocolRelativePrefix(decoded)
                || decoded.IndexOf("//", StringComparison.Ordinal) >= 0
                || decoded.IndexOf("\\\\", StringComparison.Ordinal) >= 0
                || decoded.IndexOf("/\\", StringComparison.Ordinal) >= 0
                || decoded.IndexOf("\\/", StringComparison.Ordinal) >= 0
                || HasTraversalSegment(decoded)) {
                return false;
            }
        }

        return true;
    }

    private static bool HasProtocolRelativePrefix(string path)
        => path.StartsWith("//", StringComparison.Ordinal)
            || path.StartsWith("/\\", StringComparison.Ordinal)
            || path.StartsWith("\\\\", StringComparison.Ordinal)
            || path.StartsWith("\\/", StringComparison.Ordinal);

    private static bool HasTraversalSegment(string path)
        => path == "/.."
            || path.StartsWith("/../", StringComparison.Ordinal)
            || path.EndsWith("/..", StringComparison.Ordinal)
            || path.IndexOf("/../", StringComparison.Ordinal) >= 0;

    // BiDi overrides (U+202A..U+202E), directional isolates (U+2066..U+2069),
    // zero-width chars (U+200B..U+200F), and BOM (U+FEFF) render benign but navigate elsewhere.
    private static bool IsDisplaySpoofingChar(char c)
        => c is (>= '\u202A' and <= '\u202E')
            or (>= '\u2066' and <= '\u2069')
            or (>= '\u200B' and <= '\u200F')
            or '\uFEFF';
}
