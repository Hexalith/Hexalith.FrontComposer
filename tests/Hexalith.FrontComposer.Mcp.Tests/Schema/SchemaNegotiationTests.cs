using Hexalith.FrontComposer.Contracts.Schema;
using Hexalith.FrontComposer.Mcp.Schema;

using Shouldly;
using Xunit;

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
