using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hexalith.FrontComposer.Contracts.Schema;

/// <summary>
/// Well-known v1 schema fingerprint algorithm identifiers.
/// </summary>
public static class SchemaFingerprintAlgorithm {
    public const string Sha256CanonicalJsonV1 = "frontcomposer.schema.sha256.canonical-json.v1";
    public const string CanonicalizerVersionV1 = "frontcomposer.canonical-json.v1";
    public const string TestVectorIdV1 = "hfc-schema-v1";
}

/// <summary>
/// SDK-neutral fingerprint metadata emitted with a generated contract descriptor.
/// </summary>
public sealed record SchemaFingerprint(
    string AlgorithmId,
    string Value,
    string CanonicalizerVersion = SchemaFingerprintAlgorithm.CanonicalizerVersionV1,
    string TestVectorId = SchemaFingerprintAlgorithm.TestVectorIdV1);

public enum SchemaContractFamily {
    CommandTool,
    ProjectionResource,
    LifecycleResult,
    MarkdownRendererContract,
    SkillCorpusManifest,
    SkillCorpusResource,
    AggregateMcpManifest,
}

public enum SchemaCollectionOrder {
    NonStructuralSorted,
    StructuralOrder,
}

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

public enum SchemaMaterialValidationCategory {
    None,
    DuplicateJsonKey,
    MalformedJson,
    UnknownRootDiscriminator,
    UnknownContractFamily,
    DuplicateStableId,
}

public sealed record SchemaMaterialValidationResult(
    bool IsValid,
    SchemaMaterialValidationCategory Category,
    string MessageKey,
    string? Path = null) {
    public static SchemaMaterialValidationResult Valid { get; } = new(true, SchemaMaterialValidationCategory.None, "schema.valid");
}

/// <summary>
/// Deterministic canonical JSON and SHA-256 fingerprint helper for structural schema material.
/// </summary>
public static class CanonicalSchemaMaterial {
    private const int MaxDepth = 32;
    public static SchemaCanonicalPayload CreatePayload(SchemaContractDocument document) {
        if (document is null) {
            throw new ArgumentNullException(nameof(document));
        }
        SchemaMaterialValidationResult validation = ValidateDocument(document);
        if (!validation.IsValid) {
            throw new InvalidOperationException(validation.MessageKey + ": " + validation.Path);
        }

        SchemaContractDocument normalized = Normalize(document);
        string json = JsonSerializer.Serialize(normalized, SchemaFingerprintJsonContext.Default.SchemaContractDocument);
        string hash = Sha256Hex(json);
        return new SchemaCanonicalPayload(
            normalized,
            json,
            new SchemaFingerprint(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, hash));
    }

    public static SchemaMaterialValidationResult ValidateCanonicalJson(string json) {
        if (string.IsNullOrWhiteSpace(json)) {
            return new(false, SchemaMaterialValidationCategory.MalformedJson, "schema.json.malformed", "$");
        }

        try {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json), new JsonReaderOptions {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = MaxDepth,
            });
            return ValidateJsonObject(ref reader, "$");
        }
        catch (JsonException) {
            return new(false, SchemaMaterialValidationCategory.MalformedJson, "schema.json.malformed", "$");
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

        HashSet<string> fieldNames = new(StringComparer.Ordinal);
        foreach (SchemaFieldContract field in document.Fields) {
            if (!fieldNames.Add(field.Name)) {
                return new(false, SchemaMaterialValidationCategory.DuplicateStableId, "schema.field.duplicate-id", "$.Fields." + field.Name);
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

    private static IReadOnlyDictionary<string, string> NormalizeDictionary(IReadOnlyDictionary<string, string>? values)
        => values is null
            ? new SortedDictionary<string, string>(StringComparer.Ordinal)
            : new SortedDictionary<string, string>(
                values.ToDictionary(
                    p => NormalizeScalar(p.Key),
                    p => NormalizeScalar(p.Value),
                    StringComparer.Ordinal),
                StringComparer.Ordinal);

    private static string NormalizeScalar(string value)
        => value.Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Trim();

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : NormalizeScalar(value!);

    private static string Sha256Hex(string value) {
        using SHA256 sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
        char[] chars = new char[bytes.Length * 2];
        const string Hex = "0123456789abcdef";
        for (int i = 0; i < bytes.Length; i++) {
            chars[i * 2] = Hex[bytes[i] >> 4];
            chars[(i * 2) + 1] = Hex[bytes[i] & 0xF];
        }

        return new string(chars);
    }

    private static SchemaMaterialValidationResult ValidateJsonObject(ref Utf8JsonReader reader, string path) {
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) {
            return new(false, SchemaMaterialValidationCategory.MalformedJson, "schema.json.root-object", path);
        }

        return ValidateObjectBody(ref reader, path);
    }

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

    private static SchemaMaterialValidationResult ValidateJsonValue(ref Utf8JsonReader reader, string path)
        => reader.TokenType switch {
            JsonTokenType.StartObject => ValidateObjectBody(ref reader, path),
            JsonTokenType.StartArray => ValidateArray(ref reader, path),
            JsonTokenType.String or JsonTokenType.Number or JsonTokenType.True or JsonTokenType.False or JsonTokenType.Null
                => SchemaMaterialValidationResult.Valid,
            _ => new(false, SchemaMaterialValidationCategory.MalformedJson, "schema.json.value-invalid", path),
        };

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
}

[JsonSerializable(typeof(SchemaContractDocument))]
[JsonSourceGenerationOptions(WriteIndented = false)]
internal sealed partial class SchemaFingerprintJsonContext : JsonSerializerContext;
