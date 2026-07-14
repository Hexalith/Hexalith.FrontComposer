using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Hexalith.FrontComposer.SourceTools.Drift;

internal static class DriftBaselineLoader {
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    internal static DriftBaselineLoadResult Load(ImmutableArray<DriftBaselineInput> inputs, DriftOptions options) {
        ImmutableArray<DriftDiagnosticFact>.Builder diagnostics = ImmutableArray.CreateBuilder<DriftDiagnosticFact>();
        ImmutableArray<DriftBaselineContract>.Builder contracts = ImmutableArray.CreateBuilder<DriftBaselineContract>();

        var sorted = inputs
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
                return EmptyResult(diagnostics, options.MaxDiagnostics);
            }
        }

        if (eligible.Length == 0) {
            // Story 9-1 P3 (AC2): the baseline-load path emits HFC1058 unconditionally when
            // drift detection is enabled and no matching baseline exists. Higher up the stack
            // the orchestrator already gates on options.Enabled so this only runs in the
            // opt-in case.
            diagnostics.Add(DriftDiagnosticFact.MissingBaseline());
            return EmptyResult(diagnostics, options.MaxDiagnostics);
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
            return EmptyResult(diagnostics, options.MaxDiagnostics);
        }

        return new DriftBaselineLoadResult(
            true,
            new DriftBaselineSet(contracts.ToImmutable()),
            CapLoadDiagnostics(diagnostics, options.MaxDiagnostics));
    }

    private static DriftBaselineLoadResult EmptyResult(
        ImmutableArray<DriftDiagnosticFact>.Builder diagnostics,
        int maxDiagnostics)
        => new(
            comparisonEnabled: false,
            baseline: new DriftBaselineSet(ImmutableArray<DriftBaselineContract>.Empty),
            diagnostics: CapLoadDiagnostics(diagnostics, maxDiagnostics));

    private static ImmutableArray<DriftDiagnosticFact> CapLoadDiagnostics(
        ImmutableArray<DriftDiagnosticFact>.Builder diagnostics,
        int maxDiagnostics) {
        if (diagnostics.Count <= maxDiagnostics) {
            return diagnostics.ToImmutable();
        }

        ImmutableArray<DriftDiagnosticFact>.Builder capped = ImmutableArray.CreateBuilder<DriftDiagnosticFact>();
        capped.AddRange(diagnostics.Take(maxDiagnostics));
        capped.Add(DriftDiagnosticFact.Truncation(diagnostics.Count - maxDiagnostics));
        return capped.ToImmutable();
    }

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
            _ = unsafeValues.Add(value);
        }
    }
}
