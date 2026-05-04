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
        McpSchemaNegotiationResult result = McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
            IsHiddenOrUnknown: false,
            IsStaleDescriptor: false,
            Client,
            Server,
            HasTrustedBaseline: true,
            HasCompatibleAdditiveDrift: true,
            HasSchemaIntegrityMismatch: false));

        result.Kind.ShouldBe(McpSchemaNegotiationResultKind.CompatibleAdditive);
        result.AllowsSideEffects.ShouldBeTrue();
        result.AgentCategory.ShouldBe("schema-compatible-warning");
    }

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
