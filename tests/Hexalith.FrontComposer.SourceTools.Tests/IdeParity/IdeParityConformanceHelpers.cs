using System.Text;
using System.Text.RegularExpressions;

namespace Hexalith.FrontComposer.SourceTools.Conformance;

internal static class GeneratedOutputPathContract
{
    public const string Version = "v1";
    public const string Template = "obj/{Config}/{TFM}/generated/HexalithFrontComposer/{TypeName}.g.razor.cs";

    public static string BuildProjectRelativePath(string configuration, string targetFramework, string generatedFileName)
    {
        if (string.IsNullOrWhiteSpace(configuration))
        {
            throw new ArgumentException("Configuration is required.", nameof(configuration));
        }

        if (string.IsNullOrWhiteSpace(targetFramework))
        {
            throw new ArgumentException("Target framework is required.", nameof(targetFramework));
        }

        if (string.IsNullOrWhiteSpace(generatedFileName))
        {
            throw new ArgumentException("Generated file name is required.", nameof(generatedFileName));
        }

        if (generatedFileName.Contains('/') || generatedFileName.Contains('\\'))
        {
            throw new ArgumentException("Generated file name must not include path separators.", nameof(generatedFileName));
        }

        return string.Join(
            "/",
            "obj",
            configuration,
            targetFramework,
            "generated",
            "HexalithFrontComposer",
            generatedFileName);
    }
}

internal static class IdeParityEvidencePath
{
    private static readonly string[] UnsupportedUriPrefixes = ["file:", "http:", "https:"];

    public static bool TryNormalizeProjectRelativePath(
        string repositoryRoot,
        string candidate,
        bool caseSensitive,
        out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(repositoryRoot) || string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        string trimmed = candidate.Trim();
        if (UnsupportedUriPrefixes.Any(prefix => trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            || trimmed.Contains("://", StringComparison.Ordinal)
            || Path.IsPathRooted(trimmed)
            || trimmed.Any(char.IsControl))
        {
            return false;
        }

        string slashNormalized = trimmed.Replace('\\', '/');
        if (slashNormalized.StartsWith("../", StringComparison.Ordinal)
            || slashNormalized.Equals("..", StringComparison.Ordinal)
            || slashNormalized.Contains("/../", StringComparison.Ordinal))
        {
            return false;
        }

        string fullRoot = EnsureTrailingSeparator(Path.GetFullPath(repositoryRoot));
        string candidateFullPath = Path.GetFullPath(Path.Combine(fullRoot, slashNormalized.Replace('/', Path.DirectorySeparatorChar)));
        StringComparison comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        if (!candidateFullPath.StartsWith(fullRoot, comparison))
        {
            return false;
        }

        normalized = candidateFullPath[fullRoot.Length..]
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
        return !string.IsNullOrWhiteSpace(normalized);
    }

    private static string EnsureTrailingSeparator(string path)
        => path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
}

internal enum IdeParityReportFormat
{
    Markdown,
    Json,
    Csv,
    Terminal,
    IssueBody,
}

internal static class IdeParityReportSanitizer
{
    private static readonly Regex ControlCharacters = new("[\\x00-\\x08\\x0B\\x0C\\x0E-\\x1F\\x7F]", RegexOptions.Compiled);
    private static readonly Regex SecretLikeValues = new("\\b(?:ghp|gho|ghu|ghs|github_pat|pat|sk)-[A-Za-z0-9_\\-]{20,}\\b|\\b(?:ghp|gho|ghu|ghs)_[A-Za-z0-9_]{20,}\\b|\\btoken\\s*=\\s*[^\\s,)]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex WindowsAbsolutePath = new("\\b[A-Za-z]:[/\\\\][^\\s\\)\\]\\}\\\"']+", RegexOptions.Compiled);
    private static readonly Regex FileUri = new("file:///?[^\\s\\)\\]\\}\\\"']+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex MachineName = new("\\bmachine\\s*=\\s*[^\\s,)]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex UserHomeSegment = new("(?i)(/Users/|/home/|\\\\Users\\\\)[^/\\\\\\s]+", RegexOptions.Compiled);

    public static string Sanitize(string value, IdeParityReportFormat format, int maxLength = 4096)
    {
        string sanitized = value ?? string.Empty;
        sanitized = sanitized.Replace("\u001b", "\\u001B", StringComparison.Ordinal);
        sanitized = ControlCharacters.Replace(sanitized, string.Empty);
        sanitized = FileUri.Replace(sanitized, "[redacted-path]");
        sanitized = WindowsAbsolutePath.Replace(sanitized, "[redacted-path]");
        sanitized = UserHomeSegment.Replace(sanitized, match => match.Value.StartsWith('\\') ? "\\Users\\[redacted-user]" : match.Value[..match.Groups[1].Length] + "[redacted-user]");
        sanitized = SecretLikeValues.Replace(sanitized, "[redacted-secret]");
        sanitized = MachineName.Replace(sanitized, "machine=[redacted-machine]");

        sanitized = format switch
        {
            IdeParityReportFormat.Markdown or IdeParityReportFormat.IssueBody => EscapeMarkdownTableCells(sanitized),
            IdeParityReportFormat.Json => sanitized.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal),
            IdeParityReportFormat.Csv => EscapeCsv(sanitized),
            IdeParityReportFormat.Terminal => sanitized,
            _ => sanitized,
        };

        if (sanitized.Length > maxLength)
        {
            sanitized = sanitized[..maxLength] + $"[truncated:{sanitized.Length - maxLength}]";
        }

        return sanitized;
    }

    private static string EscapeMarkdownTableCells(string value)
        => value.Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("<script", "&lt;script", StringComparison.OrdinalIgnoreCase)
            .Replace("</script", "&lt;/script", StringComparison.OrdinalIgnoreCase);

    private static string EscapeCsv(string value)
    {
        string escaped = value.Replace("\"", "\"\"", StringComparison.Ordinal);
        if (escaped.Length > 0 && "=+-@".Contains(escaped[0], StringComparison.Ordinal))
        {
            escaped = "'" + escaped;
        }

        return escaped.Contains(',', StringComparison.Ordinal) || escaped.Contains('"', StringComparison.Ordinal) || escaped.Contains('\n', StringComparison.Ordinal)
            ? "\"" + escaped + "\""
            : escaped;
    }
}

