using System.Globalization;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.Services.Auth;

internal static class FrontComposerReturnUrl {
    /// <summary>P3 — return URL length cap. Beyond this size the URL is dropped to `/` because
    /// it cannot fit in a typical OIDC state cookie (and represents abuse / DoS surface).</summary>
    public const int MaxReturnUrlLength = 2048;

    private const int MaxUnescapeIterations = 8;

    public static string Sanitize(string? returnUrl) {
        if (string.IsNullOrWhiteSpace(returnUrl)) {
            return "/";
        }

        if (returnUrl.Length > MaxReturnUrlLength) {
            return "/";
        }

        string candidate = returnUrl.Trim();
        if (candidate.StartsWith("~/", StringComparison.Ordinal)) {
            candidate = candidate[1..];
        }
        else if (candidate.StartsWith('?')) {
            candidate = "/" + candidate;
        }

        // P3 — fixpoint unescape (bounded to prevent pathological loops). Prior implementation
        // capped at 2 iterations, which let `/%2525%252fevil` and similar multi-encoded payloads
        // skate past the `//` check. On net10 `Uri.UnescapeDataString` never throws for malformed
        // percent sequences (it returns them unchanged); malformed-percent rejection is enforced
        // by the delegated `ReturnPathValidator.IsSafeRelativePath` check below.
        string unescaped = candidate;
        for (int i = 0; i < MaxUnescapeIterations; i++) {
            string next = Uri.UnescapeDataString(unescaped);
            if (string.Equals(next, unescaped, StringComparison.Ordinal)) {
                break;
            }

            unescaped = next;
        }

        if (!candidate.StartsWith('/')
            || candidate.StartsWith("//", StringComparison.Ordinal)
            || candidate.StartsWith("/@", StringComparison.Ordinal)
            || candidate.StartsWith("/\\", StringComparison.Ordinal)
            || unescaped.StartsWith("//", StringComparison.Ordinal)
            || unescaped.StartsWith("/@", StringComparison.Ordinal)
            || unescaped.Contains('\\', StringComparison.Ordinal)
            || ContainsForbiddenCharacter(unescaped)
            || !ReturnPathValidator.IsSafeRelativePath(candidate)) {
            return "/";
        }

        return candidate;
    }

    /// <summary>P3 — Reject characters that browsers and renderers may treat inconsistently in a
    /// return URL: control characters, exotic (non-ASCII) whitespace, and every Unicode "format"
    /// (Cf) code point (RTL/LTR overrides, directional isolates, zero-width joiners/spaces, word
    /// joiner, soft hyphen, Arabic letter mark, BOM, and astral format code points such as
    /// U+E0001). Rejecting the whole Cf category rather than an enumerated denylist closes the
    /// invisible-character class in one predicate and stays aligned with <c>ReturnPathValidator</c>.
    /// The scan is code-point aware so astral format code points, which are surrogate pairs, are
    /// classified correctly.</summary>
    private static bool ContainsForbiddenCharacter(string value) {
        for (int i = 0; i < value.Length; i++) {
            char c = value[i];
            if (char.IsControl(c) || (char.IsWhiteSpace(c) && c != ' ')) {
                return true;
            }

            if (c > (char)127 && CharUnicodeInfo.GetUnicodeCategory(value, i) == UnicodeCategory.Format) {
                return true;
            }

            if (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1])) {
                i++;
            }
        }

        return false;
    }
}
