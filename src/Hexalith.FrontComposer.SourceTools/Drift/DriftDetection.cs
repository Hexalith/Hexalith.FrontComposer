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

        if (int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed)
            && parsed >= minInclusive
            && parsed <= maxInclusive) {
            return parsed;
        }

        diagnostics.Add(DriftDiagnosticFact.Configuration(
            key.Split('.').Last(),
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
        if (options.TryGetValue(key, out string? value)) {
            return value?.Trim() ?? string.Empty;
        }

        return null;
    }
}

internal sealed class DriftBaselineInput(string path, string text) : IEquatable<DriftBaselineInput> {
    internal string Path { get; } = path;
    internal string Text { get; } = text;

    internal static bool IsCandidate(string path) {
        string extension = System.IO.Path.GetExtension(path);
        return string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase);
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
    string? displayFormat) {
    internal string Name { get; } = name;
    internal string Category { get; } = category;
    internal bool Nullable { get; } = nullable;
    internal bool? Derivable { get; } = derivable;
    internal string? DisplayName { get; } = displayName;
    internal string? Description { get; } = description;
    internal int? ColumnPriority { get; } = columnPriority;
    internal string? FieldGroup { get; } = fieldGroup;
    internal string? DisplayFormat { get; } = displayFormat;
}

internal static class DriftBaselineLoader {
    internal static DriftBaselineLoadResult Load(ImmutableArray<DriftBaselineInput> inputs, DriftOptions options) {
        ImmutableArray<DriftDiagnosticFact>.Builder diagnostics = ImmutableArray.CreateBuilder<DriftDiagnosticFact>();
        ImmutableArray<DriftBaselineContract>.Builder contracts = ImmutableArray.CreateBuilder<DriftBaselineContract>();

        ImmutableArray<DriftBaselineInput> sorted = inputs
            .OrderBy(static i => i.Path, StringComparer.Ordinal)
            .ToImmutableArray();

        if (options.ConfiguredBaselinePath is not null
            && !sorted.Any(i => PathsEqual(i.Path, options.ConfiguredBaselinePath))) {
            diagnostics.Add(DriftDiagnosticFact.InvalidBaselinePath(options.ConfiguredBaselinePath));
            return new DriftBaselineLoadResult(false, new DriftBaselineSet(contracts.ToImmutable()), diagnostics.ToImmutable());
        }

        if (sorted.Length == 0) {
            if (options.Enabled) {
                diagnostics.Add(DriftDiagnosticFact.MissingBaseline());
            }

            return new DriftBaselineLoadResult(false, new DriftBaselineSet(contracts.ToImmutable()), diagnostics.ToImmutable());
        }

        HashSet<string> contractIdentities = new(StringComparer.Ordinal);
        HashSet<string> unsafeValues = new(StringComparer.Ordinal);
        bool trustFailed = false;

        foreach (DriftBaselineInput input in sorted) {
            if (input.Text.Length > options.MaxBaselineBytes
                || input.Text.IndexOf("_oversizedHint", StringComparison.Ordinal) >= 0) {
                diagnostics.Add(DriftDiagnosticFact.TrustFailure(
                    DriftConstants.BaselineBoundsExceededId,
                    "oversized baseline",
                    "at most " + options.MaxBaselineBytes.ToString(CultureInfo.InvariantCulture) + " bytes",
                    input.Text.Length.ToString(CultureInfo.InvariantCulture),
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

            JsonDocument document;
            try {
                document = JsonDocument.Parse(input.Text);
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
                string schemaVersion = ReadString(root, "schemaVersion") ?? string.Empty;
                string algorithm = ReadString(root, "algorithm") ?? string.Empty;

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

        return new DriftBaselineLoadResult(!trustFailed, new DriftBaselineSet(contracts.ToImmutable()), diagnostics.ToImmutable());
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
        HashSet<string> propertyNames = new(StringComparer.Ordinal);
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

                properties.Add(new DriftBaselineProperty(
                    name,
                    category,
                    nullable,
                    ReadBool(propertyElement, "derivable"),
                    ReadString(propertyElement, "displayName"),
                    ReadString(propertyElement, "description"),
                    TryReadInt(propertyElement, "columnPriority"),
                    ReadString(propertyElement, "fieldGroup"),
                    ReadString(propertyElement, "displayFormat")));
            }
        }

        string? displayName = ReadString(contractElement, "displayName");
        string? displayGroupName = ReadString(contractElement, "displayGroupName");
        string? role = ReadString(contractElement, "role");
        string? icon = ReadString(contractElement, "icon");
        string? requiresPolicy = ReadString(contractElement, "requiresPolicy");

        TrackUnsafe(displayName, unsafeValues);
        TrackUnsafe(displayGroupName, unsafeValues);
        TrackUnsafe(role, unsafeValues);
        TrackUnsafe(icon, unsafeValues);
        TrackUnsafe(requiresPolicy, unsafeValues);

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
            properties.ToImmutable());
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

    private static bool PathsEqual(string left, string right)
        => string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase)
            || NormalizePath(left).EndsWith(NormalizePath(right), StringComparison.OrdinalIgnoreCase);

    private static string NormalizePath(string path)
        => path.Replace('\\', '/').TrimStart('/');

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
    string? displayFormat) {
    internal string Name { get; } = name;
    internal string Category { get; } = category;
    internal bool Nullable { get; } = nullable;
    internal bool? Derivable { get; } = derivable;
    internal string? DisplayName { get; } = displayName;
    internal string? Description { get; } = description;
    internal int? ColumnPriority { get; } = columnPriority;
    internal string? FieldGroup { get; } = fieldGroup;
    internal string? DisplayFormat { get; } = displayFormat;
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
                    property.DisplayFormat.ToString()));
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
                    property.DisplayFormat.ToString()));
            }

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
                properties.ToImmutable(),
                string.Empty,
                -1,
                -1));
        }

        return new DriftCurrentSnapshot(contracts.ToImmutable());
    }

    private static string QualifiedType(string @namespace, string typeName)
        => string.IsNullOrEmpty(@namespace) ? typeName : @namespace + "." + typeName;
}

