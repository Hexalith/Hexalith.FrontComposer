using Hexalith.FrontComposer.Contracts.Schema;
using Hexalith.FrontComposer.Mcp.Schema;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Schema;

public sealed class SchemaNegotiationTests {
    private static readonly SchemaFingerprint Server = new(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, new string('a', 64));
    private static readonly SchemaFingerprint Client = new(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, new string('b', 64));

    [Fact]
    public void Negotiate_HiddenAndStalePrecedenceWinBeforeSchemaMismatch() {
        McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
                IsHiddenOrUnknown: true,
                IsStaleDescriptor: true,
                Client,
                Server,
                HasTrustedBaseline: false,
                HasCompatibleAdditiveDrift: false,
                HasSchemaIntegrityMismatch: true))
            .Kind.ShouldBe(McpSchemaNegotiationResultKind.HiddenOrUnknown);

        McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
                IsHiddenOrUnknown: false,
                IsStaleDescriptor: true,
                Client,
                Server,
                HasTrustedBaseline: true,
                HasCompatibleAdditiveDrift: false,
                HasSchemaIntegrityMismatch: false))
            .Kind.ShouldBe(McpSchemaNegotiationResultKind.StaleDescriptor);
    }

    [Fact]
    public void Negotiate_UnsupportedAlgorithm_DoesNotCompareHashStrings() {
        var unsupported = new SchemaFingerprint("frontcomposer.schema.sha512.future", Server.Value);

        McpSchemaNegotiationResult result = McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
            IsHiddenOrUnknown: false,
            IsStaleDescriptor: false,
            unsupported,
            Server,
            HasTrustedBaseline: true,
            HasCompatibleAdditiveDrift: true,
            HasSchemaIntegrityMismatch: false));

        result.Kind.ShouldBe(McpSchemaNegotiationResultKind.UnsupportedAlgorithm);
        result.AllowsSideEffects.ShouldBeFalse();
        result.AgentCategory.ShouldBe("unsupported-schema-fingerprint");
    }

    [Fact]
    public void Negotiate_SameValueDifferentSupportedAlgorithm_IsNotExact() {
        SchemaFingerprint sourceToolsClient = new(SchemaFingerprintAlgorithm.Sha256SourceToolsBlobV1, Server.Value);

        McpSchemaNegotiationResult result = McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
            IsHiddenOrUnknown: false,
            IsStaleDescriptor: false,
            sourceToolsClient,
            Server,
            HasTrustedBaseline: false,
            HasCompatibleAdditiveDrift: false,
            HasSchemaIntegrityMismatch: false));

        result.Kind.ShouldBe(McpSchemaNegotiationResultKind.UnknownServerBaseline);
        result.AllowsSideEffects.ShouldBeFalse();
        // 11-5 review P6 / D6: pin the public wire-contract category and message key so a
        // future change that collapses different schema failures into a generic "schema-
        // mismatch" agent category surfaces here. AC11 requires consistent naming or an
        // explicit compatibility decision; a silent rename is forbidden.
        result.AgentCategory.ShouldBe("schema-unavailable");
        result.MessageKey.ShouldBe("schema.baseline.unknown");
        result.FailureCategory.ShouldBe(FrontComposerMcpFailureCategory.UnknownSchemaBaseline);
    }

    [Fact]
    public void Negotiate_CompatibleAdditive_AllowsOnlyAdmittedPath() {
        // 8-6a review M1: the legacy `HasCompatibleAdditiveDrift` bool is now ignored per AC6;
        // additive compatibility must be derived from baseline+server snapshot inputs through
        // the analyzer. Build a baseline with one required field and a server snapshot that adds
        // an optional field — analyzer classifies the diff as AdditiveCompatible.
        SchemaBaselineSnapshot baseline = BuildSnapshot(
            fingerprintValue: new string('a', 64),
            fields: [new SchemaFieldContract("Number", "Int32", "integer", true, false)]);
        SchemaBaselineSnapshot server = BuildSnapshot(
            fingerprintValue: new string('b', 64),
            fields: [
                new SchemaFieldContract("Number", "Int32", "integer", true, false),
                new SchemaFieldContract("Note", "String", "string", false, true),
            ]);

        McpSchemaNegotiationResult result = McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
            IsHiddenOrUnknown: false,
            IsStaleDescriptor: false,
            Client,
            Server,
            HasTrustedBaseline: true,
            HasCompatibleAdditiveDrift: false,
            HasSchemaIntegrityMismatch: false,
            Baseline: baseline,
            Server: server));

        result.Kind.ShouldBe(McpSchemaNegotiationResultKind.CompatibleAdditive);
        result.AllowsSideEffects.ShouldBeTrue();
        result.AgentCategory.ShouldBe("schema-compatible-warning");
    }

    [Fact]
    public void Negotiate_CompatibleWarning_AllowsSideEffects_WithWarningClassification() {
        SchemaBaselineSnapshot baseline = BuildSnapshot(
            fingerprintValue: new string('a', 64),
            fields: [
                new SchemaFieldContract(
                    "Status",
                    "String",
                    "string",
                    true,
                    false,
                    EnumValues: ["Pending", "Paid"]),
            ]);
        SchemaBaselineSnapshot server = BuildSnapshot(
            fingerprintValue: new string('b', 64),
            fields: [
                new SchemaFieldContract(
                    "Status",
                    "String",
                    "string",
                    true,
                    false,
                    EnumValues: ["Pending", "Paid", "Canceled"]),
            ]);

        McpSchemaNegotiationResult result = McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
            IsHiddenOrUnknown: false,
            IsStaleDescriptor: false,
            Client,
            Server,
            HasTrustedBaseline: true,
            HasCompatibleAdditiveDrift: false,
            HasSchemaIntegrityMismatch: false,
            Baseline: baseline,
            Server: server));

        result.Kind.ShouldBe(McpSchemaNegotiationResultKind.CompatibleWarning);
        result.AllowsSideEffects.ShouldBeTrue();
        result.AgentCategory.ShouldBe("schema-compatible-warning");
        result.MessageKey.ShouldBe("schema.compatible-warning");
        result.DocsCode.ShouldBe("HFC-SCHEMA-COMPATIBLE-WARNING");
    }

    private static SchemaBaselineSnapshot BuildSnapshot(string fingerprintValue, IReadOnlyList<SchemaFieldContract> fields)
        => new(
            new SchemaBaselineProvenance(
                SchemaContractFamily.ProjectionResource,
                "frontcomposer.projection-resource.v1",
                SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1,
                "Hexalith.FrontComposer",
                "test-vector",
                requiresMigrationGuide: false),
            new SchemaContractDocument(
                "frontcomposer.schema.contract.v1",
                SchemaContractFamily.ProjectionResource,
                "frontcomposer://test/baseline",
                "frontcomposer.projection-resource.v1",
                "Hexalith",
                "Hexalith.FrontComposer.Test",
                "frontcomposer://test/baseline",
                fields,
                [new SchemaCollectionContract("fields", SchemaCollectionOrder.NonStructuralSorted, "name")],
                new Dictionary<string, string>()),
            new SchemaFingerprint(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, fingerprintValue));

    [Fact]
    public void Negotiate_IncompatibleBlocksSideEffects() {
        McpSchemaNegotiationResult result = McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
            IsHiddenOrUnknown: false,
            IsStaleDescriptor: false,
            Client,
            Server,
            HasTrustedBaseline: true,
            HasCompatibleAdditiveDrift: false,
            HasSchemaIntegrityMismatch: false));

        result.Kind.ShouldBe(McpSchemaNegotiationResultKind.Incompatible);
        result.AllowsSideEffects.ShouldBeFalse();
        result.FailureCategory.ShouldBe(FrontComposerMcpFailureCategory.SchemaMismatch);
    }
}
