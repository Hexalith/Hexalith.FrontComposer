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
        // skate past the `//` check.
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
            || ContainsForbiddenCharacter(unescaped)) {
            return "/";
        }

        return candidate;
    }

    private static bool ContainsForbiddenCharacter(string value) {
        for (int i = 0; i < value.Length; i++) {
            char c = value[i];
            if (char.IsControl(c) || IsForbiddenFormatChar(c)) {
                return true;
            }
        }

        return false;
    }

    /// <summary>P3 — Unicode format characters that browsers and renderers may treat
    /// inconsistently (RTL/LTR overrides, line/paragraph separators, BOM, zero-width spaces,
    /// non-breaking space). Reject them outright in return URLs to prevent UI spoofing,
    /// log injection, and header confusion. Codepoints listed explicitly to avoid
    /// editor/encoding mistakes when reading visually identical glyphs.</summary>
    private static bool IsForbiddenFormatChar(char c) {
        int cp = c;
        return cp switch {
            0x00A0 => true, // NO-BREAK SPACE
            0x200B => true, // ZERO WIDTH SPACE
            0x200C => true, // ZERO WIDTH NON-JOINER
            0x200D => true, // ZERO WIDTH JOINER
            0x200E => true, // LEFT-TO-RIGHT MARK
            0x200F => true, // RIGHT-TO-LEFT MARK
            0x2028 => true, // LINE SEPARATOR
            0x2029 => true, // PARAGRAPH SEPARATOR
            0x202A => true, // LEFT-TO-RIGHT EMBEDDING
            0x202B => true, // RIGHT-TO-LEFT EMBEDDING
            0x202C => true, // POP DIRECTIONAL FORMATTING
            0x202D => true, // LEFT-TO-RIGHT OVERRIDE
            0x202E => true, // RIGHT-TO-LEFT OVERRIDE
            0x2060 => true, // WORD JOINER
            0xFEFF => true, // ZERO WIDTH NO-BREAK SPACE / BOM
            _ => false,
        };
    }
}
