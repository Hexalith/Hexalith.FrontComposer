using Hexalith.FrontComposer.Contracts.Schema;
using Hexalith.FrontComposer.Mcp.Schema;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Schema;

/// <summary>
/// AC3 / AC15 / T8 — table-driven precedence matrix. Each row stacks multiple causes
/// (hidden + stale + integrity + unsupported algo + unknown baseline + incompatible drift) and
/// asserts the earliest-precedence category wins deterministically. The leakage guards prove no
/// lower-priority message-key, docs-code, or agent-category bleeds into the response.
/// </summary>
public sealed class SchemaNegotiationPrecedenceMatrixTests {
    private static readonly SchemaFingerprint Server = new(
        SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1,
        new string('a', 64));

    private static readonly SchemaFingerprint Client = new(
        SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1,
        new string('b', 64));

    private static readonly SchemaFingerprint Unsupported = new("frontcomposer.schema.sha512.future", Server.Value);

    public static TheoryData<PrecedenceCase> Cases() {
        TheoryData<PrecedenceCase> data = [];

        // Row 1: hidden ALWAYS wins. (AC2 anchor.)
        data.Add(new PrecedenceCase(
            "row-1-hidden-over-everything",
            IsHidden: true, IsStale: true, IntegrityMismatch: true, BaselineTrusted: false,
            ClientFingerprint: null, ServerFingerprint: Unsupported, AdditiveDrift: false,
            Expected: McpSchemaNegotiationResultKind.HiddenOrUnknown,
            ExpectedAgentCategory: "unknown_resource",
            ExpectedMessageKey: "schema.hidden-or-unknown",
            ForbiddenAgentCategories: ["schema-mismatch", "schema-compatible-warning", "schema-unavailable", "unsupported-schema-fingerprint"],
            ForbiddenMessageKey: "schema.incompatible"));

        data.Add(new PrecedenceCase(
            "row-1b-hidden-over-schema-mismatch",
            IsHidden: true, IsStale: false, IntegrityMismatch: false, BaselineTrusted: true,
            ClientFingerprint: Client, ServerFingerprint: Server, AdditiveDrift: false,
            Expected: McpSchemaNegotiationResultKind.HiddenOrUnknown,
            ExpectedAgentCategory: "unknown_resource",
            ExpectedMessageKey: "schema.hidden-or-unknown",
            ForbiddenAgentCategories: ["schema-mismatch", "schema-compatible-warning", "schema-unavailable", "unsupported-schema-fingerprint"],
            ForbiddenMessageKey: "schema.incompatible"));

        // Row 2: stale wins over schema integrity.
        data.Add(new PrecedenceCase(
            "row-2-stale-over-integrity",
            IsHidden: false, IsStale: true, IntegrityMismatch: true, BaselineTrusted: true,
            ClientFingerprint: Client, ServerFingerprint: Server, AdditiveDrift: false,
            Expected: McpSchemaNegotiationResultKind.StaleDescriptor,
            ExpectedAgentCategory: "projection_unavailable",
            ExpectedMessageKey: "schema.stale-descriptor",
            ForbiddenAgentCategories: ["schema-mismatch", "unsupported-schema-fingerprint"],
            ForbiddenMessageKey: "schema.integrity-mismatch"));

        // Row 3: integrity wins over unsupported algorithm. (P-42 ordering.)
        data.Add(new PrecedenceCase(
            "row-3-integrity-over-algorithm",
            IsHidden: false, IsStale: false, IntegrityMismatch: true, BaselineTrusted: true,
            ClientFingerprint: Client, ServerFingerprint: Unsupported, AdditiveDrift: false,
            Expected: McpSchemaNegotiationResultKind.SchemaIntegrityMismatch,
            ExpectedAgentCategory: "schema-unavailable",
            ExpectedMessageKey: "schema.integrity-mismatch",
            ForbiddenAgentCategories: ["unsupported-schema-fingerprint", "schema-mismatch"],
            ForbiddenMessageKey: "schema.algorithm.unsupported"));

        // Row 4: unsupported algorithm wins over missing baseline.
        data.Add(new PrecedenceCase(
            "row-4-algorithm-over-baseline",
            IsHidden: false, IsStale: false, IntegrityMismatch: false, BaselineTrusted: false,
            ClientFingerprint: Client, ServerFingerprint: Unsupported, AdditiveDrift: false,
            Expected: McpSchemaNegotiationResultKind.UnsupportedAlgorithm,
            ExpectedAgentCategory: "unsupported-schema-fingerprint",
            ExpectedMessageKey: "schema.algorithm.unsupported",
            ForbiddenAgentCategories: ["schema-mismatch", "schema-unavailable", "unknown-version"],
            ForbiddenMessageKey: "schema.baseline.unknown"));

        data.Add(new PrecedenceCase(
            "row-4b-server-algorithm-over-missing-client",
            IsHidden: false, IsStale: false, IntegrityMismatch: false, BaselineTrusted: true,
            ClientFingerprint: null, ServerFingerprint: Unsupported, AdditiveDrift: false,
            Expected: McpSchemaNegotiationResultKind.UnsupportedAlgorithm,
            ExpectedAgentCategory: "unsupported-schema-fingerprint",
            ExpectedMessageKey: "schema.algorithm.unsupported",
            ForbiddenAgentCategories: ["unknown-version"],
            ForbiddenMessageKey: "schema.client-version.unknown"));

        data.Add(new PrecedenceCase(
            "row-4c-client-algorithm-over-baseline",
            IsHidden: false, IsStale: false, IntegrityMismatch: false, BaselineTrusted: false,
            ClientFingerprint: Unsupported, ServerFingerprint: Server, AdditiveDrift: false,
            Expected: McpSchemaNegotiationResultKind.UnsupportedAlgorithm,
            ExpectedAgentCategory: "unsupported-schema-fingerprint",
            ExpectedMessageKey: "schema.algorithm.unsupported",
            ForbiddenAgentCategories: ["schema-exact", "schema-unavailable", "unknown-version"],
            ForbiddenMessageKey: "schema.baseline.unknown"));

        // Row 5: missing baseline wins over incompatible drift when client/server differ.
        data.Add(new PrecedenceCase(
            "row-5-baseline-over-drift",
            IsHidden: false, IsStale: false, IntegrityMismatch: false, BaselineTrusted: false,
            ClientFingerprint: Client, ServerFingerprint: Server, AdditiveDrift: false,
            Expected: McpSchemaNegotiationResultKind.UnknownServerBaseline,
            ExpectedAgentCategory: "schema-unavailable",
            ExpectedMessageKey: "schema.baseline.unknown",
            ForbiddenAgentCategories: ["schema-mismatch", "schema-compatible-warning"],
            ForbiddenMessageKey: "schema.incompatible"));

        // Row 6: incompatible drift wins over additive when snapshots show breaking deltas.
        data.Add(new PrecedenceCase(
            "row-6-incompatible-over-additive",
            IsHidden: false, IsStale: false, IntegrityMismatch: false, BaselineTrusted: true,
            ClientFingerprint: Client, ServerFingerprint: Server, AdditiveDrift: false,
            Expected: McpSchemaNegotiationResultKind.Incompatible,
            ExpectedAgentCategory: "schema-mismatch",
            ExpectedMessageKey: "schema.incompatible",
            ForbiddenAgentCategories: ["schema-compatible-warning"],
            ForbiddenMessageKey: "schema.compatible-additive"));

        // Row 7: additive wins when only optional drift is present.
        data.Add(new PrecedenceCase(
            "row-7-additive-allows-side-effects",
            IsHidden: false, IsStale: false, IntegrityMismatch: false, BaselineTrusted: true,
            ClientFingerprint: Client, ServerFingerprint: Server, AdditiveDrift: true,
            Expected: McpSchemaNegotiationResultKind.CompatibleAdditive,
            ExpectedAgentCategory: "schema-compatible-warning",
            ExpectedMessageKey: "schema.compatible-additive",
            ForbiddenAgentCategories: ["schema-mismatch", "unsupported-schema-fingerprint"],
            ForbiddenMessageKey: "schema.incompatible",
            AllowsSideEffects: true));

        // Row 8: exact byte-match short-circuits even without trusted baseline (P-40).
        data.Add(new PrecedenceCase(
            "row-8-exact-shortcircuits-baseline",
            IsHidden: false, IsStale: false, IntegrityMismatch: false, BaselineTrusted: false,
            ClientFingerprint: Server, ServerFingerprint: Server, AdditiveDrift: false,
            Expected: McpSchemaNegotiationResultKind.Exact,
            ExpectedAgentCategory: "schema-exact",
            ExpectedMessageKey: "schema.exact",
            ForbiddenAgentCategories: ["schema-unavailable", "schema-mismatch"],
            ForbiddenMessageKey: "schema.baseline.unknown",
            AllowsSideEffects: true));

        // Row 9: missing client fingerprint after algorithm gate yields UnknownClientVersion
        // — distinct from algorithm/baseline rows so agents see actionable remediation.
        data.Add(new PrecedenceCase(
            "row-9-missing-client-version",
            IsHidden: false, IsStale: false, IntegrityMismatch: false, BaselineTrusted: true,
            ClientFingerprint: null, ServerFingerprint: Server, AdditiveDrift: false,
            Expected: McpSchemaNegotiationResultKind.UnknownClientVersion,
            ExpectedAgentCategory: "unknown-version",
            ExpectedMessageKey: "schema.client-version.unknown",
            ForbiddenAgentCategories: ["schema-mismatch", "unsupported-schema-fingerprint"],
            ForbiddenMessageKey: "schema.incompatible"));

        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Negotiate_PrecedenceMatrix_RowYieldsExpectedClassification(PrecedenceCase row) {
        ArgumentNullException.ThrowIfNull(row);
        McpSchemaNegotiationResult result = McpSchemaNegotiator.Negotiate(BuildInput(row));

        result.Kind.ShouldBe(row.Expected, $"row {row.Name}");
        result.AgentCategory.ShouldBe(row.ExpectedAgentCategory, $"row {row.Name}");
        result.MessageKey.ShouldBe(row.ExpectedMessageKey, $"row {row.Name}");
        result.AllowsSideEffects.ShouldBe(row.AllowsSideEffects, $"row {row.Name}");
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void LeakageGuards_LowerPriorityFieldsDoNotBleedIntoResult(PrecedenceCase row) {
        ArgumentNullException.ThrowIfNull(row);
        McpSchemaNegotiationResult result = McpSchemaNegotiator.Negotiate(BuildInput(row));

        foreach (string forbidden in row.ForbiddenAgentCategories) {
            result.AgentCategory.ShouldNotBe(forbidden, $"row {row.Name} leaked lower-priority agent category");
        }

        result.MessageKey.ShouldNotBe(row.ForbiddenMessageKey, $"row {row.Name} leaked lower-priority message key");
        result.DocsCode.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void LeakageGuards_NoFingerprintHashEverAppearsInPublicFields() {
        // AC15: no raw client fingerprint hashes leak through agent-visible fields.
        McpSchemaNegotiationResult result = McpSchemaNegotiator.Negotiate(BuildInput(new PrecedenceCase(
            "leak-guard-fingerprint",
            IsHidden: false, IsStale: false, IntegrityMismatch: false, BaselineTrusted: true,
            ClientFingerprint: Client, ServerFingerprint: Server, AdditiveDrift: false,
            Expected: McpSchemaNegotiationResultKind.Incompatible,
            ExpectedAgentCategory: "schema-mismatch",
            ExpectedMessageKey: "schema.incompatible",
            ForbiddenAgentCategories: [],
            ForbiddenMessageKey: "")));

        result.AgentCategory.ShouldNotContain(Server.Value);
        result.AgentCategory.ShouldNotContain(Client.Value);
        result.MessageKey.ShouldNotContain(Server.Value);
        result.MessageKey.ShouldNotContain(Client.Value);
        result.DocsCode.ShouldNotContain(Server.Value);
        result.DocsCode.ShouldNotContain(Client.Value);
    }

    [Fact]
    public void LeakageGuards_DocsCodeIsBoundedShortStableIdentifier() {
        // AC15 / Story 8-4a: docs codes are bounded short identifiers, not free-form messages.
        McpSchemaNegotiationResult result = McpSchemaNegotiator.Negotiate(BuildInput(new PrecedenceCase(
            "leak-guard-docs-code",
            IsHidden: false, IsStale: true, IntegrityMismatch: false, BaselineTrusted: true,
            ClientFingerprint: Client, ServerFingerprint: Server, AdditiveDrift: false,
            Expected: McpSchemaNegotiationResultKind.StaleDescriptor,
            ExpectedAgentCategory: "projection_unavailable",
            ExpectedMessageKey: "schema.stale-descriptor",
            ForbiddenAgentCategories: [],
            ForbiddenMessageKey: "")));

        result.DocsCode.Length.ShouldBeLessThanOrEqualTo(64);
        // Docs codes are stable HFC-prefixed identifiers per Story 8-4a taxonomy.
        result.DocsCode.ShouldStartWith("HFC-");
    }

    private static McpSchemaNegotiationInput BuildInput(PrecedenceCase row) {
        // 8-6a review M1: when the row's intent is "additive drift admits side effects", supply
        // baseline + server snapshots that the analyzer classifies as AdditiveCompatible. The
        // legacy `HasCompatibleAdditiveDrift` bool is now ignored per AC6.
        SchemaBaselineSnapshot? baseline = null;
        SchemaBaselineSnapshot? serverSnapshot = null;
        if (row.AdditiveDrift) {
            baseline = BuildSnapshot(row.ClientFingerprint.ShouldNotBeNull(), [
                new SchemaFieldContract("Number", "Int32", "integer", true, false),
            ]);
            serverSnapshot = BuildSnapshot(row.ServerFingerprint, [
                new SchemaFieldContract("Number", "Int32", "integer", true, false),
                new SchemaFieldContract("Note", "String", "string", false, true),
            ]);
        }

        return new McpSchemaNegotiationInput(
            IsHiddenOrUnknown: row.IsHidden,
            IsStaleDescriptor: row.IsStale,
            ClientFingerprint: row.ClientFingerprint,
            ServerFingerprint: row.ServerFingerprint,
            HasTrustedBaseline: row.BaselineTrusted,
            HasCompatibleAdditiveDrift: row.AdditiveDrift,
            HasSchemaIntegrityMismatch: row.IntegrityMismatch,
            Baseline: baseline,
            Server: serverSnapshot);
    }

    private static SchemaBaselineSnapshot BuildSnapshot(SchemaFingerprint fingerprint, IReadOnlyList<SchemaFieldContract> fields)
        => new(
            new SchemaBaselineProvenance(
                SchemaContractFamily.ProjectionResource,
                "frontcomposer.projection-resource.v1",
                fingerprint.AlgorithmId,
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
            fingerprint);

    public sealed record PrecedenceCase(
        string Name,
        bool IsHidden,
        bool IsStale,
        bool IntegrityMismatch,
        bool BaselineTrusted,
        SchemaFingerprint? ClientFingerprint,
        SchemaFingerprint ServerFingerprint,
        bool AdditiveDrift,
        McpSchemaNegotiationResultKind Expected,
        string ExpectedAgentCategory,
        string ExpectedMessageKey,
        IReadOnlyList<string> ForbiddenAgentCategories,
        string ForbiddenMessageKey,
        bool AllowsSideEffects = false) {
        public override string ToString() => Name;
    }
}
