using System.Globalization;

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
    private const int MaxDecodeIterations = 6;

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

        // Reject any form that the URI parser recognizes as absolute with a real (web/network)
        // scheme — http, https, javascript, mailto, file://host, etc.
        // NOTE: on Unix, Uri.TryCreate("/path", Absolute) SUCCEEDS with an implicit `file` scheme
        // (a leading-slash path is a valid absolute *file* path there; on Windows it returns false).
        // A leading-slash relative URL is exactly what the check below requires, so the implicit
        // `file` scheme must not be treated as "absolute" here — only real schemes are rejected.
        // Cross-origin `file://host/...` forms are still blocked: they do not start with `/`, so the
        // leading-slash requirement below rejects them.
        if (Uri.TryCreate(path, UriKind.Absolute, out Uri? absolute)
            && !string.Equals(absolute.Scheme, Uri.UriSchemeFile, StringComparison.Ordinal)) {
            return false;
        }

        // Require a leading slash so the value resolves under the application's base path
        // rather than relative to the current page.
        if (!path.StartsWith("/", StringComparison.Ordinal)) {
            return false;
        }

        if (ContainsUnsafeCharacters(path)) {
            return false;
        }

        if (!IsWellFormedRootRelativePath(path)) {
            return false;
        }

        // Path-traversal defense: reject paths that contain a `..` segment anywhere. Under a
        // non-root base href, `/../admin` escapes the intended scope.
        if (HasUnsafePathShape(path)) {
            return false;
        }

        // Percent-encoding bypass defense: repeatedly decode a bounded number of times and
        // re-assert the security invariants after each pass. This catches double-encoded slash
        // and traversal payloads such as `/%252f/evil.example` and `/%252e%252e/admin`.
        // Malformed percent sequences are rejected up-front by HasInvalidPercentEncoding;
        // modern .NET's UnescapeDataString returns malformed sequences unchanged rather than
        // throwing, so the catch below is a fail-closed safety net for legacy netstandard2.0
        // runtimes only.
        string current = path;
        for (int i = 0; i < MaxDecodeIterations; i++) {
            string decoded;
            try {
                decoded = Uri.UnescapeDataString(current);
            }
            catch (UriFormatException) {
                return false;
            }

            if (string.Equals(decoded, current, StringComparison.Ordinal)) {
                return true;
            }

            if (!decoded.StartsWith("/", StringComparison.Ordinal)
                || ContainsUnsafeCharacters(decoded)
                || HasUnsafePathShape(decoded)) {
                return false;
            }

            current = decoded;
        }

        return false;
    }

    private static bool HasProtocolRelativePrefix(string path)
        => path.StartsWith("//", StringComparison.Ordinal)
            || path.StartsWith("/\\", StringComparison.Ordinal)
            || path.StartsWith("\\\\", StringComparison.Ordinal)
            || path.StartsWith("\\/", StringComparison.Ordinal);

    // `:/` (not just `://`) — lenient browser URL parsers normalize single-slash scheme forms
    // such as `https:/evil.example` to `https://evil.example`, so a nested `next=https:/evil`
    // token must not survive validation. The mid-string `//` check applies to raw and decoded
    // forms alike so acceptance never depends on whether unrelated percent-encoding is present.
    private static bool HasUnsafePathShape(string path)
        => HasProtocolRelativePrefix(path)
            || path.Contains(":/", StringComparison.Ordinal)
            || path.IndexOf('\\') >= 0
            || HasTraversalSegment(path)
            || path.IndexOf("//", StringComparison.Ordinal) >= 0;

    private static bool HasTraversalSegment(string path) {
        int queryIndex = path.IndexOfAny(new[] { '?', '#' });
        string pathOnly = queryIndex >= 0 ? path.Substring(0, queryIndex) : path;
        return pathOnly == "/.."
            || pathOnly.StartsWith("/../", StringComparison.Ordinal)
            || pathOnly.EndsWith("/..", StringComparison.Ordinal)
            || pathOnly.IndexOf("/../", StringComparison.Ordinal) >= 0;
    }

    // Runs on the raw path AND every decoded form, so an unsafe character cannot slip through by
    // being percent-encoded. Rejected, per Unicode code point:
    //   * control characters,
    //   * exotic (non-ASCII) whitespace — encoded shapes such as `/%E2%80%89hidden` (thin space)
    //     or `/%E3%80%80x` (ideographic space) must not pass while their raw forms are rejected;
    //     a plain ASCII space is excluded because `%20` is the canonical legitimate space encoding,
    //   * the RFC 3986 forbidden characters (`< > " ` { } | ^`), so their encoded forms such as
    //     `/orders/%3Cscript%3E` are rejected on the decoded pass exactly as their raw forms are,
    //   * every Unicode "format" (Cf) code point — BiDi overrides, directional isolates, zero-width
    //     joiners/spaces, word joiner, soft hyphen, Arabic letter mark, BOM, AND astral format
    //     code points such as U+E0001 — which render invisibly while altering the resolved target.
    // Rejecting the whole Cf category (rather than an enumerated denylist) closes the invisible-char
    // class in one predicate. The scan is code-point aware (not per-UTF-16-char) so astral format
    // code points, which are surrogate pairs, are classified correctly; the netstandard2.0 target
    // has no System.Text.Rune, so CharUnicodeInfo.GetUnicodeCategory(string, index) provides the
    // surrogate-pair-aware lookup.
    private static bool ContainsUnsafeCharacters(string path) {
        for (int i = 0; i < path.Length; i++) {
            char c = path[i];
            if (char.IsControl(c) || (char.IsWhiteSpace(c) && c != ' ') || IsForbiddenUriChar(c)) {
                return true;
            }

            // Only non-ASCII code points can be in the Unicode "format" category, so skip the
            // category lookup on the ASCII fast path.
            if (c > (char)127 && CharUnicodeInfo.GetUnicodeCategory(path, i) == UnicodeCategory.Format) {
                return true;
            }

            if (char.IsHighSurrogate(c) && i + 1 < path.Length && char.IsLowSurrogate(path[i + 1])) {
                i++;
            }
        }

        return false;
    }

    private static bool IsWellFormedRootRelativePath(string path) {
        foreach (char c in path) {
            if (char.IsWhiteSpace(c) || IsForbiddenUriChar(c)) {
                return false;
            }
        }

        return !HasInvalidPercentEncoding(path)
            && Uri.TryCreate(path, UriKind.Relative, out _);
    }

    // RFC 3986 forbids these characters raw in URIs and the pre-hardening
    // `Uri.IsWellFormedUriString(Relative)` check rejected them; `Uri.TryCreate(UriKind.Relative)`
    // tolerates them. `IsWellFormedUriString` itself cannot be used here because it also rejects
    // legitimate fragments in relative references (`/orders?tab=x#items`), so the strictness is
    // restored with this explicit, runtime-deterministic character set instead.
    private static bool IsForbiddenUriChar(char c)
        => c is '<' or '>' or '"' or '`' or '{' or '}' or '|' or '^';

    private static bool HasInvalidPercentEncoding(string path) {
        for (int i = 0; i < path.Length; i++) {
            if (path[i] != '%') {
                continue;
            }

            if (i + 2 >= path.Length
                || !IsHexDigit(path[i + 1])
                || !IsHexDigit(path[i + 2])) {
                return true;
            }
        }

        return false;
    }

    private static bool IsHexDigit(char c)
        => c is (>= '0' and <= '9')
            or (>= 'A' and <= 'F')
            or (>= 'a' and <= 'f');
}
