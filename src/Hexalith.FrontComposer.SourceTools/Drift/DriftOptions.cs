using System.Collections.Immutable;
using System.Globalization;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Hexalith.FrontComposer.SourceTools.Drift;

internal sealed class DriftOptions {
    private DriftOptions(
        bool enabled,
        string? configuredBaselinePath,
        int maxDiagnostics,
        int maxBaselineBytes,
        bool trimOrAotAdvisoryEnabled,
        DiagnosticSeverity driftSeverity) {
        Enabled = enabled;
        ConfiguredBaselinePath = configuredBaselinePath;
        MaxDiagnostics = maxDiagnostics;
        MaxBaselineBytes = maxBaselineBytes;
        TrimOrAotAdvisoryEnabled = trimOrAotAdvisoryEnabled;
        DriftSeverity = driftSeverity;
    }

    internal bool Enabled { get; }
    internal string? ConfiguredBaselinePath { get; }
    internal int MaxDiagnostics { get; }
    internal int MaxBaselineBytes { get; }
    internal bool TrimOrAotAdvisoryEnabled { get; }
    internal DiagnosticSeverity DriftSeverity { get; }

    internal static DriftOptionsResult Bind(AnalyzerConfigOptionsProvider provider) {
        AnalyzerConfigOptions options = provider.GlobalOptions;
        ImmutableArray<DriftDiagnosticFact>.Builder diagnostics = ImmutableArray.CreateBuilder<DriftDiagnosticFact>();

        bool enabled = TryReadBool(options, "build_property.HfcDriftDetectionEnabled")
            ?? TryReadBool(options, "build_property.FrontComposerDriftDetectionEnabled")
            ?? false;
        bool trimOrAotAdvisoryEnabled =
            (TryReadBool(options, "build_property.PublishTrimmed") ?? false)
            || (TryReadBool(options, "build_property.PublishAot") ?? false);

        string? configuredBaselinePath = TryReadString(options, "build_property.HfcDriftBaselinePath");

        int maxDiagnostics = ReadPositiveInt(
            options,
            "build_property.HfcDriftMaxDiagnostics",
            DriftConstants.DefaultMaxDiagnostics,
            minInclusive: 1,
            maxInclusive: 500,
            diagnostics);

        int maxBaselineBytes = ReadPositiveInt(
            options,
            "build_property.HfcDriftMaxBaselineBytes",
            DriftConstants.DefaultMaxBaselineBytes,
            minInclusive: 1,
            maxInclusive: 10 * 1024 * 1024,
            diagnostics);

        DiagnosticSeverity severity = DiagnosticSeverity.Warning;
        string? severityValue = TryReadString(options, "build_property.HfcDriftSeverity");
        if (severityValue is not null) {
            if (string.Equals(severityValue, "Warning", StringComparison.OrdinalIgnoreCase)) {
                severity = DiagnosticSeverity.Warning;
            }
            else if (string.Equals(severityValue, "Error", StringComparison.OrdinalIgnoreCase)) {
                severity = DiagnosticSeverity.Error;
            }
            else if (string.Equals(severityValue, "Info", StringComparison.OrdinalIgnoreCase)
                || string.Equals(severityValue, "Information", StringComparison.OrdinalIgnoreCase)) {
                severity = DiagnosticSeverity.Info;
            }
            else {
                diagnostics.Add(DriftDiagnosticFact.Configuration(
                    "HfcDriftSeverity",
                    "Warning, Error, or Info",
                    severityValue));
            }
        }

        return new DriftOptionsResult(
            new DriftOptions(
                enabled,
                configuredBaselinePath,
                maxDiagnostics,
                maxBaselineBytes,
                trimOrAotAdvisoryEnabled,
                severity),
            diagnostics.ToImmutable());
    }

    private static int ReadPositiveInt(
        AnalyzerConfigOptions options,
        string key,
        int defaultValue,
        int minInclusive,
        int maxInclusive,
        ImmutableArray<DriftDiagnosticFact>.Builder diagnostics) {
        string? raw = TryReadString(options, key);
        if (raw is null) {
            return defaultValue;
        }

        // Story 9-1 P25: NumberStyles.Integer accepts a leading sign, so we explicitly enforce
        // the [minInclusive, maxInclusive] range below. NumberStyles.None previously rejected
        // valid signed forms ("+50") with a confusing diagnostic.
        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            && parsed >= minInclusive
            && parsed <= maxInclusive) {
            return parsed;
        }

        int lastDot = key.LastIndexOf('.');
        string optionDisplayName = lastDot >= 0 && lastDot + 1 < key.Length
            ? key.Substring(lastDot + 1)
            : key;
        diagnostics.Add(DriftDiagnosticFact.Configuration(
            optionDisplayName,
            minInclusive.ToString(CultureInfo.InvariantCulture) + ".." + maxInclusive.ToString(CultureInfo.InvariantCulture),
            raw));
        return defaultValue;
    }

    private static bool? TryReadBool(AnalyzerConfigOptions options, string key) {
        string? raw = TryReadString(options, key);
        if (raw is null) {
            return null;
        }

        if (string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        if (string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        return null;
    }

    private static string? TryReadString(AnalyzerConfigOptions options, string key) {
        // Story 9-1 P24: previously returned "" for both whitespace-only AND missing values
        // mixed with `null` for some paths, which caused empty <HfcDriftBaselinePath/>
        // properties to be treated as configured (and trigger a spurious HFC1059). Treat any
        // whitespace-only or absent value as "not configured" (null).
        if (!options.TryGetValue(key, out string? value)) {
            return null;
        }

        string trimmed = value?.Trim() ?? string.Empty;
        return trimmed.Length == 0 ? null : trimmed;
    }
}
