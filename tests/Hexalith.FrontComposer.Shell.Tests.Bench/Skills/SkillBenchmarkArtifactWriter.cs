using System.Text.Json;
using System.Text.RegularExpressions;

namespace Hexalith.FrontComposer.Mcp.Skills;

public static class SkillBenchmarkArtifactWriter {
    /// <summary>
    /// P-36: stable diagnostic constants instead of magic strings. P-15: redaction-not-passed is
    /// now joined by sanitization-shape diagnostics that block persistence even when the caller
    /// asserts redaction passed.
    /// </summary>
    public const string RedactionFailedDiagnostic = "redaction-not-passed";
    public const string SanitizationShapeDiagnostic = "sanitized-diagnostic-contains-raw-path";
    public const string UnsafeSummaryDiagnostic = "sanitized-diagnostic-contains-unsafe-summary";
    public const string MissingProviderMetadataDiagnostic = "benchmark-result-missing-provider-metadata";

    private static readonly Regex LooksLikeLocalPathRegex = new(
        @"(?:[A-Za-z]:[\\/])|(?:^|\s)[/\\][A-Za-z][^\s]*[/\\]",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(500));

    public static bool CanPersist(SkillBenchmarkResult result)
        => GetPersistenceDiagnostic(result) is null;

    internal static string? GetPersistenceDiagnostic(SkillBenchmarkResult result) {
        ArgumentNullException.ThrowIfNull(result);
        if (result.RedactionStatus != SkillBenchmarkRedactionStatus.Passed) {
            return RedactionFailedDiagnostic;
        }

        if (ContainsRawLocalPath(result.SanitizedDiagnostics)) {
            return SanitizationShapeDiagnostic;
        }

        if (ContainsUnsafeSummary(result.SanitizedDiagnostics)) {
            return UnsafeSummaryDiagnostic;
        }

        return HasProviderMetadata(result) ? null : MissingProviderMetadataDiagnostic;
    }

    public static SkillBenchmarkArtifactBuildResult TryBuildArtifact(SkillBenchmarkResult result) {
        if (GetPersistenceDiagnostic(result) is string diagnostic) {
            return new SkillBenchmarkArtifactBuildResult(false, [diagnostic], null);
        }

        // P-5: serialize via the source-gen context to avoid reflection-based metadata, which
        // is a known AOT/trim hazard.
        return new SkillBenchmarkArtifactBuildResult(
            true,
            [],
            JsonSerializer.Serialize(result, SkillBenchmarkJsonContext.Default.SkillBenchmarkResult));
    }

    private static bool ContainsRawLocalPath(IReadOnlyList<string> diagnostics) {
        foreach (string diagnostic in diagnostics) {
            if (LooksLikeLocalPathRegex.IsMatch(diagnostic)) {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsUnsafeSummary(IReadOnlyList<string> diagnostics) {
        foreach (string diagnostic in diagnostics) {
            string sanitized = SkillBenchmarkSummarySanitizer.Sanitize(diagnostic);
            if (!string.Equals(diagnostic, sanitized, StringComparison.Ordinal)) {
                return true;
            }
        }

        return false;
    }

    public static bool HasProviderMetadata(SkillBenchmarkResult result) {
        ArgumentNullException.ThrowIfNull(result);

        return !string.IsNullOrWhiteSpace(result.ProviderId)
            && !string.IsNullOrWhiteSpace(result.ModelId)
            && !string.IsNullOrWhiteSpace(result.ProviderConfigHash)
            && result.ProviderConfigHash.Length == 64
            && !string.IsNullOrWhiteSpace(result.FrameworkVersion)
            && !string.IsNullOrWhiteSpace(result.CorpusVersion)
            && !string.IsNullOrWhiteSpace(result.ScorerVersion)
            && !string.IsNullOrWhiteSpace(result.ValidatorVersion)
            && !string.IsNullOrWhiteSpace(result.GeneratedArtifactToken)
            && !string.IsNullOrWhiteSpace(result.SanitizedArtifactToken)
            && !string.IsNullOrWhiteSpace(result.CacheKey)
            && result.TimeoutSeconds > 0
            && result.RetryCount >= 0;
    }
}
