using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Hexalith.FrontComposer.SourceTools.Tests.IdeParity;

internal static class IdeParityEvidencePath {
    private static readonly Regex _schemePrefix = new("^[A-Za-z][A-Za-z0-9+.\\-]*:", RegexOptions.Compiled);

    private static readonly char[] _bidiAndZeroWidth =
    {
        '﻿', '​', '‌', '‍', '⁠',
        '‎', '‏', '‪', '‫', '‬', '‭', '‮',
        '⁦', '⁧', '⁨', '⁩',
    };

    public static bool TryNormalizeProjectRelativePath(
        string repositoryRoot,
        string candidate,
        bool caseSensitive,
        out string normalized) {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(repositoryRoot) || string.IsNullOrWhiteSpace(candidate)) {
            return false;
        }

        string trimmed = candidate.Trim().Normalize(NormalizationForm.FormC);
        if (trimmed.IndexOfAny(_bidiAndZeroWidth) >= 0) {
            return false;
        }

        if (_schemePrefix.IsMatch(trimmed)
            || trimmed.Contains("://", StringComparison.Ordinal)
            || Path.IsPathRooted(trimmed)
            || trimmed.Any(char.IsControl)) {
            return false;
        }

        string slashNormalized = trimmed.Replace('\\', '/');

        if (slashNormalized.StartsWith("//", StringComparison.Ordinal)) {
            return false;
        }

        if (slashNormalized.Split('/').Any(segment => segment == "..")) {
            return false;
        }

        string fullRoot = EnsureTrailingSeparator(ResolveLinkTarget(Path.GetFullPath(repositoryRoot.Normalize(NormalizationForm.FormC))));
        string candidateFullPath = ResolveLinkTarget(
            Path.GetFullPath(Path.Combine(fullRoot, slashNormalized.Replace('/', Path.DirectorySeparatorChar))));

        StringComparison comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        if (!candidateFullPath.StartsWith(fullRoot, comparison)) {
            return false;
        }

        normalized = candidateFullPath[fullRoot.Length..]
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
        return !string.IsNullOrWhiteSpace(normalized);
    }

    public static bool DefaultCaseSensitivityForFilesystem()
        => !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
           && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    private static string EnsureTrailingSeparator(string path)
        => path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;

    private static string ResolveLinkTarget(string path) {
        try {
            for (int hops = 0; hops < 16; hops++) {
                FileSystemInfo? info = Directory.Exists(path)
                    ? new DirectoryInfo(path)
                    : File.Exists(path) ? (FileSystemInfo)new FileInfo(path) : null;

                FileSystemInfo? resolved = info?.ResolveLinkTarget(returnFinalTarget: true);
                if (resolved is null) {
                    return path;
                }

                path = Path.GetFullPath(resolved.FullName);
            }
        }
        catch (IOException) {
        }
        catch (UnauthorizedAccessException) {
        }
        catch (NotSupportedException) {
        }

        return path;
    }
}

internal enum IdeParityReportFormat {
    Markdown,
    Json,
    Csv,
    Terminal,
    IssueBody,
}

