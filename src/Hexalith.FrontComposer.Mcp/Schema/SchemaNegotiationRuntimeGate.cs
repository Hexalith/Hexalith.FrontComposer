using System.Reflection;
using System.Text.Json.Nodes;

using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Contracts.Schema;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.FrontComposer.Mcp.Schema;

internal static class SchemaNegotiationRuntimeGate {
    private const string PackageOwner = "Hexalith.FrontComposer";
    private const string DefaultFixtureId = "baseline-known-v1";

    public static McpSchemaNegotiationResult? EvaluateResource(
        McpResourceDescriptor descriptor,
        IFrontComposerMcpAgentContextAccessor accessor,
        IServiceProvider services) {
        SchemaFingerprint? client = TryGetClientFingerprint(accessor);
        if (client is null) {
            return null;
        }

        SchemaBaselineSnapshot? baseline = TryResolveBaseline(services, SchemaContractFamily.ProjectionResource);
        SchemaBaselineSnapshot? server = CreateResourceSnapshot(descriptor);
        return McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
            IsHiddenOrUnknown: false,
            IsStaleDescriptor: false,
            client,
            descriptor.Fingerprint,
            HasTrustedBaseline: baseline is not null || descriptor.Fingerprint is not null,
#pragma warning disable CS0618
            HasCompatibleAdditiveDrift: false,
#pragma warning restore CS0618
            HasSchemaIntegrityMismatch: false,
            Baseline: baseline,
            Server: server));
    }

    public static McpSchemaNegotiationResult? EvaluateCommand(
        McpCommandDescriptor descriptor,
        IFrontComposerMcpAgentContextAccessor accessor,
        IServiceProvider services) {
        SchemaFingerprint? client = TryGetClientFingerprint(accessor);
        if (client is null) {
            return null;
        }

        SchemaBaselineSnapshot? baseline = TryResolveBaseline(services, SchemaContractFamily.CommandTool);
        SchemaBaselineSnapshot? server = CreateCommandSnapshot(descriptor);
        return McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
            IsHiddenOrUnknown: false,
            IsStaleDescriptor: false,
            client,
            descriptor.Fingerprint,
            HasTrustedBaseline: baseline is not null || descriptor.Fingerprint is not null,
#pragma warning disable CS0618
            HasCompatibleAdditiveDrift: false,
#pragma warning restore CS0618
            HasSchemaIntegrityMismatch: false,
            Baseline: baseline,
            Server: server));
    }

    public static FrontComposerMcpResult ToStructuredFailure(FrontComposerMcpFailureCategory category) {
        SchemaFailureContract contract = MapSchemaFailure(category);
        return FrontComposerMcpResult.Failure(category, contract.SafeText, BuildStructuredPayload(contract));
    }

    public static JsonObject BuildStructuredFailure(FrontComposerMcpFailureCategory category)
        => BuildStructuredPayload(MapSchemaFailure(category));

    private static JsonObject BuildStructuredPayload(SchemaFailureContract contract)
        => new() {
            ["category"] = contract.AgentCategory,
            ["message"] = contract.SafeText,
            ["docsCode"] = contract.DocsCode,
            ["retryable"] = contract.Retryable,
            ["refreshResources"] = contract.RefreshResources,
            ["isHiddenEquivalent"] = false,
        };

    // 8-6a review M4: align command/tool schema-failure payload with the projection mapper's
    // shape (include `message`, drop hardcoded retryable=false). Per-category retryable mirrors
    // the projection mapper's table — keep the surfaces consistent so agents see the same
    // remediation cues regardless of whether the call hit a projection read or a command/tool
    // dispatch path.
    private static SchemaFailureContract MapSchemaFailure(FrontComposerMcpFailureCategory category)
        => category switch {
            FrontComposerMcpFailureCategory.SchemaMismatch => new(
                "schema-mismatch",
                "Schema is not compatible with the client manifest. Refresh schema metadata and retry.",
                "HFC-SCHEMA-MISMATCH",
                Retryable: false,
                RefreshResources: false),
            FrontComposerMcpFailureCategory.UnknownSchemaBaseline => new(
                "schema-unavailable",
                "Schema baseline is unavailable. Refresh schema metadata or contact the host maintainer.",
                "HFC-SCHEMA-UNKNOWN-BASELINE",
                Retryable: false,
                RefreshResources: false),
            FrontComposerMcpFailureCategory.UnsupportedSchemaAlgorithm => new(
                "unsupported-schema-fingerprint",
                "Schema fingerprint algorithm is not supported by this server.",
                "HFC-SCHEMA-UNSUPPORTED-ALGORITHM",
                Retryable: false,
                RefreshResources: false),
            FrontComposerMcpFailureCategory.SchemaIntegrityMismatch => new(
                "schema-unavailable",
                "Schema metadata failed an integrity check.",
                "HFC-SCHEMA-INTEGRITY-MISMATCH",
                Retryable: false,
                RefreshResources: false),
            _ => new(
                "downstream_failed",
                "Request failed.",
                "HFC-MCP-DOWNSTREAM-FAILED",
                Retryable: true,
                RefreshResources: false),
        };

    private sealed record SchemaFailureContract(
        string AgentCategory,
        string SafeText,
        string DocsCode,
        bool Retryable,
        bool RefreshResources);

    private static SchemaFingerprint? TryGetClientFingerprint(IFrontComposerMcpAgentContextAccessor accessor) {
        PropertyInfo? property = accessor.GetType().GetProperty("ClientFingerprintHint", BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(accessor) as SchemaFingerprint;
    }

    private static SchemaBaselineSnapshot? TryResolveBaseline(IServiceProvider services, SchemaContractFamily family) {
        ISchemaBaselineProvider? provider = services.GetService<ISchemaBaselineProvider>();
        return provider is not null
            && provider.TryResolve(family, PackageOwner, DefaultFixtureId, out SchemaBaselineSnapshot? snapshot)
            ? snapshot
            : null;
    }

    private static SchemaBaselineSnapshot CreateResourceSnapshot(McpResourceDescriptor descriptor) {
        SchemaContractDocument document = new(
            "frontcomposer.schema.contract.v1",
            SchemaContractFamily.ProjectionResource,
            descriptor.ProtocolUri,
            "frontcomposer.projection-resource.v1",
            descriptor.BoundedContext,
            descriptor.ProjectionTypeName,
            descriptor.ProtocolUri,
            descriptor.Fields.Select(ToField).ToArray(),
            [new SchemaCollectionContract("fields", SchemaCollectionOrder.NonStructuralSorted, "name")],
            new Dictionary<string, string> {
                ["renderStrategy"] = descriptor.RenderStrategy.ToString(),
            });
        return Snapshot(document, descriptor.Fingerprint, SchemaContractFamily.ProjectionResource);
    }

    private static SchemaBaselineSnapshot CreateCommandSnapshot(McpCommandDescriptor descriptor) {
        SchemaContractDocument document = new(
            "frontcomposer.schema.contract.v1",
            SchemaContractFamily.CommandTool,
            descriptor.ProtocolName,
            "frontcomposer.command-tool.v1",
            descriptor.BoundedContext,
            descriptor.CommandTypeName,
            descriptor.ProtocolName,
            descriptor.Parameters.Select(ToField).ToArray(),
            [new SchemaCollectionContract("parameters", SchemaCollectionOrder.NonStructuralSorted, "name")],
            new Dictionary<string, string>());
        return Snapshot(document, descriptor.Fingerprint, SchemaContractFamily.CommandTool);
    }

    private static SchemaBaselineSnapshot Snapshot(
        SchemaContractDocument document,
        SchemaFingerprint? descriptorFingerprint,
        SchemaContractFamily family) {
        SchemaCanonicalPayload payload = CanonicalSchemaMaterial.CreatePayload(document);
        SchemaFingerprint fingerprint = descriptorFingerprint ?? payload.Fingerprint;
        return new SchemaBaselineSnapshot(
            new SchemaBaselineProvenance(
                family,
                document.ContractSchemaVersion,
                fingerprint.AlgorithmId,
                PackageOwner,
                DefaultFixtureId,
                requiresMigrationGuide: false,
                fingerprint.CanonicalizerVersion,
                fingerprint.TestVectorId),
            payload.Document,
            fingerprint);
    }

    private static SchemaFieldContract ToField(McpParameterDescriptor parameter)
        => new(
            parameter.Name,
            parameter.TypeName,
            parameter.JsonType,
            parameter.IsRequired,
            parameter.IsNullable,
            parameter.Title,
            parameter.Description,
            parameter.EnumValues);
}