internal sealed record IdeParityVersionPin(
    string Product,
    string MinimumInclusive,
    string MaximumExclusive,
    string Owner);

internal sealed record IdeParityDetectedVersion(
    string Product,
    string Version,
    string Os,
    string Fixture,
    IReadOnlyList<string> MatrixRows,
    string ExpectedBehavior,
    string ObservedBehavior);

internal sealed record IdeParityRevalidationIssue(
    string Title,
    string Body,
    IReadOnlyList<string> Labels,
    bool DryRun,
    bool IsBlocking);

internal static class IdeParityVersionRevalidator
{
    public static IdeParityRevalidationIssue CreateDryRunIssue(
        IdeParityVersionPin supported,
        IdeParityDetectedVersion detected,
        bool githubAvailable)
    {
        bool outOfRange = CompareVersions(detected.Version, supported.MinimumInclusive) < 0
            || CompareVersions(detected.Version, supported.MaximumExclusive) >= 0;
        bool blocking = outOfRange || !githubAvailable;
        string title = $"IDE parity revalidation required: {detected.Product} {detected.Version}";

        StringBuilder body = new();
        _ = body.AppendLine("## IDE parity version drift");
        _ = body.AppendLine();
        _ = body.AppendLine($"- product: {IdeParityReportSanitizer.Sanitize(detected.Product, IdeParityReportFormat.IssueBody)}");
        _ = body.AppendLine($"- detected version: {IdeParityReportSanitizer.Sanitize(detected.Version, IdeParityReportFormat.IssueBody)}");
        _ = body.AppendLine($"- current pin: {supported.MinimumInclusive} <= version < {supported.MaximumExclusive}");
        _ = body.AppendLine($"- OS/container: {IdeParityReportSanitizer.Sanitize(detected.Os, IdeParityReportFormat.IssueBody)}");
        _ = body.AppendLine($"- fixture: {IdeParityReportSanitizer.Sanitize(detected.Fixture, IdeParityReportFormat.IssueBody)}");
        _ = body.AppendLine($"- release owner: {IdeParityReportSanitizer.Sanitize(supported.Owner, IdeParityReportFormat.IssueBody)}");
        _ = body.AppendLine("- Visual Studio calibration row passes: evidence required before widening the pin");
        _ = body.AppendLine($"- expected behavior: {IdeParityReportSanitizer.Sanitize(detected.ExpectedBehavior, IdeParityReportFormat.IssueBody)}");
        _ = body.AppendLine($"- observed behavior: {IdeParityReportSanitizer.Sanitize(detected.ObservedBehavior, IdeParityReportFormat.IssueBody)}");
        _ = body.AppendLine($"- required evidence: refresh matrix rows {string.Join(", ", detected.MatrixRows.Select(row => IdeParityReportSanitizer.Sanitize(row, IdeParityReportFormat.IssueBody)))}");
        if (!githubAvailable)
        {
            _ = body.AppendLine("- fallback: GitHub issue creation unavailable; this dry-run artifact blocks the release checklist.");
        }

        return new IdeParityRevalidationIssue(
            title,
            body.ToString(),
            ["ide-parity", "conformance-revalidation"],
            DryRun: !githubAvailable,
            IsBlocking: blocking);
    }

    private static int CompareVersions(string left, string right)
    {
        int[] leftParts = ParseVersionParts(left);
        int[] rightParts = ParseVersionParts(right);
        int length = Math.Max(leftParts.Length, rightParts.Length);
        for (int i = 0; i < length; i++)
        {
            int l = i < leftParts.Length ? leftParts[i] : 0;
            int r = i < rightParts.Length ? rightParts[i] : 0;
            if (l != r)
            {
                return l.CompareTo(r);
            }
        }

        return 0;
    }

    private static int[] ParseVersionParts(string version)
        => version.Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => int.TryParse(new string(part.TakeWhile(char.IsDigit).ToArray()), out int value) ? value : 0)
            .ToArray();
}
