using System.Text.RegularExpressions;

namespace Hexalith.FrontComposer.Mcp.Skills;

public static partial class SkillBenchmarkSummarySanitizer {
    private const int MaxFieldLength = 600;

    public static string Sanitize(string? value) {
        string text = value ?? string.Empty;
        text = text.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
        text = SecretRegex().Replace(text, "[REDACTED]");
        text = LocalPathRegex().Replace(text, "[LOCAL_PATH]");
        text = TenantRegex().Replace(text, "$1=[REDACTED]");
        text = text.Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("<script", "&lt;script", StringComparison.OrdinalIgnoreCase)
            .Replace("</script", "&lt;/script", StringComparison.OrdinalIgnoreCase);
        if (text.StartsWith("::", StringComparison.Ordinal)) {
            text = "\\" + text;
        }

        return text.Length > MaxFieldLength ? text[..MaxFieldLength] + "..." : text;
    }

    [GeneratedRegex(@"(?i)\b(?:sk-[A-Za-z0-9_-]{12,}|ghp_[A-Za-z0-9_]{12,}|github_pat_[A-Za-z0-9_]{12,}|xox[baprs]-[A-Za-z0-9-]{12,}|bearer\s+[A-Za-z0-9._~+/=-]+)\b", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 500)]
    private static partial Regex SecretRegex();

    [GeneratedRegex(@"(?:[A-Za-z]:[\\/][^\s]+)|(?<![\w/])/(?:home|Users|tmp|var)/[^\s]+", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 500)]
    private static partial Regex LocalPathRegex();

    [GeneratedRegex(@"(?i)\b(tenant|tenantid|user|userid|commandpayload)\s*[:=]\s*[^,; ]+", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 500)]
    private static partial Regex TenantRegex();
}
