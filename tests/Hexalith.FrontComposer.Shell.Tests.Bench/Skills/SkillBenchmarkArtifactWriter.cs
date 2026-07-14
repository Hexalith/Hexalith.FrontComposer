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

    public static bool CanPersist(SkillBenchmarkResult result) {
        ArgumentNullException.ThrowIfNull(result);

        if (result.RedactionStatus != SkillBenchmarkRedactionStatus.Passed) {
            return false;
        }

        // P-15: sanity-check the SanitizedDiagnostics shape so a producer that mis-sets the
        // status can't bypass the persistence gate. We don't try to be a complete redactor —
        // just block obvious local-path leaks.
        return !ContainsRawLocalPath(result.SanitizedDiagnostics)
            && !ContainsUnsafeSummary(result.SanitizedDiagnostics)
            && HasProviderMetadata(result);
    }

    public static SkillBenchmarkArtifactBuildResult TryBuildArtifact(SkillBenchmarkResult result) {
        ArgumentNullException.ThrowIfNull(result);

        if (result.RedactionStatus != SkillBenchmarkRedactionStatus.Passed) {
            return new SkillBenchmarkArtifactBuildResult(false, [RedactionFailedDiagnostic], null);
        }

        if (ContainsRawLocalPath(result.SanitizedDiagnostics)) {
            return new SkillBenchmarkArtifactBuildResult(false, [SanitizationShapeDiagnostic], null);
        }

        if (ContainsUnsafeSummary(result.SanitizedDiagnostics)) {
            return new SkillBenchmarkArtifactBuildResult(false, [UnsafeSummaryDiagnostic], null);
        }

        if (!HasProviderMetadata(result)) {
            return new SkillBenchmarkArtifactBuildResult(false, [MissingProviderMetadataDiagnostic], null);
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
