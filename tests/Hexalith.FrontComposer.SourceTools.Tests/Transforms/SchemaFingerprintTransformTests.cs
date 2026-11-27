using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Transforms;

[Trait("Category", "MutationErrorHandling")]
public sealed class SchemaFingerprintTransformTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SkillFingerprintDistinguishesElementBoundariesAndPreservesOrdinalOrdering(bool samplePaths)
    {
        GeneratedSchemaPayload Payload(params string[] values) => SchemaFingerprintTransform.CreateSkillCorpusResourcePayload(
            "skill", "1.0", "frontcomposer://skill", "Skill", "text/markdown", 1,
            samplePaths ? [] : values, samplePaths ? values : []);

        GeneratedSchemaPayload first = Payload("A|B", "C");
        first.Json.ShouldContain("schemaVersion=frontcomposer.skill-corpus.v2\n");
        first.Fingerprint.Value.ShouldBe(Payload("C", "A|B").Fingerprint.Value);
        first.Fingerprint.Value.ShouldNotBe(Payload("A", "B|C").Fingerprint.Value);
        first.Fingerprint.Value.ShouldNotBe(Payload("A", "B", "C").Fingerprint.Value);
        Payload("").Fingerprint.Value.ShouldNotBe(Payload().Fingerprint.Value);
        Payload(" ").Fingerprint.Value.ShouldNotBe(Payload().Fingerprint.Value);
        Payload("A", "B", "C").Json.ShouldContain("schemaVersion=frontcomposer.skill-corpus.v1\n");
    }

    [Fact]
    public void CommandFingerprint_ChangesWhenStructuralFieldChanges_AndIgnoresRuntimeNames() {
        const string Source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            using System.ComponentModel.DataAnnotations;
            namespace Orders;
            [Command]
            [BoundedContext("Sales")]
            [Display(Name = "Approve Order")]
            public partial class ApproveOrderCommand {
                public string MessageId { get; set; } = "";
                public string TenantId { get; set; } = "";
                [Display(Name = "Order number")]
                public string OrderNumber { get; set; } = "";
            }
            """;

        CommandModel parsed = CompilationHelper.ParseCommand(Source, "Orders.ApproveOrderCommand").Model!;
        McpCommandDescriptorModel descriptor = McpManifestTransform.TransformCommand(parsed);
        GeneratedSchemaPayload payload = SchemaFingerprintTransform.CreateCommandPayload(descriptor);
        McpCommandDescriptorModel changed = new(
            descriptor.ProtocolName,
            descriptor.CommandTypeName,
            descriptor.BoundedContext,
            "Approve Purchase Order",
            descriptor.Description,
            descriptor.AuthorizationPolicyName,
            descriptor.Parameters,
            descriptor.DerivablePropertyNames);

        payload.Fingerprint.Value.ShouldNotBe(SchemaFingerprintTransform.CreateCommandPayload(changed).Fingerprint.Value);
        payload.Json.ShouldNotContain("TenantId");
        payload.Json.ShouldNotContain("MessageId");
    }

    [Fact]
    public void ResourceFingerprint_IsIndependentOfFieldEnumerationOrder() {
        var fieldA = new McpParameterDescriptorModel("Number", "String", "string", true, false, "Number", null, [], false);
        var fieldB = new McpParameterDescriptorModel("Status", "Enum", "string", true, false, "Status", null, ["Pending", "Approved"], false);
        var left = new McpResourceDescriptorModel(
            "frontcomposer://Sales/projections/OrderQueue",
            "OrderQueue",
            "Orders.OrderQueueProjection",
            "Sales",
            "Order queue",
            null,
            [fieldA, fieldB]);
        var right = new McpResourceDescriptorModel(
            left.ProtocolUri,
            left.Name,
            left.ProjectionTypeName,
            left.BoundedContext,
            left.Title,
            left.Description,
            [fieldB, fieldA]);

        SchemaFingerprintTransform.CreateResourcePayload(left).Fingerprint.Value
            .ShouldBe(SchemaFingerprintTransform.CreateResourcePayload(right).Fingerprint.Value);
    }

    [Fact]
    public void AggregateFingerprint_ChangesWhenNestedFingerprintChanges() {
        GeneratedSchemaFingerprint a = new(SchemaFingerprintTransform.AlgorithmId, new string('a', 64), SchemaFingerprintTransform.CanonicalizerVersion, SchemaFingerprintTransform.TestVectorId);
        GeneratedSchemaFingerprint b = new(SchemaFingerprintTransform.AlgorithmId, new string('b', 64), SchemaFingerprintTransform.CanonicalizerVersion, SchemaFingerprintTransform.TestVectorId);

        GeneratedSchemaFingerprint one = SchemaFingerprintTransform.CreateAggregateManifestPayload([a], [], null, [], []).Fingerprint;
        GeneratedSchemaFingerprint two = SchemaFingerprintTransform.CreateAggregateManifestPayload([b], [], null, [], []).Fingerprint;

        one.Value.ShouldNotBe(two.Value);
    }
}
