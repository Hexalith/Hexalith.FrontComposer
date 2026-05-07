using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.FrontComposer.SourceTools.Diagnostics;
using Hexalith.FrontComposer.SourceTools.Parsing;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Hexalith.FrontComposer.SourceTools.Drift;

internal static class DriftConstants {
    internal const string SchemaVersion = "frontcomposer.generated-ui-baseline.v1";
    internal const string Algorithm = "frontcomposer-structural-v1";
    internal const int DefaultMaxDiagnostics = 50;
    internal const int DefaultMaxBaselineBytes = 256 * 1024;
    internal const int DefaultMaxDeclarations = 512;
    internal const int DefaultMaxPropertiesPerDeclaration = 256;

    internal const string MissingBaselineId = "HFC1058";
    internal const string InvalidBaselinePathId = "HFC1059";
    internal const string InvalidBaselineContentId = "HFC1060";
    internal const string UnsupportedSchemaId = "HFC1061";
    internal const string UnsupportedAlgorithmId = "HFC1062";
    internal const string BaselineBoundsExceededId = "HFC1063";
    internal const string DuplicateOrInvariantId = "HFC1064";
    internal const string StructuralDriftId = "HFC1065";
    internal const string MetadataDriftId = "HFC1066";
    internal const string InvalidOptionId = "HFC1067";
    internal const string TruncationId = "HFC1068";
    internal const string RedactionSuppressedId = "HFC1069";
    internal const string TrimAotReflectionCatalogId = "HFC1070";
}

internal sealed class DriftOptionsResult(
    DriftOptions options,
    ImmutableArray<DriftDiagnosticFact> diagnostics) {
    internal DriftOptions Options { get; } = options;
    internal ImmutableArray<DriftDiagnosticFact> Diagnostics { get; } = diagnostics;
}

internal sealed class DriftOptions {
    private DriftOptions(
        bool enabled,
        string? configuredBaselinePath,
        int maxDiagnostics,
        int maxBaselineBytes,
        bool publishTrimmed,
        DiagnosticSeverity driftSeverity) {
        Enabled = enabled;
        ConfiguredBaselinePath = configuredBaselinePath;
        MaxDiagnostics = maxDiagnostics;
        MaxBaselineBytes = maxBaselineBytes;
        PublishTrimmed = publishTrimmed;
        DriftSeverity = driftSeverity;
    }

    internal bool Enabled { get; }
    internal string? ConfiguredBaselinePath { get; }
    internal int MaxDiagnostics { get; }
    internal int MaxBaselineBytes { get; }
    internal bool PublishTrimmed { get; }
    internal DiagnosticSeverity DriftSeverity { get; }

