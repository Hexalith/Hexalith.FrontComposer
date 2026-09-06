using Hexalith.FrontComposer.Contracts.Schema;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Schema;

/// <summary>
/// Story 11.21 golden-vector guard for <see cref="CanonicalSchemaMaterial"/>. The Recommended-analyzer
/// burn-down rewrote three constructs inside the canonicalizer: <c>SHA256.Create().ComputeHash</c>
/// became the static <c>SHA256.HashData</c> (CA1850), <c>Enum.IsDefined(Type, object)</c> became the
/// generic overload (CA2263), and the private <c>NormalizeDictionary</c> return type was narrowed to
/// <see cref="SortedDictionary{TKey, TValue}"/> (CA1859). All three are required to be observationally
/// inert. The pre-existing tests only assert self-consistency (same input to same fingerprint), which
/// cannot detect a whole-algorithm drift, so this pins the literal canonical JSON and the literal
/// SHA-256 hex for a fixed document.
/// </summary>
public sealed class CanonicalSchemaMaterialFingerprintVectorTests {
    private const string ExpectedCanonicalJson =
        """{"RootDiscriminator":"frontcomposer.schema.contract.v1","Family":0,"ContractId":"Sales.ApproveOrder.Execute","ContractSchemaVersion":"frontcomposer.command-tool.v1","BoundedContext":"Sales","FullyQualifiedName":"Orders.ApproveOrderCommand","ProtocolIdentifier":"Sales.ApproveOrder.Execute","Fields":[{"Name":"Comment","TypeName":"String","JsonType":"string","IsRequired":false,"IsNullable":true,"Title":null,"Description":null,"EnumValues":[],"ValidationConstraints":{},"Metadata":{}},{"Name":"OrderNumber","TypeName":"String","JsonType":"string","IsRequired":true,"IsNullable":false,"Title":null,"Description":null,"EnumValues":[],"ValidationConstraints":{},"Metadata":{}},{"Name":"Status","TypeName":"String","JsonType":"string","IsRequired":true,"IsNullable":false,"Title":null,"Description":null,"EnumValues":["Pending","approved","pending"],"ValidationConstraints":{},"Metadata":{}}],"Collections":[{"Name":"Events","Order":1,"StableIdField":"eventId"},{"Name":"commands","Order":0,"StableIdField":"commandId"}],"Metadata":{"title":"Approve Order"}}""";

    /// <summary>
    /// SHA-256 of <see cref="ExpectedCanonicalJson"/> as UTF-8 bytes, lowercase hex. Derived
    /// independently of the product code with <c>sha256sum</c> so the pin cannot be circular:
    /// <c>printf '%s' "&lt;canonical json&gt;" | sha256sum</c>.
    /// </summary>
    private const string ExpectedFingerprint = "0c44b927ac1a3a56ada1dadeac6e08a4b16b79ad909d1c12f74e4c7006cd6766";

    [Fact]
    public void CreatePayload_PinnedDocument_ProducesTheExactCanonicalJsonAndFingerprint() {
        SchemaCanonicalPayload payload = CanonicalSchemaMaterial.CreatePayload(PinnedDocument());

        payload.Json.ShouldBe(ExpectedCanonicalJson);
        payload.Fingerprint.Value.ShouldBe(ExpectedFingerprint);
        payload.Fingerprint.AlgorithmId.ShouldBe(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1);
        payload.Fingerprint.CanonicalizerVersion.ShouldBe(SchemaFingerprintAlgorithm.CanonicalizerVersionV1);
    }

    [Fact]
    public void CreatePayload_EquivalentCollectionAndEnumPermutations_ProduceThePinnedVector() {
        SchemaContractDocument original = PinnedDocument();
        SchemaContractDocument permuted = original with {
            Fields = [.. original.Fields.Select(field => field.Name == "Status"
                ? field with { EnumValues = ["approved", "pending", "Pending"] }
                : field)],
            Collections = [.. original.Collections.Reverse()],
        };

        SchemaCanonicalPayload first = CanonicalSchemaMaterial.CreatePayload(original);
        SchemaCanonicalPayload second = CanonicalSchemaMaterial.CreatePayload(permuted);

        first.Json.ShouldBe(ExpectedCanonicalJson);
        second.Json.ShouldBe(ExpectedCanonicalJson);
        first.Fingerprint.Value.ShouldBe(ExpectedFingerprint);
        second.Fingerprint.Value.ShouldBe(ExpectedFingerprint);
        first.Fingerprint.AlgorithmId.ShouldBe(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1);
        second.Fingerprint.AlgorithmId.ShouldBe(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1);
        first.Fingerprint.CanonicalizerVersion.ShouldBe(SchemaFingerprintAlgorithm.CanonicalizerVersionV1);
        second.Fingerprint.CanonicalizerVersion.ShouldBe(SchemaFingerprintAlgorithm.CanonicalizerVersionV1);
    }