internal sealed class DriftComparisonResult(ImmutableArray<DriftDiagnosticFact> diagnostics) {
    public ImmutableArray<DriftDiagnosticFact> Diagnostics { get; } = diagnostics;
}

internal sealed class DriftComparisonService {
    public static DriftComparisonResult Compare(DriftCurrentSnapshot current, DriftBaselineSet baseline)
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
        if (baseline.Destructive != current.Destructive && baseline.Destructive is not null) {
            facts.Add(DriftDiagnosticFact.Metadata(
                "Destructive",
                baseline,
                current,
                null,
                "What: metadata drift changed Destructive on " + DriftSanitizer.Safe(current.Type) + ". Expected: " + baseline.Destructive + ". Got: " + current.Destructive + ". Fix: update source metadata or the checked-in generated UI baseline. DocsLink: " + Docs(DriftConstants.MetadataDriftId),
                severity));
        }

        void AddIfChanged(string kind, string? expected, string? got) {
            if (expected is null || string.Equals(expected, got, StringComparison.Ordinal)) {
                return;
            }

            facts.Add(DriftDiagnosticFact.Metadata(
                kind,
                baseline,
                current,
                null,
                "What: metadata drift changed " + kind + " on " + DriftSanitizer.Safe(current.Type) + ". Expected: " + DriftSanitizer.Safe(expected) + ". Got: " + DriftSanitizer.Safe(got ?? "<none>") + ". Fix: update source metadata or the checked-in generated UI baseline. DocsLink: " + Docs(DriftConstants.MetadataDriftId),
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
        if (!string.Equals(expected.FieldGroup, got.FieldGroup, StringComparison.Ordinal)) {
            AddMetadata("ProjectionFieldGroup", expected.FieldGroup, got.FieldGroup);
            AddMetadata("Display.GroupName", expected.FieldGroup, got.FieldGroup);
        }

        AddMetadataIfChanged("DisplayFormat", expected.DisplayFormat, got.DisplayFormat);
        if (expected.Derivable is not null && expected.Derivable != got.Derivable) {
            AddMetadata("Derivable", expected.Derivable.ToString(), got.Derivable.ToString());
        }

        void AddMetadataIfChanged(string kind, string? expectedValue, string? gotValue) {
            if (expectedValue is null || string.Equals(expectedValue, gotValue, StringComparison.Ordinal)) {
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
        string expectedHash = Hash(baseline?.Type + "|" + baseline?.BoundedContext + "|" + memberName + "|" + driftKind);
        string actualHash = Hash(current?.Type + "|" + current?.BoundedContext + "|" + memberName + "|" + driftKind);
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
            Hash(baseline.Type + "|" + memberName + "|" + driftKind),
            Hash(current.Type + "|" + memberName + "|" + driftKind),
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
            Hash(id + "|expected|" + driftKind),
            Hash(id + "|actual|" + driftKind),
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
            foreach (SyntaxTree tree in compilation.SyntaxTrees) {
                if (!string.Equals(tree.FilePath, SourcePath, StringComparison.Ordinal)) {
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

        LinePosition position = new(SourceLine, SourceColumn);
        return Location.Create(SourcePath, new TextSpan(0, 0), new LinePositionSpan(position, position));
    }

    private static string Hash(string? input) {
        byte[] bytes = Encoding.UTF8.GetBytes(input ?? string.Empty);
        byte[] hash;
        using (SHA256 sha = SHA256.Create()) {
            hash = sha.ComputeHash(bytes);
        }

        StringBuilder sb = new(hash.Length * 2);
        foreach (byte b in hash) {
            sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    private static string Docs(string id) => "https://hexalith.github.io/FrontComposer/diagnostics/" + id;
}

internal static class DriftSanitizer {
    internal static bool IsUnsafe(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return false;
        }

        return value.Contains("SENTINEL_", StringComparison.Ordinal)
            || value.Contains("Bearer ", StringComparison.OrdinalIgnoreCase)
            || value.Contains("eyJ", StringComparison.Ordinal)
            || value.Contains("{\"", StringComparison.Ordinal)
            || value.Contains("C:\\", StringComparison.OrdinalIgnoreCase)
            || value.Contains("C__", StringComparison.OrdinalIgnoreCase)
            || value.Contains("token", StringComparison.OrdinalIgnoreCase)
            || value.Contains("tenant_", StringComparison.OrdinalIgnoreCase)
            || value.Contains("user_", StringComparison.OrdinalIgnoreCase)
            || value.Contains("etag", StringComparison.OrdinalIgnoreCase);
    }

    internal static string SafeMessage(string message) {
        string safe = message;
        foreach (string token in new[] { "SENTINEL_", "Bearer ", "eyJ", "C:\\", "C__", "{\"", "///auto/" }) {
            if (safe.Contains(token, StringComparison.Ordinal)) {
                return "What: drift diagnostic content was suppressed by redaction. Expected: sanitized structural metadata. Got: unsafe diagnostic payload. Fix: remove runtime data from baseline/source metadata. DocsLink: https://hexalith.github.io/FrontComposer/diagnostics/" + DriftConstants.RedactionSuppressedId;
            }
        }

        return safe;
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

    internal static string NormalizePath(string path) {
        if (string.IsNullOrWhiteSpace(path) || path == "<none>") {
            return "<none>";
        }

        string normalized = path.Replace('\\', '/');
        if (normalized.IndexOf(':') >= 0) {
            return System.IO.Path.GetFileName(normalized);
        }

        normalized = normalized.TrimStart('/');
        return string.IsNullOrWhiteSpace(normalized) ? "<none>" : normalized;
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
