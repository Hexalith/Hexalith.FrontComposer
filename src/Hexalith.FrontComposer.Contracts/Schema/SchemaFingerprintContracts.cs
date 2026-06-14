using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace Hexalith.FrontComposer.Contracts.Schema;

public enum SchemaCollectionOrder {
    NonStructuralSorted,
    StructuralOrder,
}

public enum SchemaContractFamily {
    CommandTool,
    ProjectionResource,
    LifecycleResult,
    MarkdownRendererContract,
    SkillCorpusManifest,
    SkillCorpusResource,
    AggregateMcpManifest,
}

public enum SchemaMaterialValidationCategory {
    None,
    DuplicateJsonKey,
    MalformedJson,
    UnknownRootDiscriminator,
    UnknownContractFamily,
    DuplicateStableId,
    DuplicateFieldName,
    JsonDepthExceeded,
    PayloadTooLarge,
}

/// <summary>
/// Deterministic canonical JSON and SHA-256 fingerprint helper for structural schema material.
/// </summary>
public static class CanonicalSchemaMaterial {

    /// <summary>
    /// Sentinel surrogate-pair-free placeholder used by canonical fingerprint material to
    /// distinguish a logical "value not provided" from an explicit empty string. Chosen to
    /// avoid collision with any plausible user-provided scalar.
    /// </summary>
    public const string AbsentValueSentinel = "<absent>";

    /// <summary>
    /// Upper bound on raw canonical JSON byte length accepted by <see cref="ValidateCanonicalJson"/>.
    /// Prevents an untrusted oversized payload from forcing a full UTF-8 byte allocation.
    /// </summary>
    public const int MaxCanonicalJsonBytes = 4 * 1024 * 1024;

    private const int _maxDepth = 32;

    /// <summary>
    /// Canonical JSON serialization options pinned with a stable JavaScript encoder so default
    /// encoder escape tables changing between .NET runtimes cannot silently drift fingerprints.
    /// The source-gen <see cref="SchemaFingerprintJsonContext"/> is the type-info resolver, so
    /// AOT/trim consumers stay supported via the typed <see cref="_canonicalTypeInfo"/> below.
    /// </summary>
    private static readonly JsonSerializerOptions _canonicalOptions = new() {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        TypeInfoResolver = SchemaFingerprintJsonContext.Default,
    };

    private static readonly System.Text.Json.Serialization.Metadata.JsonTypeInfo<SchemaContractDocument> _canonicalTypeInfo
        = (System.Text.Json.Serialization.Metadata.JsonTypeInfo<SchemaContractDocument>)_canonicalOptions.GetTypeInfo(typeof(SchemaContractDocument));

    public static SchemaCanonicalPayload CreatePayload(SchemaContractDocument document) {
        if (document is null) {
            throw new ArgumentNullException(nameof(document));
        }
        SchemaMaterialValidationResult validation = ValidateDocument(document);
        if (!validation.IsValid) {
            throw new SchemaMaterialValidationException(validation);
        }

        SchemaContractDocument normalized = Normalize(document);
        string json = JsonSerializer.Serialize(normalized, _canonicalTypeInfo);
        // P-43: producer-side parser validation closes the AC25 / D16 emit-side gap.
        SchemaMaterialValidationResult roundTrip = ValidateCanonicalJson(json);
        if (!roundTrip.IsValid) {
            throw new SchemaMaterialValidationException(roundTrip);
        }
        string hash = Sha256Hex(json);
        return new SchemaCanonicalPayload(
            normalized,
            json,
            new SchemaFingerprint(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, hash));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0057:Use range operator", Justification = "Not supported in this context")]
    public static SchemaMaterialValidationResult ValidateCanonicalJson(string json) {
        if (string.IsNullOrWhiteSpace(json)) {
            return new(false, SchemaMaterialValidationCategory.MalformedJson, "schema.json.malformed", "$");
        }

        // P-31: strip a leading UTF-8 BOM before we hand bytes to Utf8JsonReader.
        string normalizedJson = json.Length == 0 || json[0] != '﻿' ? json : json.Substring(1);

        // P-33: bound payload size before allocating the UTF-8 byte array.
        int byteCount = Encoding.UTF8.GetByteCount(normalizedJson);
        if (byteCount > MaxCanonicalJsonBytes) {
            return new(false, SchemaMaterialValidationCategory.PayloadTooLarge, "schema.json.payload-too-large", "$");
        }

        try {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(normalizedJson), new JsonReaderOptions {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = _maxDepth,
            });
            return ValidateJsonObject(ref reader, "$");
        }
        catch (JsonException ex) {
            // P-30: depth-exceeded is a distinct category so consumers get actionable remediation.
            bool depthExceeded = ex.Message?.IndexOf("depth", StringComparison.OrdinalIgnoreCase) >= 0;
            return depthExceeded
                ? new(false, SchemaMaterialValidationCategory.JsonDepthExceeded, "schema.json.depth-exceeded", "$")
                : new(false, SchemaMaterialValidationCategory.MalformedJson, "schema.json.malformed", "$");
        }
    }