    internal static DriftOptionsResult Bind(AnalyzerConfigOptionsProvider provider) {
        AnalyzerConfigOptions options = provider.GlobalOptions;
        ImmutableArray<DriftDiagnosticFact>.Builder diagnostics = ImmutableArray.CreateBuilder<DriftDiagnosticFact>();

        bool enabled = TryReadBool(options, "build_property.HfcDriftDetectionEnabled")
            ?? TryReadBool(options, "build_property.FrontComposerDriftDetectionEnabled")
            ?? false;
        bool publishTrimmed = TryReadBool(options, "build_property.PublishTrimmed")
            ?? TryReadBool(options, "build_property.PublishAot")
            ?? false;

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
                publishTrimmed,
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

internal sealed class DriftBaselineInput(string path, string text) : IEquatable<DriftBaselineInput> {
    internal string Path { get; } = path;
    internal string Text { get; } = text;

    internal static bool IsCandidate(string path) {
        // Story 9-1 P5 (T5): enforce the documented baseline naming contract. Previously every
        // *.json AdditionalText was treated as a candidate baseline, so an unrelated config file
        // would produce HFC1060/HFC1064 errors. Accepted prefixes mirror the schemaVersion
        // family ("frontcomposer.generated-ui-baseline*") and the historic short form
        // ("frontcomposer.drift-baseline*").
        string fileName = System.IO.Path.GetFileName(path);
        if (string.IsNullOrEmpty(fileName)) {
            return false;
        }

        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        return fileName.StartsWith("frontcomposer.drift-baseline", StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith("frontcomposer.generated-ui-baseline", StringComparison.OrdinalIgnoreCase);
    }

    internal static DriftBaselineInput FromAdditionalText(AdditionalText text, CancellationToken cancellationToken) {
        SourceText? source = text.GetText(cancellationToken);
        return new DriftBaselineInput(text.Path, source?.ToString() ?? string.Empty);
    }

    public bool Equals(DriftBaselineInput? other)
        => other is not null && Path == other.Path && Text == other.Text;

    public override bool Equals(object? obj) => Equals(obj as DriftBaselineInput);

    public override int GetHashCode() {
        unchecked {
            return ((Path?.GetHashCode() ?? 0) * 397) ^ (Text?.GetHashCode() ?? 0);
        }
    }
}

internal sealed class DriftBaselineLoadResult(
    bool comparisonEnabled,
    DriftBaselineSet baseline,
    ImmutableArray<DriftDiagnosticFact> diagnostics) {
    internal bool ComparisonEnabled { get; } = comparisonEnabled;
    internal DriftBaselineSet Baseline { get; } = baseline;
    internal ImmutableArray<DriftDiagnosticFact> Diagnostics { get; } = diagnostics;
}

internal sealed class DriftBaselineSet(ImmutableArray<DriftBaselineContract> contracts) {
    internal ImmutableArray<DriftBaselineContract> Contracts { get; } = contracts;
}

internal sealed class DriftBaselineContract(
    string sourcePath,
    string family,
    string type,
    string boundedContext,
    string? displayName,
    string? displayGroupName,
    string? role,
    string? icon,
    bool? destructive,
    string? requiresPolicy,
    string? emptyStateCtaCommandTypeName,
    ImmutableArray<DriftBaselineProperty> properties) {
    internal string SourcePath { get; } = sourcePath;
    internal string Family { get; } = family;
    internal string Type { get; } = type;
    internal string BoundedContext { get; } = boundedContext;
    internal string? DisplayName { get; } = displayName;
    internal string? DisplayGroupName { get; } = displayGroupName;
    internal string? Role { get; } = role;
    internal string? Icon { get; } = icon;
    internal bool? Destructive { get; } = destructive;
    internal string? RequiresPolicy { get; } = requiresPolicy;
    /// <summary>Story 9-1 P6 (AC7): contract-level <c>[ProjectionEmptyStateCta]</c> target command type name.</summary>
    internal string? EmptyStateCtaCommandTypeName { get; } = emptyStateCtaCommandTypeName;
    internal ImmutableArray<DriftBaselineProperty> Properties { get; } = properties;

    internal string IdentityWithoutContext => Family + "|" + Type;
    internal string IdentityWithContext => Family + "|" + Type + "|" + BoundedContext;
}

internal sealed class DriftBaselineProperty(
    string name,
    string category,
    bool nullable,
    bool? derivable,
    string? displayName,
    string? description,
    int? columnPriority,
    string? fieldGroup,
    string? displayFormat,
    int? relativeTimeWindowDays,
    string? badgeSignature) {
    internal string Name { get; } = name;
    internal string Category { get; } = category;
    internal bool Nullable { get; } = nullable;
    internal bool? Derivable { get; } = derivable;
    internal string? DisplayName { get; } = displayName;
    internal string? Description { get; } = description;
    internal int? ColumnPriority { get; } = columnPriority;
    internal string? FieldGroup { get; } = fieldGroup;
    internal string? DisplayFormat { get; } = displayFormat;
    /// <summary>Story 9-1 P6 (AC7): days window for <c>FieldDisplayFormat.RelativeTime</c>; <c>null</c> when not relative-time.</summary>
    internal int? RelativeTimeWindowDays { get; } = relativeTimeWindowDays;
    /// <summary>Story 9-1 P6 (AC7): canonical signature of <c>[ProjectionBadge]</c> mappings — comma-joined <c>"EnumMember=Slot"</c> entries ordered ordinally; <c>null</c> when no badge mappings.</summary>
    internal string? BadgeSignature { get; } = badgeSignature;
}

internal static class DriftBaselineLoader {
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    internal static DriftBaselineLoadResult Load(ImmutableArray<DriftBaselineInput> inputs, DriftOptions options) {
        ImmutableArray<DriftDiagnosticFact>.Builder diagnostics = ImmutableArray.CreateBuilder<DriftDiagnosticFact>();
        ImmutableArray<DriftBaselineContract>.Builder contracts = ImmutableArray.CreateBuilder<DriftBaselineContract>();

        ImmutableArray<DriftBaselineInput> sorted = inputs
            .OrderBy(static i => i.Path, StringComparer.Ordinal)
            .ToImmutableArray();

        // Story 9-1 P8: when ConfiguredBaselinePath is set, narrow the eligible baselines to
        // matching candidates. If none match, surface HFC1059 against the configured path (it
        // is a trust failure for the configured path only — but per AC9 we still fail-closed
        // because there is no other authoritative input).
        ImmutableArray<DriftBaselineInput> eligible = sorted;
        if (options.ConfiguredBaselinePath is not null) {
            eligible = sorted
                .Where(i => PathsEqual(options.ConfiguredBaselinePath, i.Path))
                .ToImmutableArray();
            if (eligible.Length == 0) {
                diagnostics.Add(DriftDiagnosticFact.InvalidBaselinePath(options.ConfiguredBaselinePath));
                return EmptyResult(diagnostics);
            }
        }

        if (eligible.Length == 0) {
            // Story 9-1 P3 (AC2): the baseline-load path emits HFC1058 unconditionally when
            // drift detection is enabled and no matching baseline exists. Higher up the stack
            // the orchestrator already gates on options.Enabled so this only runs in the
            // opt-in case.
            diagnostics.Add(DriftDiagnosticFact.MissingBaseline());
            return EmptyResult(diagnostics);
        }

        HashSet<string> contractIdentities = new(StringComparer.Ordinal);
        HashSet<string> unsafeValues = new(StringComparer.Ordinal);
        bool trustFailed = false;

        foreach (DriftBaselineInput input in eligible) {
            // Story 9-1 P10: count UTF-8 bytes, not chars — option name is MaxBaselineBytes.
            int byteCount = Encoding.UTF8.GetByteCount(input.Text);
            if (byteCount > options.MaxBaselineBytes) {
                diagnostics.Add(DriftDiagnosticFact.TrustFailure(
                    DriftConstants.BaselineBoundsExceededId,
                    "oversized baseline",
                    "at most " + options.MaxBaselineBytes.ToString(CultureInfo.InvariantCulture) + " bytes",
                    byteCount.ToString(CultureInfo.InvariantCulture),
                    input.Path));
                trustFailed = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(input.Text)) {
                diagnostics.Add(DriftDiagnosticFact.TrustFailure(
                    DriftConstants.InvalidBaselineContentId,
                    "empty baseline",
                    "valid JSON baseline content",
                    "empty or whitespace",
                    input.Path));
                trustFailed = true;
                continue;
            }

            // Story 9-1 P11: strip a leading UTF-8 BOM so editors that save with a BOM (very
            // common on Windows) do not produce HFC1060 against an otherwise valid baseline.
            string textForParse = StripUtf8Bom(input.Text);

            JsonDocument document;
            try {
                document = JsonDocument.Parse(textForParse);
            }
            catch (JsonException) {
                diagnostics.Add(DriftDiagnosticFact.TrustFailure(
                    DriftConstants.InvalidBaselineContentId,
                    "malformed baseline",
                    "valid JSON baseline content",
                    "malformed JSON",
                    input.Path));
                trustFailed = true;
                continue;
            }

            using (document) {
                JsonElement root = document.RootElement;
                // Story 9-1 P12: trim+ordinal compare and clamp displayed `Got` values through
                // the sanitizer so untrusted JSON cannot leak into the diagnostic property bag.
                string schemaVersion = (ReadString(root, "schemaVersion") ?? string.Empty).Trim();
                string algorithm = (ReadString(root, "algorithm") ?? string.Empty).Trim();

                if (!string.Equals(schemaVersion, DriftConstants.SchemaVersion, StringComparison.Ordinal)) {
                    diagnostics.Add(DriftDiagnosticFact.TrustFailure(
                        DriftConstants.UnsupportedSchemaId,
                        "unsupported schema version",
                        DriftConstants.SchemaVersion,
                        schemaVersion,
                        input.Path,
                        schemaVersion,
                        algorithm));
                    trustFailed = true;
                    continue;
                }

                if (!string.Equals(algorithm, DriftConstants.Algorithm, StringComparison.Ordinal)) {
                    diagnostics.Add(DriftDiagnosticFact.TrustFailure(
                        DriftConstants.UnsupportedAlgorithmId,
                        "unsupported algorithm",
                        DriftConstants.Algorithm,
                        algorithm,
                        input.Path,
                        schemaVersion,
                        algorithm));
                    trustFailed = true;
                    continue;
                }

                if (!root.TryGetProperty("contracts", out JsonElement contractArray)
                    || contractArray.ValueKind != JsonValueKind.Array) {
                    diagnostics.Add(Invariant(input.Path, "contracts array is missing"));
                    trustFailed = true;
                    continue;
                }

                if (contractArray.GetArrayLength() > DriftConstants.DefaultMaxDeclarations) {
                    diagnostics.Add(DriftDiagnosticFact.TrustFailure(
                        DriftConstants.BaselineBoundsExceededId,
                        "declaration count exceeded",
                        DriftConstants.DefaultMaxDeclarations.ToString(CultureInfo.InvariantCulture),
                        contractArray.GetArrayLength().ToString(CultureInfo.InvariantCulture),
                        input.Path));
                    trustFailed = true;
                    continue;
                }

                foreach (JsonElement contractElement in contractArray.EnumerateArray()) {
                    DriftBaselineContract? contract = ParseContract(input.Path, contractElement, unsafeValues, diagnostics);
                    if (contract is null) {
                        trustFailed = true;
                        continue;
                    }

                    if (!contractIdentities.Add(contract.IdentityWithContext)) {
                        diagnostics.Add(DriftDiagnosticFact.TrustFailure(
                            DriftConstants.DuplicateOrInvariantId,
                            "duplicate identity",
                            "unique contract identity",
                            contract.Type,
                            input.Path));
                        trustFailed = true;
                        continue;
                    }

                    contracts.Add(contract);
                }
            }
        }

        if (unsafeValues.Count > 0) {
            diagnostics.Add(DriftDiagnosticFact.RedactionSuppressed());
            trustFailed = true;
        }

        // Story 9-1 P29: when trust failed, drop the partially-parsed contracts so the
        // fail-closed contract is self-evident — even though `ComparisonEnabled=false` already
        // halts comparison, leaving the half-populated set in `Baseline` was misleading.
        if (trustFailed) {
            return EmptyResult(diagnostics);
        }

        return new DriftBaselineLoadResult(true, new DriftBaselineSet(contracts.ToImmutable()), diagnostics.ToImmutable());
    }

    private static DriftBaselineLoadResult EmptyResult(ImmutableArray<DriftDiagnosticFact>.Builder diagnostics)
        => new(
            comparisonEnabled: false,
            baseline: new DriftBaselineSet(ImmutableArray<DriftBaselineContract>.Empty),
            diagnostics: diagnostics.ToImmutable());

    private static string StripUtf8Bom(string text) {
        if (text.Length > 0 && text[0] == '﻿') {
            return text.Substring(1);
        }

        // Defensive: also strip the literal BOM byte sequence if it survived as UTF-8 chars.
        if (text.Length >= Utf8Bom.Length
            && (byte)text[0] == Utf8Bom[0]
            && (byte)text[1] == Utf8Bom[1]
            && (byte)text[2] == Utf8Bom[2]) {
            return text.Substring(Utf8Bom.Length);
        }

        return text;
    }

    private static DriftBaselineContract? ParseContract(
        string path,
        JsonElement contractElement,
        HashSet<string> unsafeValues,
        ImmutableArray<DriftDiagnosticFact>.Builder diagnostics) {
        string family = ReadString(contractElement, "family") ?? string.Empty;
        string type = ReadString(contractElement, "type") ?? string.Empty;
        string boundedContext = ReadString(contractElement, "boundedContext") ?? string.Empty;

        TrackUnsafe(family, unsafeValues);
        TrackUnsafe(type, unsafeValues);
        TrackUnsafe(boundedContext, unsafeValues);

        if ((family != "projection" && family != "command" && family != "boundedContext")
            || string.IsNullOrWhiteSpace(type)) {
            diagnostics.Add(Invariant(path, "contract invariant violation"));
            return null;
        }

        if (!contractElement.TryGetProperty("properties", out JsonElement propertyArray)
            || propertyArray.ValueKind != JsonValueKind.Array) {
            propertyArray = default;
        }

        ImmutableArray<DriftBaselineProperty>.Builder properties = ImmutableArray.CreateBuilder<DriftBaselineProperty>();
        // Story 9-1 P13: dedupe by OrdinalIgnoreCase so a baseline declaring both "Foo" and
        // "foo" is rejected as an invariant violation here (rather than allowed through to
        // CompareContract.ToDictionary(Ordinal) which would silently keep both, then risk a
        // case-collision crash if downstream consumers normalize names).
        HashSet<string> propertyNames = new(StringComparer.OrdinalIgnoreCase);
        if (propertyArray.ValueKind == JsonValueKind.Array) {
            if (propertyArray.GetArrayLength() > DriftConstants.DefaultMaxPropertiesPerDeclaration) {
                diagnostics.Add(DriftDiagnosticFact.TrustFailure(
                    DriftConstants.BaselineBoundsExceededId,
                    "property count exceeded",
                    DriftConstants.DefaultMaxPropertiesPerDeclaration.ToString(CultureInfo.InvariantCulture),
                    propertyArray.GetArrayLength().ToString(CultureInfo.InvariantCulture),
                    path));
                return null;
            }

            foreach (JsonElement propertyElement in propertyArray.EnumerateArray()) {
                string name = ReadString(propertyElement, "name") ?? string.Empty;
                string category = ReadString(propertyElement, "category") ?? string.Empty;
                bool nullable = ReadBool(propertyElement, "nullable") ?? false;

                TrackUnsafe(name, unsafeValues);
                TrackUnsafe(category, unsafeValues);
                TrackUnsafe(ReadString(propertyElement, "displayName"), unsafeValues);
                TrackUnsafe(ReadString(propertyElement, "description"), unsafeValues);
                TrackUnsafe(ReadString(propertyElement, "fieldGroup"), unsafeValues);
                TrackUnsafe(ReadString(propertyElement, "displayFormat"), unsafeValues);

                if (string.IsNullOrWhiteSpace(name)
                    || string.IsNullOrWhiteSpace(category)
                    || !propertyNames.Add(name)
                    || (TryReadInt(propertyElement, "columnPriority") is int priority && priority < 0)) {
                    diagnostics.Add(Invariant(path, "property invariant violation"));
                    return null;
                }

                // Story 9-1 P6 (AC7): canonical badge signature + relative-time window so
                // ProjectionBadge / RelativeTime metadata changes produce drift diagnostics.
                string? badgeSignature = ReadBadgeSignature(propertyElement);
                TrackUnsafe(badgeSignature, unsafeValues);

                properties.Add(new DriftBaselineProperty(
                    name,
                    category,
                    nullable,
                    ReadBool(propertyElement, "derivable"),
                    ReadString(propertyElement, "displayName"),
                    ReadString(propertyElement, "description"),
                    TryReadInt(propertyElement, "columnPriority"),
                    ReadString(propertyElement, "fieldGroup"),
                    ReadString(propertyElement, "displayFormat"),
                    TryReadInt(propertyElement, "relativeTimeWindowDays"),
                    badgeSignature));
            }
        }

        string? displayName = ReadString(contractElement, "displayName");
        string? displayGroupName = ReadString(contractElement, "displayGroupName");
        string? role = ReadString(contractElement, "role");
        string? icon = ReadString(contractElement, "icon");
        string? requiresPolicy = ReadString(contractElement, "requiresPolicy");
        string? emptyStateCta = ReadString(contractElement, "emptyStateCtaCommandTypeName");

        TrackUnsafe(displayName, unsafeValues);
        TrackUnsafe(displayGroupName, unsafeValues);
        TrackUnsafe(role, unsafeValues);
        TrackUnsafe(icon, unsafeValues);
        TrackUnsafe(requiresPolicy, unsafeValues);
        TrackUnsafe(emptyStateCta, unsafeValues);

        return new DriftBaselineContract(
            path,
            family,
            type,
            boundedContext,
            displayName,
            displayGroupName,
            role,
            icon,
            ReadBool(contractElement, "destructive"),
            requiresPolicy,
            emptyStateCta,
            properties.ToImmutable());
    }

    /// <summary>
    /// Story 9-1 P6 (AC7): parses <c>"badges": [ {"enumMember": "...", "slot": "..."}, ... ]</c>
    /// into a deterministic comma-joined <c>"EnumMember=Slot"</c> signature ordered ordinally.
    /// Returns <c>null</c> when the <c>badges</c> property is absent or empty so that
    /// <c>AddMetadataIfChanged(null, signature)</c> still surfaces a null→value addition.
    /// </summary>
    private static string? ReadBadgeSignature(JsonElement propertyElement) {
        if (propertyElement.ValueKind != JsonValueKind.Object
            || !propertyElement.TryGetProperty("badges", out JsonElement badges)
            || badges.ValueKind != JsonValueKind.Array
            || badges.GetArrayLength() == 0) {
            return null;
        }

        List<string> entries = [];
        foreach (JsonElement badge in badges.EnumerateArray()) {
            string? enumMember = ReadString(badge, "enumMember");
            string? slot = ReadString(badge, "slot");
            if (string.IsNullOrEmpty(enumMember) || string.IsNullOrEmpty(slot)) {
                continue;
            }

            entries.Add(enumMember + "=" + slot);
        }

        if (entries.Count == 0) {
            return null;
        }

        entries.Sort(StringComparer.Ordinal);
        return string.Join(",", entries);
    }

    private static DriftDiagnosticFact Invariant(string path, string got)
        => DriftDiagnosticFact.TrustFailure(
            DriftConstants.DuplicateOrInvariantId,
            "invariant violation",
            "non-empty unique structural baseline identities",
            got,
            path);

    private static string? ReadString(JsonElement element, string propertyName) {
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(propertyName, out JsonElement value)
            && value.ValueKind == JsonValueKind.String) {
            return value.GetString();
        }

        return null;
    }

    private static bool? ReadBool(JsonElement element, string propertyName) {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(propertyName, out JsonElement value)) {
            return null;
        }

        return value.ValueKind switch {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    private static int? TryReadInt(JsonElement element, string propertyName) {
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(propertyName, out JsonElement value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out int result)) {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Story 9-1 P1 (security-adjacent): determines whether <paramref name="configured"/>
    /// (the value adopters set in <c>HfcDriftBaselinePath</c>) refers to the same file as
    /// <paramref name="candidate"/> (a path coming from <c>AdditionalTextsProvider</c>).
    /// The previous implementation used an unbounded <c>EndsWith</c> which silently matched
    /// <c>baseline.json</c> against any file whose name ended in <c>baseline.json</c>,
    /// allowing a wrong baseline to be trusted. This version requires either an exact
    /// (normalized, case-insensitive) match or a segment-aligned suffix match — i.e. the
    /// shorter side must be preceded by a path separator in the longer side.
    /// </summary>
    private static bool PathsEqual(string configured, string candidate) {
        string normConfigured = PathCompareNormalize(configured);
        string normCandidate = PathCompareNormalize(candidate);
        if (string.Equals(normConfigured, normCandidate, StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        if (IsSegmentAlignedSuffix(normConfigured, normCandidate)) {
            return true;
        }

        return IsSegmentAlignedSuffix(normCandidate, normConfigured);
    }

    private static bool IsSegmentAlignedSuffix(string longer, string shorter) {
        if (longer.Length <= shorter.Length) {
            return false;
        }

        if (!longer.EndsWith(shorter, StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        char boundary = longer[longer.Length - shorter.Length - 1];
        return boundary == '/';
    }

    private static string PathCompareNormalize(string path)
        => (path ?? string.Empty).Replace('\\', '/').TrimStart('/');

    private static void TrackUnsafe(string? value, HashSet<string> unsafeValues) {
        if (value is not null && DriftSanitizer.IsUnsafe(value)) {
            unsafeValues.Add(value);
        }
    }
}

internal sealed class DriftCurrentContract(
    string family,
    string type,
    string boundedContext,
    string? displayName,
    string? displayGroupName,
    string? role,
    string? icon,
    bool? destructive,
    string? requiresPolicy,
    string? emptyStateCtaCommandTypeName,
    ImmutableArray<DriftCurrentProperty> properties,
    string sourcePath,
    int sourceLine,
    int sourceColumn) {
    internal string Family { get; } = family;
    internal string Type { get; } = type;
    internal string BoundedContext { get; } = boundedContext;
    internal string? DisplayName { get; } = displayName;
    internal string? DisplayGroupName { get; } = displayGroupName;
    internal string? Role { get; } = role;
    internal string? Icon { get; } = icon;
    internal bool? Destructive { get; } = destructive;
    internal string? RequiresPolicy { get; } = requiresPolicy;
    /// <summary>Story 9-1 P6 (AC7): contract-level <c>[ProjectionEmptyStateCta]</c> target command type name.</summary>
    internal string? EmptyStateCtaCommandTypeName { get; } = emptyStateCtaCommandTypeName;
    internal ImmutableArray<DriftCurrentProperty> Properties { get; } = properties;
    internal string SourcePath { get; } = sourcePath;
    internal int SourceLine { get; } = sourceLine;
    internal int SourceColumn { get; } = sourceColumn;
    internal string IdentityWithoutContext => Family + "|" + Type;
}

internal sealed class DriftCurrentProperty(
    string name,
    string category,
    bool nullable,
    bool? derivable,
    string? displayName,
    string? description,
    int? columnPriority,
    string? fieldGroup,
    string? displayFormat,
    int? relativeTimeWindowDays,
    string? badgeSignature) {
    internal string Name { get; } = name;
    internal string Category { get; } = category;
    internal bool Nullable { get; } = nullable;
    internal bool? Derivable { get; } = derivable;
    internal string? DisplayName { get; } = displayName;
    internal string? Description { get; } = description;
    internal int? ColumnPriority { get; } = columnPriority;
    internal string? FieldGroup { get; } = fieldGroup;
    internal string? DisplayFormat { get; } = displayFormat;
    /// <summary>Story 9-1 P6 (AC7): days window for <c>FieldDisplayFormat.RelativeTime</c>; <c>null</c> when not relative-time.</summary>
    internal int? RelativeTimeWindowDays { get; } = relativeTimeWindowDays;
    /// <summary>Story 9-1 P6 (AC7): canonical signature of <c>[ProjectionBadge]</c> mappings — comma-joined <c>"EnumMember=Slot"</c> entries ordered ordinally; <c>null</c> when no badge mappings.</summary>
    internal string? BadgeSignature { get; } = badgeSignature;
}

internal sealed class DriftCurrentSnapshot(ImmutableArray<DriftCurrentContract> contracts) {
    internal ImmutableArray<DriftCurrentContract> Contracts { get; } = contracts;

    internal static DriftCurrentSnapshot From(
        ImmutableArray<ParseResult> projections,
        ImmutableArray<CommandParseResult> commands) {
        ImmutableArray<DriftCurrentContract>.Builder contracts = ImmutableArray.CreateBuilder<DriftCurrentContract>();

        foreach (ParseResult result in projections) {
            if (result.Model is not DomainModel model) {
                continue;
            }

            ImmutableArray<DriftCurrentProperty>.Builder properties = ImmutableArray.CreateBuilder<DriftCurrentProperty>();
            foreach (PropertyModel property in model.Properties) {
                properties.Add(new DriftCurrentProperty(
                    property.Name,
                    property.TypeName,
                    property.IsNullable,
                    null,
                    property.DisplayName,
                    property.Description,
                    property.ColumnPriority,
                    property.FieldGroup,
                    // Story 9-1 P-D3: serialize Default as null so a baseline missing the
                    // displayFormat field aligns with a current property using Default.
                    property.DisplayFormat == FieldDisplayFormat.Default ? null : property.DisplayFormat.ToString(),
                    property.RelativeTimeWindowDays,
                    BuildBadgeSignature(property.BadgeMappings)));
            }

            contracts.Add(new DriftCurrentContract(
                "projection",
                QualifiedType(model.Namespace, model.TypeName),
                model.BoundedContext ?? string.Empty,
                model.DisplayName,
                model.DisplayGroupName,
                model.ProjectionRole,
                null,
                null,
                null,
                model.EmptyStateCtaCommandTypeName,
                properties.ToImmutable(),
                model.SourceFilePath,
                model.SourceLine,
                model.SourceColumn));
        }

        foreach (CommandParseResult result in commands) {
            if (result.Model is not CommandModel model) {
                continue;
            }

            ImmutableArray<DriftCurrentProperty>.Builder properties = ImmutableArray.CreateBuilder<DriftCurrentProperty>();
            HashSet<string> derivable = new(model.DerivableProperties.Select(static p => p.Name), StringComparer.Ordinal);
            foreach (PropertyModel property in model.Properties) {
                properties.Add(new DriftCurrentProperty(
                    property.Name,
                    property.TypeName,
                    property.IsNullable,
                    derivable.Contains(property.Name),
                    property.DisplayName,
                    property.Description,
                    property.ColumnPriority,
                    property.FieldGroup,
                    property.DisplayFormat == FieldDisplayFormat.Default ? null : property.DisplayFormat.ToString(),
                    property.RelativeTimeWindowDays,
                    BuildBadgeSignature(property.BadgeMappings)));
            }

            // Story 9-1 P22: thread CommandModel source path/line/column so command drift
            // diagnostics get IDE squiggles like projection drift does.
            contracts.Add(new DriftCurrentContract(
                "command",
                QualifiedType(model.Namespace, model.TypeName),
                model.BoundedContext ?? string.Empty,
                model.DisplayName,
                null,
                null,
                model.IconName,
                model.IsDestructive,
                model.AuthorizationPolicyName,
                emptyStateCtaCommandTypeName: null,
                properties.ToImmutable(),
                model.SourceFilePath,
                model.SourceLine,
                model.SourceColumn));
        }

        return new DriftCurrentSnapshot(contracts.ToImmutable());
    }

    private static string QualifiedType(string @namespace, string typeName)
        => string.IsNullOrEmpty(@namespace) ? typeName : @namespace + "." + typeName;

    /// <summary>
    /// Story 9-1 P6 (AC7): produces the canonical badge signature used by drift comparison.
    /// Mirrors <see cref="DriftBaselineLoader"/>'s <c>ReadBadgeSignature</c> output shape so
    /// baseline and current-source signatures collide exactly when the mapping set matches,
    /// regardless of declaration order.
    /// </summary>
    private static string? BuildBadgeSignature(EquatableArray<BadgeMappingEntry> mappings) {
        if (mappings.Count == 0) {
            return null;
        }

        List<string> entries = new(mappings.Count);
        foreach (BadgeMappingEntry entry in mappings) {
            entries.Add(entry.EnumMemberName + "=" + entry.Slot);
        }

        entries.Sort(StringComparer.Ordinal);
        return string.Join(",", entries);
    }
}

internal sealed class DriftComparisonResult(ImmutableArray<DriftDiagnosticFact> diagnostics) {
    // Story 9-1 P27: tightened from `public` to `internal` to match the comparison-seam contract
    // ("internal deterministic comparison service/result model"). Previously the `public`
    // modifier on an internal type signaled incipient public-surface intent.
    internal ImmutableArray<DriftDiagnosticFact> Diagnostics { get; } = diagnostics;
}

internal sealed class DriftComparisonService {
    // Story 9-1 P27: tightened from `public` to `internal` (see DriftComparisonResult).
    internal static DriftComparisonResult Compare(DriftCurrentSnapshot current, DriftBaselineSet baseline)
        => Compare(current, baseline, DriftConstants.DefaultMaxDiagnostics, DiagnosticSeverity.Warning);

    internal static DriftComparisonResult Compare(
        DriftCurrentSnapshot current,
        DriftBaselineSet baseline,
        int maxDiagnostics,
        DiagnosticSeverity severity) {
        ImmutableArray<DriftDiagnosticFact>.Builder facts = ImmutableArray.CreateBuilder<DriftDiagnosticFact>();
        Dictionary<string, DriftCurrentContract> currentByType = new(StringComparer.Ordinal);
        foreach (DriftCurrentContract contract in current.Contracts) {
            currentByType[contract.IdentityWithoutContext] = contract;
        }

        HashSet<string> baselineKeys = new(StringComparer.Ordinal);
        foreach (DriftBaselineContract baselineContract in baseline.Contracts) {
            baselineKeys.Add(baselineContract.IdentityWithoutContext);
            if (!currentByType.TryGetValue(baselineContract.IdentityWithoutContext, out DriftCurrentContract? currentContract)) {
                facts.Add(DriftDiagnosticFact.Structural(
                    "RemovedDeclaration",
                    baselineContract,
                    null,
                    null,
                    "What: structural drift removed declaration. Expected: declaration " + DriftSanitizer.Safe(baselineContract.Type) + ". Got: declaration not found. Fix: restore the declaration or update the checked-in generated UI baseline. DocsLink: " + Docs(DriftConstants.StructuralDriftId),
                    severity));
                continue;
            }

            CompareContract(baselineContract, currentContract, severity, facts);
        }

        foreach (DriftCurrentContract currentContract in current.Contracts) {
            if (baselineKeys.Contains(currentContract.IdentityWithoutContext)) {
                continue;
            }

            facts.Add(DriftDiagnosticFact.Structural(
                "AddedDeclaration",
                null,
                currentContract,
                null,
                "What: structural drift added declaration " + DriftSanitizer.Safe(currentContract.Type) + ". Expected: declaration from baseline. Got: new generated UI declaration. Fix: update the checked-in generated UI baseline if intentional. DocsLink: " + Docs(DriftConstants.StructuralDriftId),
                severity));
        }

        ImmutableArray<DriftDiagnosticFact> sorted = facts
            .OrderBy(static f => f.SortKey, StringComparer.Ordinal)
            .ToImmutableArray();

        if (sorted.Length <= maxDiagnostics) {
            return new DriftComparisonResult(sorted);
        }

        ImmutableArray<DriftDiagnosticFact>.Builder capped = ImmutableArray.CreateBuilder<DriftDiagnosticFact>();
        capped.AddRange(sorted.Take(maxDiagnostics));
        capped.Add(DriftDiagnosticFact.Truncation(sorted.Length - maxDiagnostics));
        return new DriftComparisonResult(capped.ToImmutable());
    }

    private static void CompareContract(
        DriftBaselineContract baseline,
        DriftCurrentContract current,
        DiagnosticSeverity severity,
        ImmutableArray<DriftDiagnosticFact>.Builder facts) {
        if (!string.Equals(baseline.BoundedContext, current.BoundedContext, StringComparison.Ordinal)) {
            facts.Add(DriftDiagnosticFact.Structural(
                "BoundedContextChanged",
                baseline,
                current,
                null,
                "What: bounded context drift on " + DriftSanitizer.Safe(current.Type) + ". Expected: " + DriftSanitizer.Safe(baseline.BoundedContext) + ". Got: " + DriftSanitizer.Safe(current.BoundedContext) + ". Fix: reconcile the generated UI baseline; navigation grouping, generated registration, persisted session grouping, MCP resource grouping, and badge/action queue grouping may be affected. DocsLink: " + Docs(DriftConstants.StructuralDriftId),
                severity));
        }

        CompareContractMetadata(baseline, current, severity, facts);

        Dictionary<string, DriftBaselineProperty> baselineProperties = baseline.Properties.ToDictionary(static p => p.Name, StringComparer.Ordinal);
        Dictionary<string, DriftCurrentProperty> currentProperties = current.Properties.ToDictionary(static p => p.Name, StringComparer.Ordinal);
        List<DriftBaselineProperty> removed = [.. baseline.Properties.Where(p => !currentProperties.ContainsKey(p.Name))];
        List<DriftCurrentProperty> added = [.. current.Properties.Where(p => !baselineProperties.ContainsKey(p.Name))];

        if (removed.Count == 1
            && added.Count == 1
            && string.Equals(removed[0].Category, added[0].Category, StringComparison.Ordinal)) {
            string id = DriftConstants.StructuralDriftId;
            string message = "Property '" + DriftSanitizer.Safe(removed[0].Name) + "' was expected on " + DriftSanitizer.Safe(baseline.Type) + " but not found. '" + DriftSanitizer.Safe(added[0].Name) + "' was added. If this is a rename, update the generated output. See " + id + ". Expected: " + DriftSanitizer.Safe(removed[0].Category) + ". Got: " + DriftSanitizer.Safe(added[0].Category) + ". Fix: update the checked-in generated UI baseline. DocsLink: " + Docs(id);
            facts.Add(DriftDiagnosticFact.Structural("Rename", baseline, current, added[0].Name, message, severity));
        }
        else {
            foreach (DriftBaselineProperty property in removed) {
                facts.Add(DriftDiagnosticFact.Structural(
                    "RemovedProperty",
                    baseline,
                    current,
                    property.Name,
                    "What: structural drift removed property. Expected: property '" + DriftSanitizer.Safe(property.Name) + "' on " + DriftSanitizer.Safe(baseline.Type) + ". Got: property not found. Fix: restore the member or update the checked-in generated UI baseline. Affected surface: " + SurfaceFor(baseline.Family, property.Category) + ". DocsLink: " + Docs(DriftConstants.StructuralDriftId),
                    severity));
            }

            foreach (DriftCurrentProperty property in added) {
                facts.Add(DriftDiagnosticFact.Structural(
                    "AddedProperty",
                    baseline,
                    current,
                    property.Name,
                    "What: structural drift added property. Expected: no property '" + DriftSanitizer.Safe(property.Name) + "' in baseline. Got: property added on " + DriftSanitizer.Safe(current.Type) + ". Fix: update the checked-in generated UI baseline if intentional. Affected surface: " + SurfaceFor(current.Family, property.Category) + ". DocsLink: " + Docs(DriftConstants.StructuralDriftId),
                    severity));
            }
        }

        foreach (KeyValuePair<string, DriftBaselineProperty> kvp in baselineProperties.OrderBy(static p => p.Key, StringComparer.Ordinal)) {
            if (!currentProperties.TryGetValue(kvp.Key, out DriftCurrentProperty? currentProperty)) {
                continue;
            }

            CompareProperty(baseline, current, kvp.Value, currentProperty, severity, facts);
        }
    }

    private static void CompareContractMetadata(
        DriftBaselineContract baseline,
        DriftCurrentContract current,
        DiagnosticSeverity severity,
        ImmutableArray<DriftDiagnosticFact>.Builder facts) {
        AddIfChanged("Display.Name", baseline.DisplayName, current.DisplayName);
        AddIfChanged("Display.GroupName", baseline.DisplayGroupName, current.DisplayGroupName);
        AddIfChanged("ProjectionRole", baseline.Role, current.Role);
        AddIfChanged("Icon", baseline.Icon, current.Icon);
        AddIfChanged("RequiresPolicy", baseline.RequiresPolicy, current.RequiresPolicy);
        // Story 9-1 P6 (AC7): empty-state CTA drift is contract-level metadata.
        AddIfChanged("ProjectionEmptyStateCta", baseline.EmptyStateCtaCommandTypeName, current.EmptyStateCtaCommandTypeName);

        // Story 9-1 P9: symmetric Destructive comparison. Previously fired only when
        // baseline.Destructive was non-null, so adding a [Destructive] flag against a null
        // baseline (a fresh declaration becoming destructive) was silently ignored.
        if (baseline.Destructive != current.Destructive) {
            facts.Add(DriftDiagnosticFact.Metadata(
                "Destructive",
                baseline,
                current,
                null,
                "What: metadata drift changed Destructive on " + DriftSanitizer.Safe(current.Type)
                    + ". Expected: " + (baseline.Destructive?.ToString(CultureInfo.InvariantCulture) ?? "<none>")
                    + ". Got: " + (current.Destructive?.ToString(CultureInfo.InvariantCulture) ?? "<none>")
                    + ". Fix: update source metadata or the checked-in generated UI baseline. DocsLink: " + Docs(DriftConstants.MetadataDriftId),
                severity));
        }

        void AddIfChanged(string kind, string? expected, string? got) {
            // Story 9-1 P9: drop the `expected is null` short-circuit. A null→value transition
            // (new metadata added in source against a null baseline) IS drift; AC7 must alert
            // on metadata addition, not just removal/change.
            if (string.Equals(expected, got, StringComparison.Ordinal)) {
                return;
            }

            facts.Add(DriftDiagnosticFact.Metadata(
                kind,
                baseline,
                current,
                null,
                "What: metadata drift changed " + kind + " on " + DriftSanitizer.Safe(current.Type)
                    + ". Expected: " + DriftSanitizer.Safe(expected ?? "<none>")
                    + ". Got: " + DriftSanitizer.Safe(got ?? "<none>")
                    + ". Fix: update source metadata or the checked-in generated UI baseline. DocsLink: " + Docs(DriftConstants.MetadataDriftId),
                severity));
        }
    }

    private static void CompareProperty(
        DriftBaselineContract baseline,
        DriftCurrentContract current,
        DriftBaselineProperty expected,
        DriftCurrentProperty got,
        DiagnosticSeverity severity,
        ImmutableArray<DriftDiagnosticFact>.Builder facts) {
        if (!string.Equals(expected.Category, got.Category, StringComparison.Ordinal)) {
            facts.Add(DriftDiagnosticFact.Structural(
                "TypeCategoryChanged",
                baseline,
                current,
                got.Name,
                "What: structural drift changed property category for '" + DriftSanitizer.Safe(got.Name) + "'. Expected: " + DriftSanitizer.Safe(expected.Category) + ". Got: " + DriftSanitizer.Safe(got.Category) + ". Fix: update source or reconcile the checked-in generated UI baseline. Affected surface: " + SurfaceFor(current.Family, got.Category) + ", form input, DataGrid column, filter, badge/format, MCP descriptor metadata, currency formatting. DocsLink: " + Docs(DriftConstants.StructuralDriftId),
                severity));
        }

        if (expected.Nullable != got.Nullable) {
            string hint = expected.Nullable && !got.Nullable ? "required/breaking/tightened" : "nullable";
            facts.Add(DriftDiagnosticFact.Structural(
                "NullabilityChanged",
                baseline,
                current,
                got.Name,
                "What: structural drift changed nullability for '" + DriftSanitizer.Safe(got.Name) + "'. Expected: nullable=" + expected.Nullable.ToString(CultureInfo.InvariantCulture) + ". Got: nullable=" + got.Nullable.ToString(CultureInfo.InvariantCulture) + " (" + hint + "). Fix: update source or reconcile the checked-in generated UI baseline. Affected surface: form input, DataGrid column, filter behavior, MCP descriptor metadata. DocsLink: " + Docs(DriftConstants.StructuralDriftId),
                severity));
        }

        AddMetadataIfChanged("Display.Name", expected.DisplayName, got.DisplayName);
        AddMetadataIfChanged("Description", expected.Description, got.Description);
        AddMetadataIfChanged("ColumnPriority", expected.ColumnPriority?.ToString(CultureInfo.InvariantCulture), got.ColumnPriority?.ToString(CultureInfo.InvariantCulture));

        // Story 9-1 P20: previously emitted both ProjectionFieldGroup AND Display.GroupName for
        // the same source change, doubling the diagnostic count toward the 50-cap. Single
        // diagnostic now carries both surface labels in the message text.
        if (!string.Equals(expected.FieldGroup, got.FieldGroup, StringComparison.Ordinal)) {
            facts.Add(DriftDiagnosticFact.Metadata(
                "ProjectionFieldGroup",
                baseline,
                current,
                got.Name,
                "What: metadata drift changed ProjectionFieldGroup for '" + DriftSanitizer.Safe(got.Name)
                    + "' on " + DriftSanitizer.Safe(current.Type)
                    + ". Expected: " + DriftSanitizer.Safe(expected.FieldGroup ?? "<none>")
                    + ". Got: " + DriftSanitizer.Safe(got.FieldGroup ?? "<none>")
                    + ". Fix: update source metadata or reconcile the checked-in generated UI baseline. Affected surface: ProjectionFieldGroup, Display.GroupName, DataGrid grouping, detail field, MCP descriptor metadata. DocsLink: " + Docs(DriftConstants.MetadataDriftId),
                severity));
        }

        AddMetadataIfChanged("DisplayFormat", expected.DisplayFormat, got.DisplayFormat);

        // Story 9-1 P6 (AC7): drift coverage for relative-time window and badge mappings.
        AddMetadataIfChanged(
            "RelativeTime",
            expected.RelativeTimeWindowDays?.ToString(CultureInfo.InvariantCulture),
            got.RelativeTimeWindowDays?.ToString(CultureInfo.InvariantCulture));
        AddMetadataIfChanged("ProjectionBadge", expected.BadgeSignature, got.BadgeSignature);

        // Story 9-1 P9: symmetric Derivable comparison.
        if (expected.Derivable != got.Derivable) {
            AddMetadata(
                "Derivable",
                expected.Derivable?.ToString(CultureInfo.InvariantCulture) ?? "<none>",
                got.Derivable?.ToString(CultureInfo.InvariantCulture) ?? "<none>");
        }

        void AddMetadataIfChanged(string kind, string? expectedValue, string? gotValue) {
            // Story 9-1 P9: drop the `expectedValue is null` short-circuit.
            if (string.Equals(expectedValue, gotValue, StringComparison.Ordinal)) {
                return;
            }

            AddMetadata(kind, expectedValue, gotValue);
        }

        void AddMetadata(string kind, string? expectedValue, string? gotValue) {
            facts.Add(DriftDiagnosticFact.Metadata(
                kind,
                baseline,
                current,
                got.Name,
                "What: metadata drift changed " + kind + " for '" + DriftSanitizer.Safe(got.Name) + "' on " + DriftSanitizer.Safe(current.Type) + ". Expected: " + DriftSanitizer.Safe(expectedValue ?? "<none>") + ". Got: " + DriftSanitizer.Safe(gotValue ?? "<none>") + ". Fix: update source metadata or reconcile the checked-in generated UI baseline. Affected surface: renderer-impacting metadata, DataGrid, detail, MCP descriptor metadata. DocsLink: " + Docs(DriftConstants.MetadataDriftId),
                severity));
        }
    }

    private static string SurfaceFor(string family, string category) {
        if (family == "command") {
            return "generated form input and command registration";
        }

        if (category == "Collection" || category == "Unsupported") {
            return "unsupported placeholder, detail field, and MCP projection field metadata";
        }

        if (category == "Enum") {
            return "DataGrid column, filter behavior, badge/format behavior, detail field, and MCP projection field metadata";
        }

        if (category.Contains("Date", StringComparison.Ordinal) || category.Contains("Time", StringComparison.Ordinal)) {
            return "DataGrid column, detail field, filter behavior, format behavior, and MCP projection field metadata";
        }

        return "DataGrid column, detail field, filter behavior, and MCP projection field metadata";
    }

    private static string Docs(string id) => "https://hexalith.github.io/FrontComposer/diagnostics/" + id;
}

internal sealed class DriftDiagnosticFact(
    string id,
    DiagnosticSeverity severity,
    string message,
    string boundedContext,
    string declarationName,
    string memberName,
    string driftKind,
    string baselinePath,
    string declarationPath,
    string expectedShapeHash,
    string actualShapeHash,
    string schemaVersion,
    string algorithmVersion,
    string sortKey,
    string sourcePath,
    int sourceLine,
    int sourceColumn) {
    internal string Id { get; } = id;
    internal DiagnosticSeverity Severity { get; } = severity;
    internal string Message { get; } = message;
    internal string BoundedContext { get; } = boundedContext;
    internal string DeclarationName { get; } = declarationName;
    internal string MemberName { get; } = memberName;
    internal string DriftKind { get; } = driftKind;
    internal string BaselinePath { get; } = baselinePath;
    internal string DeclarationPath { get; } = declarationPath;
    internal string ExpectedShapeHash { get; } = expectedShapeHash;
    internal string ActualShapeHash { get; } = actualShapeHash;
    internal string SchemaVersion { get; } = schemaVersion;
    internal string AlgorithmVersion { get; } = algorithmVersion;
    internal string SortKey { get; } = sortKey;
    internal string SourcePath { get; } = sourcePath;
    internal int SourceLine { get; } = sourceLine;
    internal int SourceColumn { get; } = sourceColumn;

    public override string ToString()
        => Id + "|" + BoundedContext + "|" + DeclarationName + "|" + MemberName + "|" + DriftKind + "|" + Message;

    internal Diagnostic ToDiagnostic(Compilation? compilation = null) {
        DiagnosticDescriptor descriptor = DriftDiagnosticDescriptors.GetDescriptor(Id, Severity);
        Location location = ToLocation(compilation);
        ImmutableDictionary<string, string?> properties = ImmutableDictionary<string, string?>.Empty
            .Add("BaselinePath", BaselinePath)
            .Add("DeclarationPath", DeclarationPath)
            .Add("DeclarationName", DeclarationName)
            .Add("MemberName", MemberName)
            .Add("DriftKind", DriftKind)
            .Add("ExpectedShapeHash", ExpectedShapeHash)
            .Add("ActualShapeHash", ActualShapeHash)
            .Add("SchemaVersion", SchemaVersion)
            .Add("AlgorithmVersion", AlgorithmVersion)
            .Add("BoundedContext", BoundedContext);

        return Diagnostic.Create(descriptor, location, properties, Message);
    }

    internal static DriftDiagnosticFact MissingBaseline()
        => Simple(
            DriftConstants.MissingBaselineId,
            DiagnosticSeverity.Warning,
            "What: drift detection is enabled but no trusted generated UI baseline was provided. Expected: a checked-in frontcomposer.drift-baseline.json AdditionalText. Got: first run or missing baseline. Fix: create or manually reconcile the baseline in Story 9-1; Story 9-2 owns the future CLI inspect/update workflow. DocsLink: " + Docs(DriftConstants.MissingBaselineId),
            "MissingBaseline");

    internal static DriftDiagnosticFact InvalidBaselinePath(string path)
        => Simple(
            DriftConstants.InvalidBaselinePathId,
            DiagnosticSeverity.Error,
            "What: configured drift baseline path did not resolve. Expected: an AdditionalText matching the configured baseline path. Got: " + DriftSanitizer.Safe(path) + ". Fix: include the checked-in baseline file or correct HfcDriftBaselinePath. DocsLink: " + Docs(DriftConstants.InvalidBaselinePathId),
            "InvalidBaselinePath");

    internal static DriftDiagnosticFact Configuration(string optionName, string expected, string got)
        => Simple(
            DriftConstants.InvalidOptionId,
            DiagnosticSeverity.Warning,
            "What: invalid drift detector analyzer-config option '" + DriftSanitizer.Safe(optionName) + "'. Expected: " + DriftSanitizer.Safe(expected) + ". Got: " + DriftSanitizer.Safe(got) + ". Fix: correct the MSBuild property; the generator falls back to documented safe defaults. DocsLink: " + Docs(DriftConstants.InvalidOptionId),
            "InvalidOption");

    internal static DriftDiagnosticFact TrustFailure(
        string id,
        string what,
        string expected,
        string got,
        string baselinePath,
        string schemaVersion = DriftConstants.SchemaVersion,
        string algorithmVersion = DriftConstants.Algorithm)
        => Simple(
            id,
            DiagnosticSeverity.Error,
            "What: " + DriftSanitizer.Safe(what) + ". Expected: " + DriftSanitizer.Safe(expected) + ". Got: " + DriftSanitizer.Safe(got) + ". Fix: repair or regenerate the checked-in generated UI baseline; unsafe partial drift comparison is suppressed. DocsLink: " + Docs(id),
            what,
            baselinePath: DriftSanitizer.NormalizePath(baselinePath),
            schemaVersion: DriftSanitizer.Safe(schemaVersion),
            algorithmVersion: DriftSanitizer.Safe(algorithmVersion));

    internal static DriftDiagnosticFact RedactionSuppressed()
        => Simple(
            DriftConstants.RedactionSuppressedId,
            DiagnosticSeverity.Error,
            "What: drift baseline contains structural values that could not be safely sanitized. Expected: runtime-data-free baseline metadata. Got: redaction-sensitive value. Fix: remove tenant/user/token/path/payload data from the checked-in baseline; original drift diagnostics are suppressed. DocsLink: " + Docs(DriftConstants.RedactionSuppressedId),
            "RedactionSuppressed");

    internal static DriftDiagnosticFact Structural(
        string driftKind,
        DriftBaselineContract? baseline,
        DriftCurrentContract? current,
        string? memberName,
        string message,
        DiagnosticSeverity severity) {
        string boundedContext = current?.BoundedContext ?? baseline?.BoundedContext ?? "<none>";
        string declarationName = current?.Type ?? baseline?.Type ?? "<none>";
        string baselinePath = baseline?.SourcePath ?? "<none>";
        string declarationPath = current?.SourcePath ?? "<none>";
        // Story 9-1 P19: include schema+algorithm in hash material AND emit explicit
        // <none> sentinels for null sides so an "added declaration" and a "removed
        // declaration" hashing the same memberName cannot collide.
        string expectedHash = Hash(ComposeHashInput(
            baseline?.Type ?? "<none>",
            baseline?.BoundedContext ?? "<none>",
            memberName ?? "<none>",
            driftKind,
            discriminator: "expected"));
        string actualHash = Hash(ComposeHashInput(
            current?.Type ?? "<none>",
            current?.BoundedContext ?? "<none>",
            memberName ?? "<none>",
            driftKind,
            discriminator: "actual"));
        string sortKey = boundedContext + "|" + (current?.Family ?? baseline?.Family ?? "<none>") + "|" + declarationName + "|" + (memberName ?? "<none>") + "|" + driftKind;
        return new DriftDiagnosticFact(
            DriftConstants.StructuralDriftId,
            severity,
            DriftSanitizer.SafeMessage(message),
            DriftSanitizer.Safe(boundedContext),
            DriftSanitizer.Safe(declarationName),
            DriftSanitizer.Safe(memberName ?? "<none>"),
            driftKind,
            DriftSanitizer.NormalizePath(baselinePath),
            DriftSanitizer.NormalizePath(declarationPath),
            expectedHash,
            actualHash,
            DriftConstants.SchemaVersion,
            DriftConstants.Algorithm,
            sortKey,
            current?.SourcePath ?? string.Empty,
            current?.SourceLine ?? -1,
            current?.SourceColumn ?? -1);
    }

    internal static DriftDiagnosticFact Metadata(
        string driftKind,
        DriftBaselineContract baseline,
        DriftCurrentContract current,
        string? memberName,
        string message,
        DiagnosticSeverity severity) {
        string sortKey = current.BoundedContext + "|" + current.Family + "|" + current.Type + "|" + (memberName ?? "<none>") + "|" + driftKind;
        return new DriftDiagnosticFact(
            DriftConstants.MetadataDriftId,
            severity,
            DriftSanitizer.SafeMessage(message),
            DriftSanitizer.Safe(current.BoundedContext),
            DriftSanitizer.Safe(current.Type),
            DriftSanitizer.Safe(memberName ?? "<none>"),
            driftKind,
            DriftSanitizer.NormalizePath(baseline.SourcePath),
            DriftSanitizer.NormalizePath(current.SourcePath),
            // Story 9-1 P19: schema+algorithm in hash material; explicit <none> for null members.
            Hash(ComposeHashInput(baseline.Type, baseline.BoundedContext, memberName ?? "<none>", driftKind, "expected")),
            Hash(ComposeHashInput(current.Type, current.BoundedContext, memberName ?? "<none>", driftKind, "actual")),
            DriftConstants.SchemaVersion,
            DriftConstants.Algorithm,
            sortKey,
            current.SourcePath,
            current.SourceLine,
            current.SourceColumn);
    }

    internal static DriftDiagnosticFact Truncation(int omittedCount)
        => Simple(
            DriftConstants.TruncationId,
            DiagnosticSeverity.Warning,
            "What: drift diagnostics were truncated after the configured cap. Expected: full drift output. Got: " + omittedCount.ToString(CultureInfo.InvariantCulture) + " omitted diagnostics. Fix: address earlier diagnostics or increase HfcDriftMaxDiagnostics. DocsLink: " + Docs(DriftConstants.TruncationId),
            "Truncation",
            memberName: "<summary>",
            sortKey: "~~~~|Truncation");

    internal static DriftDiagnosticFact TrimAot()
        => Simple(
            DriftConstants.TrimAotReflectionCatalogId,
            DiagnosticSeverity.Warning,
            "What: PublishTrimmed/native AOT build uses projection metadata where the default ReflectionActionQueueProjectionCatalog path may be trim-incompatible. Expected: source-generated or adopter-supplied IActionQueueProjectionCatalog evidence. Got: default reflection catalog risk. Fix: register a source-generated IActionQueueProjectionCatalog override; runtime validators remain authoritative where build-time evidence is incomplete. DocsLink: " + Docs(DriftConstants.TrimAotReflectionCatalogId),
            "TrimAotReflectionCatalog");

    private static DriftDiagnosticFact Simple(
        string id,
        DiagnosticSeverity severity,
        string message,
        string driftKind,
        string boundedContext = "<none>",
        string declarationName = "<none>",
        string memberName = "<none>",
        string baselinePath = "<none>",
        string declarationPath = "<none>",
        string schemaVersion = DriftConstants.SchemaVersion,
        string algorithmVersion = DriftConstants.Algorithm,
        string sortKey = "") {
        string actualSortKey = string.IsNullOrEmpty(sortKey)
            ? boundedContext + "|<diagnostic>|" + declarationName + "|" + memberName + "|" + driftKind
            : sortKey;
        return new DriftDiagnosticFact(
            id,
            severity,
            DriftSanitizer.SafeMessage(message),
            DriftSanitizer.Safe(boundedContext),
            DriftSanitizer.Safe(declarationName),
            DriftSanitizer.Safe(memberName),
            driftKind,
            DriftSanitizer.NormalizePath(baselinePath),
            DriftSanitizer.NormalizePath(declarationPath),
            // Story 9-1 P19: schema+algorithm in hash material to prevent cross-version collisions.
            Hash(ComposeHashInput(declarationName, boundedContext, memberName, driftKind, id + "|expected")),
            Hash(ComposeHashInput(declarationName, boundedContext, memberName, driftKind, id + "|actual")),
            DriftSanitizer.Safe(schemaVersion),
            DriftSanitizer.Safe(algorithmVersion),
            actualSortKey,
            string.Empty,
            -1,
            -1);
    }

    private Location ToLocation(Compilation? compilation) {
        if (string.IsNullOrWhiteSpace(SourcePath) || SourceLine < 0 || SourceColumn < 0) {
            return Location.None;
        }

        if (compilation is not null) {
            // Story 9-1 P21: Windows file paths compare case-insensitively; using ordinal
            // here used to leak through to the LinePosition fallback when adopters' build
            // produced a mixed-case `tree.FilePath`, which then leaked an absolute path into
            // IDE diagnostics.
            StringComparison pathComparison = System.IO.Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            foreach (SyntaxTree tree in compilation.SyntaxTrees) {
                if (!string.Equals(tree.FilePath, SourcePath, pathComparison)) {
                    continue;
                }

                SourceText text = tree.GetText();
                if (SourceLine < text.Lines.Count) {
                    TextLine line = text.Lines[SourceLine];
                    int absolutePosition = Math.Min(line.End, line.Start + SourceColumn);
                    return Location.Create(tree, new TextSpan(absolutePosition, 0));
                }
            }
        }

        // Story 9-1 P21: sanitize the SourcePath before embedding it in Location.Create —
        // otherwise the absolute path leaks into IDE diagnostics, bypassing the message-level
        // sanitization. NormalizePath reduces absolute paths to filename / `<outside-project>`.
        string sanitizedPath = DriftSanitizer.NormalizePath(SourcePath);
        LinePosition position = new(SourceLine, SourceColumn);
        return Location.Create(sanitizedPath, new TextSpan(0, 0), new LinePositionSpan(position, position));
    }

    /// <summary>
    /// Story 9-1 P18 / P19: previously created and disposed a <see cref="SHA256"/> instance
    /// per call (~500 hashes per drift pass × generator runs on every keystroke). Cache one
    /// instance per thread via <see cref="ThreadLocal{T}"/> — SHA256 instances are not
    /// thread-safe but generator pipelines do dispatch across the thread pool, so a
    /// thread-local pool gives allocation-free reuse without explicit locking.
    /// </summary>
    private static readonly ThreadLocal<SHA256> Sha256Pool = new(static () => SHA256.Create());

    private static string Hash(string? input) {
        SHA256 sha = Sha256Pool.Value!;
        byte[] bytes = Encoding.UTF8.GetBytes(input ?? "<none>");
        byte[] hash = sha.ComputeHash(bytes);
        StringBuilder sb = new(hash.Length * 2);
        foreach (byte b in hash) {
            sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    private static string ComposeHashInput(string declarationName, string boundedContext, string memberName, string driftKind, string discriminator)
        => DriftConstants.SchemaVersion + "|"
            + DriftConstants.Algorithm + "|"
            + (string.IsNullOrEmpty(declarationName) ? "<none>" : declarationName) + "|"
            + (string.IsNullOrEmpty(boundedContext) ? "<none>" : boundedContext) + "|"
            + (string.IsNullOrEmpty(memberName) ? "<none>" : memberName) + "|"
            + driftKind + "|"
            + discriminator;

    private static string Docs(string id) => "https://hexalith.github.io/FrontComposer/diagnostics/" + id;
}

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

    private static bool ContainsRedactionTrigger(string text) {
        return text.IndexOf("SENTINEL_", StringComparison.Ordinal) >= 0
            || text.IndexOf("Bearer ", StringComparison.OrdinalIgnoreCase) >= 0
            || text.IndexOf("Authorization:", StringComparison.OrdinalIgnoreCase) >= 0
            || text.IndexOf("eyJ", StringComparison.Ordinal) >= 0
            || text.IndexOf("{\"", StringComparison.Ordinal) >= 0
            || text.IndexOf("///auto/", StringComparison.Ordinal) >= 0
            || ContainsAbsolutePath(text);
    }

    private static bool ContainsAbsolutePath(string value) {
        // Windows drive root with forward OR backslash: a single ASCII letter, then ':',
        // then '\\' or '/'. The letter MUST sit at the start of `value` or after a non-
        // letter character so we don't misclassify URL schemes like "https://" — there
        // the 's' in "s:/" is preceded by a letter and is therefore part of the scheme,
        // not a drive root.
        for (int i = 0; i + 2 < value.Length; i++) {
            char c0 = value[i];
            if (!((c0 >= 'A' && c0 <= 'Z') || (c0 >= 'a' && c0 <= 'z'))) {
                continue;
            }

            if (value[i + 1] != ':') {
                continue;
            }

            char sep = value[i + 2];
            if (sep != '\\' && sep != '/') {
                continue;
            }

            char preceding = i == 0 ? ' ' : value[i - 1];
            if (!char.IsLetter(preceding)) {
                return true;
            }
        }

        return value.IndexOf("C__", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool LooksLikeWindowsDriveRoot(string normalized) {
        return normalized.Length >= 3
            && ((normalized[0] >= 'A' && normalized[0] <= 'Z') || (normalized[0] >= 'a' && normalized[0] <= 'z'))
            && normalized[1] == ':'
            && normalized[2] == '/';
    }
}

internal static class DriftDiagnosticDescriptors {
    internal static DiagnosticDescriptor GetDescriptor(string id, DiagnosticSeverity severity)
        => id switch {
            DriftConstants.MissingBaselineId => DiagnosticDescriptors.GeneratedUiBaselineMissing,
            DriftConstants.InvalidBaselinePathId => DiagnosticDescriptors.GeneratedUiBaselinePathInvalid,
            DriftConstants.InvalidBaselineContentId => DiagnosticDescriptors.GeneratedUiBaselineContentInvalid,
            DriftConstants.UnsupportedSchemaId => DiagnosticDescriptors.GeneratedUiBaselineSchemaUnsupported,
            DriftConstants.UnsupportedAlgorithmId => DiagnosticDescriptors.GeneratedUiBaselineAlgorithmUnsupported,
            DriftConstants.BaselineBoundsExceededId => DiagnosticDescriptors.GeneratedUiBaselineBoundsExceeded,
            DriftConstants.DuplicateOrInvariantId => DiagnosticDescriptors.GeneratedUiBaselineIdentityInvalid,
            DriftConstants.StructuralDriftId => WithSeverity(DiagnosticDescriptors.GeneratedUiStructuralDrift, severity),
            DriftConstants.MetadataDriftId => WithSeverity(DiagnosticDescriptors.GeneratedUiMetadataDrift, severity),
            DriftConstants.InvalidOptionId => DiagnosticDescriptors.GeneratedUiDriftOptionInvalid,
            DriftConstants.TruncationId => DiagnosticDescriptors.GeneratedUiDriftTruncated,
            DriftConstants.RedactionSuppressedId => DiagnosticDescriptors.GeneratedUiDriftRedactionSuppressed,
            DriftConstants.TrimAotReflectionCatalogId => DiagnosticDescriptors.TrimAotReflectionCatalogWarning,
            _ => DiagnosticDescriptors.GeneratedUiStructuralDrift,
        };

    private static DiagnosticDescriptor WithSeverity(DiagnosticDescriptor descriptor, DiagnosticSeverity severity)
        => severity == descriptor.DefaultSeverity
            ? descriptor
            : new DiagnosticDescriptor(
                descriptor.Id,
                descriptor.Title,
                descriptor.MessageFormat,
                descriptor.Category,
                severity,
                descriptor.IsEnabledByDefault,
                descriptor.Description,
                descriptor.HelpLinkUri,
                descriptor.CustomTags.ToArray());
}