internal static class IdeParityReportSanitizer {
    private static readonly Regex _controlCharacters = new("[\\x00-\\x08\\x0B\\x0C\\x0E-\\x1F\\x7F]", RegexOptions.Compiled);
    private static readonly Regex _osc8Hyperlink = new("\\u001B\\]8;[^\\u001B\\u0007]*(?:\\u0007|\\u001B\\\\)", RegexOptions.Compiled);
    private static readonly Regex _secretLikeValues = new(
        "\\b(?:ghp|gho|ghu|ghs|github_pat|pat|sk)-[A-Za-z0-9_\\-]{20,}\\b" +
        "|\\b(?:ghp|gho|ghu|ghs|github_pat)_[A-Za-z0-9_]{20,}\\b" +
        "|\\b(?:AKIA|ASIA)[0-9A-Z]{16}\\b" +
        "|\\bey[A-Za-z0-9_-]{10,}\\.[A-Za-z0-9_-]{10,}\\.[A-Za-z0-9_-]{10,}\\b" +
        "|\\b(?:token|password|pwd|secret|api[_\\-]?key|client[_\\-]?secret)\\s*=\\s*[^\\s,)]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex _windowsAbsolutePath = new("\\b[A-Za-z]:[/\\\\][^\\s\\)\\]\\}\\\"']+", RegexOptions.Compiled);
    private static readonly Regex _uncPath = new("\\\\\\\\[A-Za-z0-9._\\-]+\\\\[^\\s\\)\\]\\}\\\"']+", RegexOptions.Compiled);
    private static readonly Regex _fileUri = new("file:///?[^\\s\\)\\]\\}\\\"']+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex _machineName = new("\\bmachine\\s*=\\s*[^\\s,)]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex _userHomeSegment = new(
        "(?<prefix>(?i:/Users/|/home/|/root/?|/var/lib/|/var/runners/_work/|/workspaces/|\\\\Users\\\\))(?<user>[^/\\\\\\s]+)",
        RegexOptions.Compiled);
    private static readonly Regex _systemRootPath = new(
        "(?<![A-Za-z0-9_])/(?:etc|opt|srv)/[^\\s\\)\\]\\}\\\"']+",
        RegexOptions.Compiled);

    public static string Sanitize(string value, IdeParityReportFormat format, int maxLength = 4096) {
        if (maxLength <= 0) {
            throw new ArgumentOutOfRangeException(nameof(maxLength), "Max length must be positive.");
        }

        string sanitized = value ?? string.Empty;
        sanitized = _osc8Hyperlink.Replace(sanitized, "[redacted-osc8-link]");
        sanitized = sanitized.Replace("", "\\u001B", StringComparison.Ordinal);
        sanitized = _controlCharacters.Replace(sanitized, string.Empty);
        sanitized = _fileUri.Replace(sanitized, "[redacted-path]");
        sanitized = _uncPath.Replace(sanitized, "[redacted-unc-path]");
        sanitized = _windowsAbsolutePath.Replace(sanitized, "[redacted-path]");
        sanitized = _systemRootPath.Replace(sanitized, "[redacted-path]");
        sanitized = _userHomeSegment.Replace(sanitized, match => match.Groups["prefix"].Value + "[redacted-user]");
        sanitized = _secretLikeValues.Replace(sanitized, "[redacted-secret]");
        sanitized = _machineName.Replace(sanitized, "machine=[redacted-machine]");

        sanitized = format switch {
            IdeParityReportFormat.Markdown or IdeParityReportFormat.IssueBody => EscapeMarkdownTableCells(sanitized),
            IdeParityReportFormat.Json => sanitized.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal),
            IdeParityReportFormat.Csv => EscapeCsv(sanitized),
            IdeParityReportFormat.Terminal => sanitized,
            _ => sanitized,
        };

        if (sanitized.Length > maxLength) {
            int safeCut = maxLength;
            if (safeCut > 0 && char.IsHighSurrogate(sanitized[safeCut - 1])) {
                safeCut--;
            }

            sanitized = sanitized[..safeCut] + $"[truncated:{sanitized.Length - safeCut}]";
        }

        return sanitized;
    }

    private static string EscapeMarkdownTableCells(string value) {
        StringBuilder builder = new(value.Length + 16);
        foreach (char c in value) {
            _ = c switch {
                '|' => builder.Append("\\|"),
                '<' => builder.Append("&lt;"),
                '>' => builder.Append("&gt;"),
                '\r' or '\n' => builder.Append(' '),
                _ => builder.Append(c),
            };
        }

        return builder.ToString();
    }

    private static string EscapeCsv(string value) {
        string escaped = value.Replace("\"", "\"\"", StringComparison.Ordinal);
        if (escaped.Length > 0 && IsCsvFormulaTrigger(escaped[0])) {
            escaped = "'" + escaped;
        }

        return escaped.Contains(',', StringComparison.Ordinal)
            || escaped.Contains('"', StringComparison.Ordinal)
            || escaped.Contains('\n', StringComparison.Ordinal)
            ? "\"" + escaped + "\""
            : escaped;
    }

    private static bool IsCsvFormulaTrigger(char c)
        // Spreadsheet formula triggers ('=' '+' '-' '@') plus invisible characters that
        // common spreadsheet apps auto-trim before formula evaluation: TAB, CR, LF, regular
        // SPACE, NBSP (U+00A0), zero-width space (U+200B), and BOM (U+FEFF). Numeric char
        // literals avoid escape-sequence ambiguity in the source file.
        => c is '=' or '+' or '-' or '@'
            or (char)0x09 or (char)0x0A or (char)0x0D
            or (char)0x20 or (char)0xA0 or (char)0x200B or (char)0xFEFF;
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

internal interface IGithubAvailabilityProbe {
    bool IsAuthenticated();

    bool LabelsAccessible(IReadOnlyList<string> labels);
}

internal static class IdeParityVersionRevalidator {
    private static readonly IReadOnlyList<string> _requiredLabels = ["ide-parity", "conformance-revalidation"];