    [Fact]
    public void ValidateDocument_UndefinedFamilyValue_IsRejected() {
        // Guards the CA2263 rewrite of the Enum.IsDefined call: an out-of-range family must still be
        // rejected with the UnknownContractFamily category rather than silently canonicalized.
        SchemaContractDocument document = PinnedDocument() with { Family = (SchemaContractFamily)9999 };

        SchemaMaterialValidationResult result = CanonicalSchemaMaterial.ValidateDocument(document);

        result.IsValid.ShouldBeFalse();
        result.Category.ShouldBe(SchemaMaterialValidationCategory.UnknownContractFamily);
        result.MessageKey.ShouldBe("schema.family.unknown");
    }

    [Fact]
    public void ValidateDocument_EveryDefinedFamilyValue_IsAccepted() {
        foreach (SchemaContractFamily family in Enum.GetValues<SchemaContractFamily>()) {
            CanonicalSchemaMaterial.ValidateDocument(PinnedDocument() with { Family = family })
                .IsValid
                .ShouldBeTrue($"{family} is a defined contract family.");
        }
    }

    [Fact]
    public void CreatePayload_MetadataInsertionOrder_DoesNotAffectTheFingerprint() {
        // Guards the CA1859 narrowing of NormalizeDictionary: the returned map must remain ordinal
        // SortedDictionary-ordered so metadata insertion order cannot leak into the canonical bytes.
        SchemaContractDocument reordered = PinnedDocument() with {
            Metadata = new Dictionary<string, string> {
                ["zeta"] = "last",
                ["alpha"] = "first",
                ["title"] = "Approve Order",
            },
        };
        SchemaContractDocument sameContentDifferentOrder = PinnedDocument() with {
            Metadata = new Dictionary<string, string> {
                ["title"] = "Approve Order",
                ["alpha"] = "first",
                ["zeta"] = "last",
            },
        };

        SchemaCanonicalPayload first = CanonicalSchemaMaterial.CreatePayload(reordered);
        SchemaCanonicalPayload second = CanonicalSchemaMaterial.CreatePayload(sameContentDifferentOrder);

        second.Json.ShouldBe(first.Json);
        second.Fingerprint.Value.ShouldBe(first.Fingerprint.Value);
        first.Json.ShouldContain("""{"alpha":"first","title":"Approve Order","zeta":"last"}""");
    }

    [Fact]
    public void CreatePayload_FieldNestedMapInsertionOrder_DoesNotAffectTheFingerprint() {
        // NormalizeDictionary is also applied to per-field Metadata and ValidationConstraints.
        SchemaFieldContract commentFirstOrder = new(
            "Comment",
            "String",
            "string",
            false,
            true,
            ValidationConstraints: new Dictionary<string, string> {
                ["maxLength"] = "256",
                ["minLength"] = "1",
            },
            Metadata: new Dictionary<string, string> {
                ["zeta"] = "last",
                ["alpha"] = "first",
            });
        SchemaFieldContract commentSecondOrder = new(
            "Comment",
            "String",
            "string",
            false,
            true,
            ValidationConstraints: new Dictionary<string, string> {
                ["minLength"] = "1",
                ["maxLength"] = "256",
            },
            Metadata: new Dictionary<string, string> {
                ["alpha"] = "first",
                ["zeta"] = "last",
            });
        SchemaFieldContract orderNumber = new("OrderNumber", "String", "string", true, false);

        SchemaCanonicalPayload first = CanonicalSchemaMaterial.CreatePayload(
            PinnedDocument() with { Fields = [orderNumber, commentFirstOrder] });
        SchemaCanonicalPayload second = CanonicalSchemaMaterial.CreatePayload(
            PinnedDocument() with { Fields = [orderNumber, commentSecondOrder] });

        second.Json.ShouldBe(first.Json);
        second.Fingerprint.Value.ShouldBe(first.Fingerprint.Value);
        first.Json.ShouldContain("""{"alpha":"first","zeta":"last"}""");
        first.Json.ShouldContain("""{"maxLength":"256","minLength":"1"}""");
    }

    [Fact]
    public void CreatePayload_NullDocument_ThrowsArgumentNullExceptionNamingTheParameter() {
        // Guards the CA1510 rewrite to ArgumentNullException.ThrowIfNull: the exception type and the
        // parameter name are part of the published contract.
        ArgumentNullException thrown = Should.Throw<ArgumentNullException>(
            () => CanonicalSchemaMaterial.CreatePayload(null!));

        thrown.ParamName.ShouldBe("document");
    }

    private static SchemaContractDocument PinnedDocument() => new(
        "frontcomposer.schema.contract.v1",
        SchemaContractFamily.CommandTool,
        "Sales.ApproveOrder.Execute",
        "frontcomposer.command-tool.v1",
        "Sales",
        "Orders.ApproveOrderCommand",
        "Sales.ApproveOrder.Execute",
        [
            new SchemaFieldContract(
                "Status",
                "String",
                "string",
                true,
                false,
                EnumValues: ["pending", "approved", "Pending"]),
            new SchemaFieldContract("OrderNumber", "String", "string", true, false),
            new SchemaFieldContract("Comment", "String", "string", false, true),
        ],
        [
            new SchemaCollectionContract("commands", SchemaCollectionOrder.NonStructuralSorted, "commandId"),
            new SchemaCollectionContract("Events", SchemaCollectionOrder.StructuralOrder, "eventId"),
        ],
        new Dictionary<string, string> {
            ["title"] = "Approve Order",
        });
}
