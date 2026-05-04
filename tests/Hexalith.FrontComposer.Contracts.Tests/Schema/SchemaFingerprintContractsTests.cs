using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Schema;

using Shouldly;
using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Schema;

public sealed class SchemaFingerprintContractsTests {
    [Fact]
    public void CreatePayload_ProducesStableLowercaseSha256Fingerprint() {
        var document = new SchemaContractDocument(
            "frontcomposer.schema.contract.v1",
            SchemaContractFamily.CommandTool,
            "Sales.ApproveOrder.Execute",
            "frontcomposer.command-tool.v1",
            "Sales",
            "Orders.ApproveOrderCommand",
            "Sales.ApproveOrder.Execute",
            [
                new SchemaFieldContract("OrderNumber", "String", "string", true, false),
                new SchemaFieldContract("Comment", "String", "string", false, true),
            ],
            [new SchemaCollectionContract("fields", SchemaCollectionOrder.NonStructuralSorted, "name")],
            new Dictionary<string, string> {
                ["title"] = "Approve Order",
            });

        SchemaCanonicalPayload first = CanonicalSchemaMaterial.CreatePayload(document);
        SchemaCanonicalPayload second = CanonicalSchemaMaterial.CreatePayload(document with {
            Fields = [.. document.Fields.Reverse()],
        });

        second.Json.ShouldBe(first.Json);
        second.Fingerprint.ShouldBe(first.Fingerprint);
        first.Fingerprint.AlgorithmId.ShouldBe(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1);
        first.Fingerprint.Value.ShouldBe(first.Fingerprint.Value.ToLowerInvariant());
        first.Fingerprint.Value.Length.ShouldBe(64);
    }

    [Fact]
    public void ValidateCanonicalJson_RejectsExactAndCaseVariantDuplicateKeys() {
        CanonicalSchemaMaterial.ValidateCanonicalJson("""{"id":1,"id":2}""")
            .Category.ShouldBe(SchemaMaterialValidationCategory.DuplicateJsonKey);

        CanonicalSchemaMaterial.ValidateCanonicalJson("""{"id":1,"ID":2}""")
            .Category.ShouldBe(SchemaMaterialValidationCategory.DuplicateJsonKey);
    }

    [Fact]
    public void RenderContract_ModelsSurfaceMetadataWithoutRuntimeRows() {
        var contract = new FrontComposerRenderContract(
            "frontcomposer://Sales/projections/OrderQueue#markdown",
            "frontcomposer.renderer.markdown.v1",
            RenderSurfaceKind.McpMarkdown,
            "text/markdown",
            [RenderCapability.BoundedMarkdown, RenderCapability.SanitizedInertText],
            new SchemaFingerprint(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, new string('0', 64)),
            new RenderBounds(100, 12, 64_000, 4_096));

        contract.OutputContentType.ShouldBe("text/markdown");
        contract.Capabilities.ShouldContain(RenderCapability.SanitizedInertText);
        contract.Metadata.ShouldBeNull();
    }
}