    public static SchemaMaterialValidationResult ValidateDocument(SchemaContractDocument document) {
        if (document is null) {
            throw new ArgumentNullException(nameof(document));
        }

        if (!string.Equals(document.RootDiscriminator, "frontcomposer.schema.contract.v1", StringComparison.Ordinal)) {
            return new(false, SchemaMaterialValidationCategory.UnknownRootDiscriminator, "schema.root.unknown", "$.RootDiscriminator");
        }

        if (!Enum.IsDefined(typeof(SchemaContractFamily), document.Family)) {
            return new(false, SchemaMaterialValidationCategory.UnknownContractFamily, "schema.family.unknown", "$.Family");
        }

        foreach (SchemaCollectionContract collection in document.Collections) {
            if (string.IsNullOrWhiteSpace(collection.Name) || string.IsNullOrWhiteSpace(collection.StableIdField)) {
                return new(false, SchemaMaterialValidationCategory.MalformedJson, "schema.collection.invalid", "$.Collections");
            }
        }

        // P-29: field-name duplicates carry their own category distinct from collection-level
        // stable-id collisions so consumers can branch on validation feedback meaningfully.
        HashSet<string> fieldNames = new(StringComparer.Ordinal);
        foreach (SchemaFieldContract field in document.Fields) {
            if (!fieldNames.Add(field.Name)) {
                return new(false, SchemaMaterialValidationCategory.DuplicateFieldName, "schema.field.duplicate-name", "$.Fields." + field.Name);
            }
        }

        return SchemaMaterialValidationResult.Valid;
    }

    private static SchemaContractDocument Normalize(SchemaContractDocument document)
        => document with {
            RootDiscriminator = NormalizeScalar(document.RootDiscriminator),
            ContractId = NormalizeScalar(document.ContractId),
            ContractSchemaVersion = NormalizeScalar(document.ContractSchemaVersion),
            BoundedContext = NormalizeOptional(document.BoundedContext),
            FullyQualifiedName = NormalizeOptional(document.FullyQualifiedName),
            ProtocolIdentifier = NormalizeOptional(document.ProtocolIdentifier),
            Fields = [.. document.Fields
                .OrderBy(f => f.Name, StringComparer.Ordinal)
                .Select(NormalizeField)],
            Collections = [.. document.Collections
                .OrderBy(c => c.Name, StringComparer.Ordinal)
                .Select(c => c with {
                    Name = NormalizeScalar(c.Name),
                    StableIdField = NormalizeScalar(c.StableIdField),
                })],
            Metadata = NormalizeDictionary(document.Metadata),
        };

    private static IReadOnlyDictionary<string, string> NormalizeDictionary(IReadOnlyDictionary<string, string>? values)
        => values is null
            ? new SortedDictionary<string, string>(StringComparer.Ordinal)
            : new SortedDictionary<string, string>(
                values.ToDictionary(
                    p => NormalizeScalar(p.Key),
                    p => NormalizeScalar(p.Value),
                    StringComparer.Ordinal),
                StringComparer.Ordinal);

    private static SchemaFieldContract NormalizeField(SchemaFieldContract field)
            => field with {
                Name = NormalizeScalar(field.Name),
                TypeName = NormalizeScalar(field.TypeName),
                JsonType = NormalizeScalar(field.JsonType),
                Title = NormalizeOptional(field.Title),
                Description = NormalizeOptional(field.Description),
                EnumValues = field.EnumValues is null
                ? []
                : [.. field.EnumValues.Select(NormalizeScalar).OrderBy(v => v, StringComparer.Ordinal)],
                ValidationConstraints = NormalizeDictionary(field.ValidationConstraints),
                Metadata = NormalizeDictionary(field.Metadata),
            };

