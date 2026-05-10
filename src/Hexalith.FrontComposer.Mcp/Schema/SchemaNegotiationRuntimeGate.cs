using System.Text.Json.Nodes;

using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Contracts.Schema;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hexalith.FrontComposer.Mcp.Schema;

internal sealed class SchemaNegotiationRuntimeGate {
    private const string PackageOwner = "Hexalith.FrontComposer";
    private const string DefaultFixtureId = "baseline-known-v1";

    public static McpSchemaNegotiationResult? EvaluateResource(
        McpResourceDescriptor descriptor,
        IFrontComposerMcpAgentContextAccessor accessor,
        IServiceProvider services) {
        SchemaFingerprint? client = accessor.ClientFingerprintHint;
        if (client is null) {
            return null;
        }

        SchemaBaselineSnapshot? baseline = TryResolveBaseline(services, accessor, SchemaContractFamily.ProjectionResource);
        SchemaBaselineSnapshot server = CreateResourceSnapshot(descriptor);
        // The descriptor's emitter-stamped fingerprint is the trust anchor for byte-match against
        // the client claim; the freshly-built server snapshot drives structural snapshot
        // comparison. Algorithm divergence (e.g. descriptor stamped with Sha256SourceToolsBlobV1
        // vs server snapshot's Sha256CanonicalJsonV1) is acknowledged as a known v1 limitation
        // (D23) — clients computing a hash with the same canonicalizer as the descriptor produce
        // comparable byte-match values; cross-algorithm clients fall through to the structural
        // snapshot comparator below.
        SchemaFingerprint serverFingerprint = descriptor.Fingerprint ?? server.Fingerprint;
        return LogAndReturn(McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
            IsHiddenOrUnknown: false,
            IsStaleDescriptor: false,
            client,
            serverFingerprint,
            HasTrustedBaseline: baseline is not null || descriptor.Fingerprint is not null,
#pragma warning disable CS0618, HFC4001
            HasCompatibleAdditiveDrift: false,
#pragma warning restore CS0618, HFC4001
            HasSchemaIntegrityMismatch: false,
            Baseline: baseline,
            Server: server)), accessor, services);
    }

    public static McpSchemaNegotiationResult? EvaluateCommand(
        McpCommandDescriptor descriptor,
        IFrontComposerMcpAgentContextAccessor accessor,
        IServiceProvider services) {
        SchemaFingerprint? client = accessor.ClientFingerprintHint;
        if (client is null) {
            return null;
        }

        SchemaBaselineSnapshot? baseline = TryResolveBaseline(services, accessor, SchemaContractFamily.CommandTool);
        SchemaBaselineSnapshot server = CreateCommandSnapshot(descriptor);
        SchemaFingerprint serverFingerprint = descriptor.Fingerprint ?? server.Fingerprint;
        return LogAndReturn(McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
            IsHiddenOrUnknown: false,
            IsStaleDescriptor: false,
            client,
            serverFingerprint,
            HasTrustedBaseline: baseline is not null || descriptor.Fingerprint is not null,
#pragma warning disable CS0618, HFC4001
            HasCompatibleAdditiveDrift: false,
#pragma warning restore CS0618, HFC4001
            HasSchemaIntegrityMismatch: false,
            Baseline: baseline,
            Server: server)), accessor, services);
    }

    public static FrontComposerMcpResult ToStructuredFailure(FrontComposerMcpFailureCategory category) {
        SchemaFailureContract contract = MapSchemaFailureStrict(category);
        return FrontComposerMcpResult.Failure(category, contract.SafeText, BuildStructuredPayload(contract));
    }

    public static JsonObject BuildStructuredFailure(FrontComposerMcpFailureCategory category)
        => BuildStructuredPayload(MapSchemaFailureStrict(category));

    private static JsonObject BuildStructuredPayload(SchemaFailureContract contract)
        => new() {
            ["category"] = contract.AgentCategory,
            ["message"] = contract.SafeText,
            ["docsCode"] = contract.DocsCode,
            ["retryable"] = contract.Retryable,
            ["refreshResources"] = contract.RefreshResources,
            ["isHiddenEquivalent"] = false,
        };

    /// <summary>
    /// 8-6a re-review: the default branch was reachable when callers passed a non-schema
    /// category by mistake. The structured payload reported `downstream_failed` while the
    /// outer envelope kept the original category, breaking the agent branching contract.
    /// Asserting via ArgumentException keeps the contract explicit at call sites that intend
    /// a real schema failure mapping.
    /// </summary>
    private static SchemaFailureContract MapSchemaFailureStrict(FrontComposerMcpFailureCategory category)
        => category is FrontComposerMcpFailureCategory.SchemaMismatch
            or FrontComposerMcpFailureCategory.UnknownSchemaBaseline
            or FrontComposerMcpFailureCategory.UnsupportedSchemaAlgorithm
            or FrontComposerMcpFailureCategory.UnsupportedSchema
            or FrontComposerMcpFailureCategory.SchemaIntegrityMismatch
            ? MapSchemaFailure(category)
            : throw new ArgumentException(
                $"Category {category} is not a schema-failure category.", nameof(category));

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
            // C4 (Group D / chunk-2 re-review): SchemaNegotiation maps the UnknownClientVersion
            // arm to UnsupportedSchema; without this case the strict mapper threw ArgumentException
            // and the structured payload silently collapsed to DownstreamFailed via the outer catch.
            // Distinct agent category from UnsupportedSchemaAlgorithm so agents can branch between
            // "your schema version is unknown" and "your fingerprint algorithm is unsupported".
            FrontComposerMcpFailureCategory.UnsupportedSchema => new(
                "unsupported-schema-version",
                "Schema version declared by the client is not supported by this server.",
                "HFC-SCHEMA-UNSUPPORTED-VERSION",
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
                "MCP request is temporarily unavailable.",
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

    private static McpSchemaNegotiationResult LogAndReturn(
        McpSchemaNegotiationResult result,
        IFrontComposerMcpAgentContextAccessor accessor,
        IServiceProvider services) {
        if (result.Kind == McpSchemaNegotiationResultKind.Exact) {
            return result;
        }

        // C-2-new (chunk 2 re-review): prefer the request-scoped provider so per-request log
        // enrichers (correlation id, tenant id, etc.) attach to the structured-log entry.
        // Mirrors `TryResolveBaseline`'s scoping convention. Falls back to the captured
        // `services` only when no request scope is available (non-HTTP host, tests).
        IServiceProvider scope = accessor.RequestServices ?? services;
        ILogger<SchemaNegotiationRuntimeGate> logger =
            scope.GetService<ILogger<SchemaNegotiationRuntimeGate>>()
            ?? NullLogger<SchemaNegotiationRuntimeGate>.Instance;

        // 8-6a re-review: convert the enum to its name explicitly so structured-log sinks
        // produce a deterministic string instead of choosing between numeric and name based on
        // enricher configuration. The bounded D4 contract is `(category, messageKey, docsCode,
        // decisionKind)` — all string fields, all coarse, no fingerprint values, paths, tenant
        // identifiers, exception text, or runtime values.
        logger.LogInformation(
            "MCP schema negotiation decision {Category} {MessageKey} {DocsCode} {DecisionKind}.",
            result.AgentCategory,
            result.MessageKey,
            result.DocsCode,
            result.Kind.ToString());
        return result;
    }

    private static SchemaBaselineSnapshot? TryResolveBaseline(
        IServiceProvider services,
        IFrontComposerMcpAgentContextAccessor accessor,
        SchemaContractFamily family) {
        // 8-6a re-review: a request scope captured in `accessor.RequestServices` may already
        // be disposed by the time an async continuation runs. Treat ObjectDisposedException as
        // "no baseline available" rather than letting it bubble up and masquerade as a generic
        // DownstreamFailed in the outer catch.
        IServiceProvider providerScope = accessor.RequestServices ?? services;
        try {
            ISchemaBaselineProvider? provider = providerScope.GetService<ISchemaBaselineProvider>();
            return provider is not null
                && provider.TryResolve(family, PackageOwner, DefaultFixtureId, out SchemaBaselineSnapshot? snapshot)
                ? snapshot
                : null;
        }
        catch (ObjectDisposedException) {
            return null;
        }
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
        return Snapshot(document, SchemaContractFamily.ProjectionResource);
    }

    private static SchemaBaselineSnapshot CreateCommandSnapshot(McpCommandDescriptor descriptor) {
        // 8-6a re-review: legacy descriptors (older 8-6 emissions) may carry a null
        // DerivablePropertyNames; treat absence as "no derivable properties" rather than NREing.
        // Also align casing semantics with FrontComposerMcpCommandInvoker.SpoofedDerivableNames
        // (OrdinalIgnoreCase) so a descriptor declaring `TenantId` and a parameter named
        // `tenantid` produce a consistent canonical document across both layers.
        IReadOnlyCollection<string> derivable = descriptor.DerivablePropertyNames ?? Array.Empty<string>();
        SchemaContractDocument document = new(
            "frontcomposer.schema.contract.v1",
            SchemaContractFamily.CommandTool,
            descriptor.ProtocolName,
            "frontcomposer.command-tool.v1",
            descriptor.BoundedContext,
            descriptor.CommandTypeName,
            descriptor.ProtocolName,
            descriptor.Parameters
                .Where(parameter => !derivable.Contains(parameter.Name, StringComparer.OrdinalIgnoreCase))
                .Select(ToField)
                .ToArray(),
            [new SchemaCollectionContract("parameters", SchemaCollectionOrder.NonStructuralSorted, "name")],
            new Dictionary<string, string>());
        return Snapshot(document, SchemaContractFamily.CommandTool);
    }

    private static SchemaBaselineSnapshot Snapshot(
        SchemaContractDocument document,
        SchemaContractFamily family) {
        SchemaCanonicalPayload payload = CanonicalSchemaMaterial.CreatePayload(document);
        return new SchemaBaselineSnapshot(
            new SchemaBaselineProvenance(
                family,
                document.ContractSchemaVersion,
                payload.Fingerprint.AlgorithmId,
                PackageOwner,
                DefaultFixtureId,
                requiresMigrationGuide: false,
                payload.Fingerprint.CanonicalizerVersion,
                payload.Fingerprint.TestVectorId),
            payload.Document,
            payload.Fingerprint);
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