    public static IdeParityRevalidationIssue CreateDryRunIssue(
        IdeParityVersionPin supported,
        IdeParityDetectedVersion detected,
        bool githubAvailable) {
        ArgumentNullException.ThrowIfNull(supported);
        ArgumentNullException.ThrowIfNull(detected);
        ArgumentNullException.ThrowIfNull(detected.MatrixRows);

        bool outOfRange = CompareVersions(detected.Version, supported.MinimumInclusive) < 0
            || CompareVersions(detected.Version, supported.MaximumExclusive) >= 0;
        bool blocking = outOfRange || !githubAvailable;
        string sanitizedProduct = IdeParityReportSanitizer.Sanitize(detected.Product, IdeParityReportFormat.IssueBody);
        string sanitizedVersion = IdeParityReportSanitizer.Sanitize(detected.Version, IdeParityReportFormat.IssueBody);
        string title = $"IDE parity revalidation required: {sanitizedProduct} {sanitizedVersion}";

        StringBuilder body = new();
        _ = body.AppendLine("## IDE parity version drift");
        _ = body.AppendLine();
        _ = body.AppendLine($"- product: {sanitizedProduct}");
        _ = body.AppendLine($"- detected version: {sanitizedVersion}");
        _ = body.AppendLine($"- current pin: {supported.MinimumInclusive} <= version < {supported.MaximumExclusive}");
        _ = body.AppendLine($"- OS/container: {IdeParityReportSanitizer.Sanitize(detected.Os, IdeParityReportFormat.IssueBody)}");
        _ = body.AppendLine($"- fixture: {IdeParityReportSanitizer.Sanitize(detected.Fixture, IdeParityReportFormat.IssueBody)}");
        _ = body.AppendLine($"- release owner: {IdeParityReportSanitizer.Sanitize(supported.Owner, IdeParityReportFormat.IssueBody)}");
        _ = body.AppendLine("- Visual Studio calibration row passes: evidence required before widening the pin");
        _ = body.AppendLine($"- expected behavior: {IdeParityReportSanitizer.Sanitize(detected.ExpectedBehavior, IdeParityReportFormat.IssueBody)}");
        _ = body.AppendLine($"- observed behavior: {IdeParityReportSanitizer.Sanitize(detected.ObservedBehavior, IdeParityReportFormat.IssueBody)}");
        _ = body.AppendLine($"- required evidence: refresh matrix rows {string.Join(", ", detected.MatrixRows.Select(row => IdeParityReportSanitizer.Sanitize(row, IdeParityReportFormat.IssueBody)))}");
        if (!githubAvailable) {
            _ = body.AppendLine("- fallback: GitHub issue creation unavailable; this dry-run artifact blocks the release checklist.");
        }

        return new IdeParityRevalidationIssue(
            title,
            body.ToString(),
            _requiredLabels,
            DryRun: !githubAvailable,
            IsBlocking: blocking);
    }

    public static IdeParityRevalidationIssue Resolve(
        IdeParityVersionPin supported,
        IdeParityDetectedVersion detected,
        IGithubAvailabilityProbe? probe) {
        bool githubAvailable = probe is not null
            && probe.IsAuthenticated()
            && probe.LabelsAccessible(_requiredLabels);
        return CreateDryRunIssue(supported, detected, githubAvailable);
    }

    public static int CompareVersions(string left, string right) {
        int[] leftParts = ParseVersionParts(left);
        int[] rightParts = ParseVersionParts(right);
        int length = Math.Max(leftParts.Length, rightParts.Length);
        for (int i = 0; i < length; i++) {
            int l = i < leftParts.Length ? leftParts[i] : 0;
            int r = i < rightParts.Length ? rightParts[i] : 0;
            if (l != r) {
                return l.CompareTo(r);
            }
        }

        return 0;
    }

    private static int[] ParseVersionParts(string? version) {
        if (string.IsNullOrWhiteSpace(version)) {
            throw new ArgumentException("Version is required.", nameof(version));
        }

        string[] segments = version!.Split('.', StringSplitOptions.RemoveEmptyEntries);
        int[] parts = new int[segments.Length];
        for (int i = 0; i < segments.Length; i++) {
            string raw = segments[i];
            int digitEnd = 0;
            while (digitEnd < raw.Length && char.IsDigit(raw, digitEnd)) {
                digitEnd++;
            }

            if (digitEnd == 0) {
                throw new ArgumentException(
                    $"Version segment '{raw}' must start with a digit.",
                    nameof(version));
            }

            string digits = raw[..digitEnd];
            if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out int value)) {
                throw new ArgumentException(
                    $"Version segment '{raw}' overflows Int32.",
                    nameof(version));
            }

            parts[i] = value;
        }

        return parts;
    }
}