    /// <summary>
    /// Returns null only for null inputs; whitespace-only strings normalize to a sentinel so a
    /// logical "value not provided" is not silently merged with `"   "`. Callers comparing
    /// fingerprints can rely on the distinction surviving canonicalization.
    /// </summary>
    private static string? NormalizeOptional(string? value) {
        if (value is null) {
            return null;
        }

        string normalized = NormalizeScalar(value);
        return normalized.Length == 0 ? AbsentValueSentinel : normalized;
    }

    private static string NormalizeScalar(string value) {
        if (string.IsNullOrEmpty(value)) {
            return string.Empty;
        }

        // P-8: strip a leading BOM and known zero-width characters that arrive from
        // YAML/markdown loaders; normalize Unicode line separators alongside the existing
        // CR/LF normalization so culturally identical strings produce identical hashes.
        var sb = new StringBuilder(value.Length);
        foreach (char c in value) {
            switch (c) {
                case '﻿': // BOM
                case '​': // zero-width space
                case '‌': // zero-width non-joiner
                case '‍': // zero-width joiner
                    continue;
                case '\r':
                    _ = sb.Append('\n');
                    continue;
                case '\u2028': // line separator
                case '\u2029': // paragraph separator
                    _ = sb.Append('\n');
                    continue;
                default:
                    _ = sb.Append(c);
                    continue;
            }
        }

        return sb.ToString().Replace("\r\n", "\n").Trim();
    }

    private static string Sha256Hex(string value) {
        using var sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
        char[] chars = new char[bytes.Length * 2];
        const string Hex = "0123456789abcdef";
        for (int i = 0; i < bytes.Length; i++) {
            chars[i * 2] = Hex[bytes[i] >> 4];
            chars[(i * 2) + 1] = Hex[bytes[i] & 0xF];
        }

        return new string(chars);
    }

    private static SchemaMaterialValidationResult ValidateArray(ref Utf8JsonReader reader, string path) {
        int index = 0;
        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.EndArray) {
                return SchemaMaterialValidationResult.Valid;
            }

            SchemaMaterialValidationResult nested = ValidateJsonValue(ref reader, path + "[" + index.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]");
            if (!nested.IsValid) {
                return nested;
            }

            index++;
        }

        return new(false, SchemaMaterialValidationCategory.MalformedJson, "schema.json.unclosed-array", path);
    }

    private static SchemaMaterialValidationResult ValidateJsonObject(ref Utf8JsonReader reader, string path) {
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) {
            return new(false, SchemaMaterialValidationCategory.MalformedJson, "schema.json.root-object", path);
        }

        return ValidateObjectBody(ref reader, path);
    }

    private static SchemaMaterialValidationResult ValidateJsonValue(ref Utf8JsonReader reader, string path)
        => reader.TokenType switch {
            JsonTokenType.StartObject => ValidateObjectBody(ref reader, path),
            JsonTokenType.StartArray => ValidateArray(ref reader, path),
            JsonTokenType.String or JsonTokenType.Number or JsonTokenType.True or JsonTokenType.False or JsonTokenType.Null
                => SchemaMaterialValidationResult.Valid,
            _ => new(false, SchemaMaterialValidationCategory.MalformedJson, "schema.json.value-invalid", path),
        };

    private static SchemaMaterialValidationResult ValidateObjectBody(ref Utf8JsonReader reader, string path) {
        HashSet<string> exact = new(StringComparer.Ordinal);
        HashSet<string> caseVariant = new(StringComparer.OrdinalIgnoreCase);
        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.EndObject) {
                return SchemaMaterialValidationResult.Valid;
            }

            if (reader.TokenType != JsonTokenType.PropertyName) {
                return new(false, SchemaMaterialValidationCategory.MalformedJson, "schema.json.property-expected", path);
            }

            string property = reader.GetString() ?? string.Empty;
            string propertyPath = path + "." + property;
            if (!exact.Add(property) || !caseVariant.Add(property)) {
                return new(false, SchemaMaterialValidationCategory.DuplicateJsonKey, "schema.json.duplicate-key", propertyPath);
            }

            if (!reader.Read()) {
                return new(false, SchemaMaterialValidationCategory.MalformedJson, "schema.json.value-expected", propertyPath);
            }

            SchemaMaterialValidationResult nested = ValidateJsonValue(ref reader, propertyPath);
            if (!nested.IsValid) {
                return nested;
            }
        }

        return new(false, SchemaMaterialValidationCategory.MalformedJson, "schema.json.unclosed-object", path);
    }
}

