namespace Hexalith.FrontComposer.SourceTools.Drift;

internal static class DriftSanitizer {
    /// <summary>
    /// Story 9-1 P16: tightened from substring-blocklist to value-shape patterns. Previously
    /// names like <c>TokenStore</c>, <c>EtagPolicy</c>, <c>TenantConfig</c>, <c>UserCount</c>
    /// — all legitimate domain identifiers — were classified as unsafe and produced
    /// HFC1069 redaction-suppressed errors. Now we only match secret-shaped substrings
    /// (SENTINEL test sentinels, JWT-prefix tokens, JSON fragments, absolute paths).
    /// Member names are part of the structural baseline by design and must pass through.
    /// </summary>
    internal static bool IsUnsafe(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return false;
        }

        string trimmed = value.Trim();
        return trimmed.IndexOf("SENTINEL_", StringComparison.Ordinal) >= 0
            || trimmed.IndexOf("Bearer ", StringComparison.OrdinalIgnoreCase) >= 0
            || trimmed.IndexOf("Authorization:", StringComparison.OrdinalIgnoreCase) >= 0
            || trimmed.IndexOf("eyJ", StringComparison.Ordinal) >= 0
            || trimmed.IndexOf("{\"", StringComparison.Ordinal) >= 0
            || ContainsAbsolutePath(trimmed);
    }

    /// <summary>
    /// Story 9-1 P28: redact-or-pass on the broader token list including normalized variants
    /// (forward-slash drive paths like <c>C:/</c>) that the previous implementation missed.
    /// </summary>
    internal static string SafeMessage(string message) {
        if (string.IsNullOrEmpty(message)) {
            return message;
        }

        if (ContainsRedactionTrigger(message)) {
            return "What: drift diagnostic content was suppressed by redaction. Expected: sanitized structural metadata. Got: unsafe diagnostic payload. Fix: remove runtime data from baseline/source metadata. DocsLink: https://hexalith.github.io/FrontComposer/diagnostics/" + DriftConstants.RedactionSuppressedId;
        }

        return message;
    }

    internal static string Safe(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return "<none>";
        }

        string trimmed = value.Trim();
        if (IsUnsafe(trimmed)) {
            return "<redacted>";
        }

        return trimmed.Length > 96 ? trimmed.Substring(0, 96) + "<truncated>" : trimmed;
    }

    /// <summary>
    /// Story 9-1 P17: previously any string containing <c>:</c> was reduced to filename
    /// (which leaked POSIX absolute paths since they have no colon, AND truncated benign
    /// colon-bearing strings). Now: detect Windows drive roots OR a leading slash and reduce
    /// to filename. Repo-relative paths pass through unchanged.
    /// </summary>
    internal static string NormalizePath(string path) {
        if (string.IsNullOrWhiteSpace(path) || path == "<none>") {
            return "<none>";
        }

        string normalized = path.Replace('\\', '/');
        bool isAbsolute = LooksLikeWindowsDriveRoot(normalized) || normalized.StartsWith("/", StringComparison.Ordinal);
        if (isAbsolute) {
            int lastSlash = normalized.LastIndexOf('/');
            if (lastSlash < 0 || lastSlash + 1 >= normalized.Length) {
                return "<outside-project>";
            }

            normalized = normalized.Substring(lastSlash + 1);
        }
        else {
            normalized = normalized.TrimStart('/');
        }

        return string.IsNullOrWhiteSpace(normalized) ? "<none>" : normalized;
    }

    private static bool ContainsRedactionTrigger(string text) => text.IndexOf("SENTINEL_", StringComparison.Ordinal) >= 0
            || text.IndexOf("Bearer ", StringComparison.OrdinalIgnoreCase) >= 0
            || text.IndexOf("Authorization:", StringComparison.OrdinalIgnoreCase) >= 0
            || text.IndexOf("eyJ", StringComparison.Ordinal) >= 0
            || text.IndexOf("{\"", StringComparison.Ordinal) >= 0
            || text.IndexOf("///auto/", StringComparison.Ordinal) >= 0
            || ContainsAbsolutePath(text);

    private static bool ContainsAbsolutePath(string value) {
        // Windows drive root with forward OR backslash: a single ASCII letter, then ':',
        // then '\\' or '/'. The letter MUST sit at the start of `value` or after a non-
        // letter character so we don't misclassify URL schemes like "https://" — there
        // the 's' in "s:/" is preceded by a letter and is therefore part of the scheme,
        // not a drive root.
        for (int i = 0; i + 2 < value.Length; i++) {
            char c0 = value[i];
            if (c0 is not ((>= 'A' and <= 'Z') or (>= 'a' and <= 'z'))) {
                continue;
            }

            if (value[i + 1] != ':') {
                continue;
            }

            char sep = value[i + 2];
            if (sep is not '\\' and not '/') {
                continue;
            }

            char preceding = i == 0 ? ' ' : value[i - 1];
            if (!char.IsLetter(preceding)) {
                return true;
            }
        }

        return value.IndexOf("C__", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool LooksLikeWindowsDriveRoot(string normalized) => normalized.Length >= 3
            && ((normalized[0] >= 'A' && normalized[0] <= 'Z') || (normalized[0] >= 'a' && normalized[0] <= 'z'))
            && normalized[1] == ':'
            && normalized[2] == '/';
}
