namespace Hexalith.FrontComposer.Shell.Services.Auth;

internal static class FrontComposerReturnUrl {
    public static string Sanitize(string? returnUrl) {
        if (string.IsNullOrWhiteSpace(returnUrl)) {
            return "/";
        }

        string candidate = returnUrl.Trim();
        if (candidate.StartsWith("~/", StringComparison.Ordinal)) {
            candidate = candidate[1..];
        }
        else if (candidate.StartsWith("?", StringComparison.Ordinal)) {
            candidate = "/" + candidate;
        }

        string unescaped = candidate;
        for (int i = 0; i < 2; i++) {
            string next = Uri.UnescapeDataString(unescaped);
            if (string.Equals(next, unescaped, StringComparison.Ordinal)) {
                break;
            }

            unescaped = next;
        }

        if (!candidate.StartsWith("/", StringComparison.Ordinal)
            || candidate.StartsWith("//", StringComparison.Ordinal)
            || unescaped.StartsWith("//", StringComparison.Ordinal)
            || unescaped.Contains('\\', StringComparison.Ordinal)
            || ContainsControl(unescaped)) {
            return "/";
        }

        return candidate;
    }

    private static bool ContainsControl(string value) {
        for (int i = 0; i < value.Length; i++) {
            if (char.IsControl(value[i])) {
                return true;
            }
        }

        return false;
    }
}