/// <summary>
/// Well-known v1 schema fingerprint algorithm identifiers.
/// </summary>
public static class SchemaFingerprintAlgorithm {
    public const string CanonicalizerVersionV1 = "frontcomposer.canonical-json.v1";

    /// <summary>SHA-256 over canonical JSON serialization performed by <see cref="CanonicalSchemaMaterial"/>.</summary>
    public const string Sha256CanonicalJsonV1 = "frontcomposer.schema.sha256.canonical-json.v1";

    /// <summary>
    /// SHA-256 over the SourceTools-emitted newline-delimited key=value canonical blob. Distinct
    /// from <see cref="Sha256CanonicalJsonV1"/> because the build-time canonicalizer cannot share
    /// the runtime System.Text.Json source-gen pipeline (Roslyn analyzer hosting constraint).
    /// Documented as the v1 dual-algorithm contract (D23 in Story 8-6).
    /// </summary>
    public const string Sha256SourceToolsBlobV1 = "frontcomposer.schema.sha256.v1.sourcetools-blob";

    public const string TestVectorIdV1 = "hfc-schema-v1";
}

/// <summary>
/// Sanitized exception type raised by canonical schema material validation failures.
/// The message contains stable category/key only; raw paths and validation values are
/// kept on the typed properties so callers can decide whether to log them.
/// </summary>
public sealed class SchemaMaterialValidationException : InvalidOperationException {

    public SchemaMaterialValidationException(SchemaMaterialValidationResult validation)
        : base(validation?.MessageKey ?? "schema.material.invalid") => Validation = validation ?? throw new ArgumentNullException(nameof(validation));

    public SchemaMaterialValidationResult Validation { get; }
}

/// <summary>
/// SDK-neutral fingerprint metadata emitted with a generated contract descriptor.
/// </summary>
public sealed record SchemaFingerprint(
    string AlgorithmId,
    string Value,
    string CanonicalizerVersion = SchemaFingerprintAlgorithm.CanonicalizerVersionV1,
    string TestVectorId = SchemaFingerprintAlgorithm.TestVectorIdV1);
public sealed record SchemaCollectionContract(
    string Name,
    SchemaCollectionOrder Order,
    string StableIdField);

public sealed record SchemaFieldContract(
    string Name,
    string TypeName,
    string JsonType,
    bool IsRequired,
    bool IsNullable,
    string? Title = null,
    string? Description = null,
    IReadOnlyList<string>? EnumValues = null,
    IReadOnlyDictionary<string, string>? ValidationConstraints = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record SchemaContractDocument(
    string RootDiscriminator,
    SchemaContractFamily Family,
    string ContractId,
    string ContractSchemaVersion,
    string? BoundedContext,
    string? FullyQualifiedName,
    string? ProtocolIdentifier,
    IReadOnlyList<SchemaFieldContract> Fields,
    IReadOnlyList<SchemaCollectionContract> Collections,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record SchemaCanonicalPayload(
    SchemaContractDocument Document,
    string Json,
    SchemaFingerprint Fingerprint);
public sealed record SchemaMaterialValidationResult(
    bool IsValid,
    SchemaMaterialValidationCategory Category,
    string MessageKey,
    string? Path = null) {
    public static SchemaMaterialValidationResult Valid { get; } = new(true, SchemaMaterialValidationCategory.None, "schema.valid");
}

// P-2: pin the JavaScript encoder to a stable Unicode allowlist so default-encoder escape-table
// changes between .NET runtime versions cannot silently drift the canonical JSON bytes (and
// therefore every SHA-256 fingerprint). The source-generation attribute does not expose an
// Encoder property, so the canonical options are built once and consumed by CreatePayload via
// an explicit JsonTypeInfo lookup against the source-gen context.
[JsonSerializable(typeof(SchemaContractDocument))]
[JsonSourceGenerationOptions(WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
internal sealed partial class SchemaFingerprintJsonContext : JsonSerializerContext;
