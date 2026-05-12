using System.Globalization;
using System.Security.Claims;

using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Contracts.Schema;
using Hexalith.FrontComposer.Mcp.Invocation;
using Hexalith.FrontComposer.Mcp.Schema;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;
using Xunit;

namespace Hexalith.FrontComposer.Mcp.Tests.Schema;

/// <summary>
/// Story 11.5 review-resolution suite. Closes the remaining DN items from the 2026-05-12
/// adversarial review by adding focused tests that pin behavior the existing schema/admission/auth
/// coverage either implies or only proves pairwise. Each fact is anchored to a DN number in its
/// XML comment so a future reviewer can map the test back to the original review finding.
/// </summary>
public sealed class Story11_5ResolutionTests {
    private const string SentinelTenant = "SENTINEL-TENANT-9d4f7a";
    private const string SentinelUser = "SENTINEL-USER-c2e1b8";
    private const string SentinelToken = "SENTINEL-TOKEN-f08a3e";
    private const string SentinelPath = "SENTINEL-PATH-C:/secrets/key.pem";
    private const string SentinelDescriptor = "SENTINEL-DESCRIPTOR-hidden-billing-tool";
    private const string SentinelHidden = "SENTINEL-HIDDEN-NAME-71fa";
    private const string SentinelMachine = "SENTINEL-MACHINE-jpiquot-dev";
    private const string SafeRequestedToolName = "Billing.PayInvoiceCommand.Execute";

    private static readonly SchemaFingerprint ServerFingerprint = new(
        SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1,
        new string('a', 64));

    private static readonly SchemaFingerprint ClientFingerprint = new(
        SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1,
        new string('b', 64));

    /// <summary>
    /// DN8 / AC22 — sentinel redaction. Inject distinct sentinel strings shaped like the
    /// taxonomy categories AC22 enumerates (tenant, user, token, path, raw descriptor, hidden
    /// name, machine-local source). The negotiator only sees fingerprint values and structural
    /// inputs, so the sentinels must never round-trip into the agent-facing public fields. A
    /// regression that interpolated a raw fingerprint value or descriptor name into
    /// AgentCategory / MessageKey / DocsCode would surface here.
    /// </summary>
    [Fact]
    public void DN8_SentinelRedaction_NegotiationResultIsAlwaysFreeOfSentinels() {
        string[] sentinels = [
            SentinelTenant,
            SentinelUser,
            SentinelToken,
            SentinelPath,
            SentinelDescriptor,
            SentinelHidden,
            SentinelMachine,
        ];

        // The negotiator consumes fingerprint structure rather than tenant/user/token/path
        // fields. Inject the sentinels through the raw string positions that can reach this
        // surface (algorithm/value), and cover every public negotiation branch this test claims.
        McpSchemaNegotiationResult[] results = [
#pragma warning disable CS0618, HFC4001
            McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
                IsHiddenOrUnknown: true,
                IsStaleDescriptor: false,
                FingerprintWithSentinel(SentinelTenant),
                FingerprintWithSentinel(SentinelUser),
                HasTrustedBaseline: true,
                HasCompatibleAdditiveDrift: false,
                HasSchemaIntegrityMismatch: false)),
            McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
                IsHiddenOrUnknown: false,
                IsStaleDescriptor: true,
                FingerprintWithSentinel(SentinelPath),
                ServerFingerprint,
                HasTrustedBaseline: true,
                HasCompatibleAdditiveDrift: false,
                HasSchemaIntegrityMismatch: false)),
            McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
                IsHiddenOrUnknown: false,
                IsStaleDescriptor: false,
                FingerprintWithSentinel(SentinelDescriptor),
                FingerprintWithSentinel(SentinelMachine),
                HasTrustedBaseline: true,
                HasCompatibleAdditiveDrift: false,
                HasSchemaIntegrityMismatch: true)),
            McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
                IsHiddenOrUnknown: false,
                IsStaleDescriptor: false,
                new SchemaFingerprint("frontcomposer.schema." + SentinelHidden, FingerprintValue(SentinelToken)),
                ServerFingerprint,
                HasTrustedBaseline: true,
                HasCompatibleAdditiveDrift: false,
                HasSchemaIntegrityMismatch: false)),
            McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
                IsHiddenOrUnknown: false,
                IsStaleDescriptor: false,
                FingerprintWithSentinel(SentinelTenant),
                ServerFingerprint,
                HasTrustedBaseline: false,
                HasCompatibleAdditiveDrift: false,
                HasSchemaIntegrityMismatch: false)),
            McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
                IsHiddenOrUnknown: false,
                IsStaleDescriptor: false,
                ClientFingerprint,
                ServerFingerprint,
                HasTrustedBaseline: true,
                HasCompatibleAdditiveDrift: false,
                HasSchemaIntegrityMismatch: false)),
            McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
                IsHiddenOrUnknown: false,
                IsStaleDescriptor: false,
                ClientFingerprint,
                ServerFingerprint,
                HasTrustedBaseline: true,
                HasCompatibleAdditiveDrift: false,
                HasSchemaIntegrityMismatch: false,
                Baseline: SnapshotWithFields("Number"),
                Server: SnapshotWithFields("Number", "Comment"))),
            McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
                IsHiddenOrUnknown: false,
                IsStaleDescriptor: false,
                ClientFingerprint,
                ServerFingerprint,
                HasTrustedBaseline: true,
                HasCompatibleAdditiveDrift: false,
                HasSchemaIntegrityMismatch: false)),
#pragma warning restore CS0618, HFC4001
        ];

        foreach (McpSchemaNegotiationResult result in results) {
            foreach (string sentinel in sentinels) {
                result.AgentCategory.ShouldNotContain(sentinel);
                result.MessageKey.ShouldNotContain(sentinel);
                result.DocsCode.ShouldNotContain(sentinel);
            }
        }
    }

    /// <summary>
    /// DN8 / AC22 — additionally prove that the tool-rejection envelope on schema rejection
    /// keeps the descriptor opaque even when the resolved tool's name itself is shaped like a
    /// hidden descriptor sentinel. The public <see cref="McpToolResolutionResult.Tool"/> must be
    /// null on rejection (per DN18 patch), and <see cref="McpToolResolutionResult.InternalCorrelationKey"/>
    /// must be a 16-character hex prefix that does not equal or contain the descriptor name.
    /// </summary>
    [Fact]
    public void DN8_SentinelRedaction_RejectedToolStripsDescriptorBeforeWire() {
        McpCommandDescriptor descriptor = new(
            ProtocolName: SentinelDescriptor,
            CommandTypeName: typeof(object).FullName!,
            BoundedContext: "Sales",
            Title: "internal",
            Description: null,
            AuthorizationPolicyName: null,
            Parameters: [],
            DerivablePropertyNames: [],
            Fingerprint: null);
        McpVisibleToolCatalogEntry entry = new(
            Name: SentinelDescriptor,
            Title: "internal",
            Description: null,
            BoundedContext: "Sales",
            InputSummary: "internal",
            Descriptor: descriptor,
            NormalizedName: SentinelDescriptor.ToLowerInvariant());
        McpToolVisibilityContext context = new(
            TenantId: "tenant-redacted",
            UserId: "agent-redacted",
            Principal: new ClaimsPrincipal(new ClaimsIdentity()));
        McpVisibleToolCatalog catalog = new(context, [], false);

        McpToolResolutionResult rejection = McpToolResolutionResult.Reject(
            requestedName: SafeRequestedToolName,
            category: FrontComposerMcpFailureCategory.SchemaMismatch,
            catalog: catalog,
            tool: entry);

        // The descriptor itself is stripped — public callers cannot read back the resolved tool.
        rejection.Tool.ShouldBeNull();
        // The correlation key is an opaque 16-char hex prefix; not reversible to the descriptor.
        rejection.InternalCorrelationKey.ShouldNotBeNull();
        rejection.InternalCorrelationKey!.Length.ShouldBe(16);
        rejection.InternalCorrelationKey.ShouldNotContain(SentinelDescriptor);
        rejection.InternalCorrelationKey.ShouldNotContain(SentinelHidden);
        // The requested name is caller echo, not resolved descriptor metadata. Keep the public
        // echo non-sensitive so this test proves hidden descriptor stripping rather than proving
        // that descriptor-shaped text can leave through RequestedName.
        rejection.RequestedName.ShouldBe(SafeRequestedToolName);
        rejection.RequestedName.ShouldNotContain(SentinelDescriptor);
    }

    /// <summary>
    /// DN10 / AC28 — four-way fingerprint-source conflict. Header hint, descriptor claimed
    /// fingerprint, runtime manifest aggregate, and corpus aggregate disagree. The runtime
    /// admission path must fail closed without silently downgrading to the weakest compatibility
    /// outcome.
    /// </summary>
    [Fact]
    public async Task DN10_FourWayFingerprintConflict_FailsClosed_WithoutDowngrade() {
        // First prove the request-time admission path fails closed when the client header hint
        // disagrees with the descriptor's claimed fingerprint while a corpus provider is present.
        CountingCorpusFingerprintProvider provider = new([new SchemaFingerprint(
            SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1,
            new string('f', 64))]);
        FrontComposerMcpToolAdmissionService admission = BuildAdmissionWithCorpus(
            clientFingerprintHint: new SchemaFingerprint(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, new string('c', 64)),
            descriptorFingerprint: new SchemaFingerprint(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, new string('d', 64)),
            provider);

        McpToolResolutionResult result = await admission.ResolveAsync(
            SafeRequestedToolName,
            TestContext.Current.CancellationToken);

        result.Accepted.ShouldBeFalse();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.SchemaMismatch);
        provider.CallCount.ShouldBe(1, "AC28: corpus fingerprint material must be loaded into the same registry used by admission.");

        // Then pin the registry-construction integrity side: a manifest whose claimed aggregate
        // disagrees with the runtime-recomputed descriptor material fails closed before a softer
        // schema-negotiation category can be returned.
        SchemaFingerprint resourceFingerprint = new(
            SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1,
            new string('d', 64));
        SchemaFingerprint forgedManifestFingerprint = new(
            SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1,
            new string('e', 64));
        SchemaFingerprint corpusFingerprint = new(
            SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1,
            new string('f', 64));

        McpManifest conflict = new(
            "frontcomposer.mcp.v1",
            [],
            [new McpResourceDescriptor(
                "frontcomposer://Sales/projections/Order",
                "Order",
                "Hexalith.FrontComposer.Sample.OrderProjection",
                "Sales",
                "Orders",
                null,
                [new McpParameterDescriptor("Number", "String", "string", true, false, "Number", null, [], false)],
                Fingerprint: resourceFingerprint)],
            Fingerprint: forgedManifestFingerprint);

        ServiceCollection services = [];
        services.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(conflict));
        services.AddSingleton<ISkillCorpusFingerprintProvider>(new StaticCorpusFingerprintProvider([corpusFingerprint]));
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<FrontComposerMcpDescriptorRegistry>();

        FrontComposerMcpException ex = Should.Throw<FrontComposerMcpException>(
            () => services.BuildServiceProvider().GetRequiredService<FrontComposerMcpDescriptorRegistry>());

        ex.Category.ShouldBe(
            FrontComposerMcpFailureCategory.SchemaIntegrityMismatch,
            "DN10 / AC28: a four-way fingerprint disagreement must fail closed as SchemaIntegrityMismatch — never downgrade to UnknownServerBaseline or CompatibleWarning.");
    }

    /// <summary>
    /// DN11 / AC29 — descriptor-lookup memoized determinism. The descriptor registry is built
    /// once and never re-reads source descriptors per request, so repeated <c>TryGetCommand</c>
    /// calls for a hidden/unknown name return the identical bounded result without re-parsing
    /// or re-allocating from raw input. This pins the parser-equivalent memoization contract
    /// for descriptor resolution.
    /// </summary>
    [Fact]
    public void DN11_DescriptorLookup_DeterministicAcrossRetries() {
        ServiceCollection services = [];
        services.Configure<FrontComposerMcpOptions>(_ => { });
        services.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        FrontComposerMcpDescriptorRegistry registry = services.BuildServiceProvider().GetRequiredService<FrontComposerMcpDescriptorRegistry>();

        bool firstFound = registry.TryGetCommand(SentinelDescriptor, out McpCommandDescriptor _);
        bool secondFound = registry.TryGetCommand(SentinelDescriptor, out McpCommandDescriptor _);
        bool thirdFound = registry.TryGetCommand(SentinelDescriptor.ToUpperInvariant(), out McpCommandDescriptor _);

        firstFound.ShouldBeFalse();
        secondFound.ShouldBe(firstFound, "DN11 / AC29: repeated descriptor lookups must yield the same bounded outcome on retry.");
        thirdFound.ShouldBe(firstFound, "DN11 / AC29: OrdinalIgnoreCase lookup behavior must remain stable across casing variants.");

        // Epoch is constant (no hot-reload in v1); proves the snapshot identity callers depend on.
        McpDescriptorEpochs epochsA = registry.GetEpochs();
        McpDescriptorEpochs epochsB = registry.GetEpochs();
        epochsA.ShouldBe(epochsB);
    }

    /// <summary>
    /// DN11 / AC29 — fingerprint comparison memoized determinism. The pure-function negotiator
    /// returns identical bounded results for identical input even after the bound failure is
    /// observed. Sentinel header hints injected as raw strings into fingerprint values cannot
    /// produce divergent public categories on retry.
    /// </summary>
    [Fact]
    public void DN11_FingerprintNegotiation_DeterministicAcrossRetries() {
#pragma warning disable CS0618, HFC4001
        McpSchemaNegotiationInput input = new(
            IsHiddenOrUnknown: false,
            IsStaleDescriptor: false,
            new SchemaFingerprint("frontcomposer.schema.attacker.v1", SentinelToken.PadRight(64, 'x').Substring(0, 64)),
            ServerFingerprint,
            HasTrustedBaseline: true,
            HasCompatibleAdditiveDrift: false,
            HasSchemaIntegrityMismatch: false);
#pragma warning restore CS0618, HFC4001

        McpSchemaNegotiationResult first = McpSchemaNegotiator.Negotiate(input);
        McpSchemaNegotiationResult second = McpSchemaNegotiator.Negotiate(input);
        McpSchemaNegotiationResult third = McpSchemaNegotiator.Negotiate(input);

        first.AgentCategory.ShouldBe("unsupported-schema-fingerprint");
        second.AgentCategory.ShouldBe(first.AgentCategory);
        third.AgentCategory.ShouldBe(first.AgentCategory);
        first.MessageKey.ShouldBe(second.MessageKey);
        first.DocsCode.ShouldBe(second.DocsCode);
        first.AgentCategory.ShouldNotContain(SentinelToken);
    }

    /// <summary>
    /// DN12 / AC31 — machine contract values are ordinal/invariant. Switching
    /// <see cref="CultureInfo.CurrentCulture"/> to a culture with case-folding surprises (tr-TR
    /// dotted/dotless I) must not change <c>MessageKey</c>, <c>AgentCategory</c>, or
    /// <c>DocsCode</c>. The negotiator is pure-functional so this also proves no ambient
    /// <c>ToString()</c> drift on enum values leaks into the public contract.
    /// </summary>
    [Theory]
    [InlineData("tr-TR")]
    [InlineData("de-DE")]
    [InlineData("ja-JP")]
    public void DN12_AgentContract_RemainsOrdinalAcrossNonInvariantCultures(string cultureName) {
#pragma warning disable CS0618, HFC4001
        McpSchemaNegotiationInput input = new(
            IsHiddenOrUnknown: false,
            IsStaleDescriptor: false,
            ClientFingerprint,
            ServerFingerprint,
            HasTrustedBaseline: true,
            HasCompatibleAdditiveDrift: false,
            HasSchemaIntegrityMismatch: false);
#pragma warning restore CS0618, HFC4001

        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo previousUi = CultureInfo.CurrentUICulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            McpSchemaNegotiationResult[] invariantBaseline = CultureCases();

            CultureInfo.CurrentCulture = new CultureInfo(cultureName);
            CultureInfo.CurrentUICulture = new CultureInfo(cultureName);
            McpSchemaNegotiationResult[] underCulture = CultureCases();

            underCulture.Length.ShouldBe(invariantBaseline.Length);
            for (int i = 0; i < underCulture.Length; i++) {
                underCulture[i].AgentCategory.ShouldBe(invariantBaseline[i].AgentCategory);
                underCulture[i].MessageKey.ShouldBe(invariantBaseline[i].MessageKey);
                underCulture[i].DocsCode.ShouldBe(invariantBaseline[i].DocsCode);
                underCulture[i].Kind.ShouldBe(invariantBaseline[i].Kind);
                underCulture[i].FailureCategory.ShouldBe(invariantBaseline[i].FailureCategory);

                // Ordinal property: the keys/categories use lowercase ASCII separators; a culture
                // shift must never produce mixed-script or uppercased output that an agent would
                // parse as a different category.
                underCulture[i].AgentCategory.ShouldBe(underCulture[i].AgentCategory.ToLowerInvariant());
                underCulture[i].MessageKey.ShouldBe(underCulture[i].MessageKey.ToLowerInvariant());
            }
        }
        finally {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    /// <summary>
    /// DN13 / AC26 — enum display labels do not participate in the agent contract values. The
    /// <see cref="McpSchemaNegotiationResultKind"/> enum names (e.g., "CompatibleAdditive") are
    /// implementation labels. AC26 requires proof that those labels are not used as machine
    /// contract input. The agent contract values (<c>AgentCategory</c>, <c>MessageKey</c>,
    /// <c>DocsCode</c>) are kebab/dot-cased identifiers and must not equal or contain the
    /// enum's <c>ToString()</c> form on any classification branch.
    /// </summary>
    [Fact]
    public void DN13_EnumDisplayLabels_DoNotLeakIntoAgentContractValues() {
        string[] enumNames = [.. Enum.GetNames(typeof(McpSchemaNegotiationResultKind))];

        McpSchemaNegotiationResult[] results = [
#pragma warning disable CS0618, HFC4001
            McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
                IsHiddenOrUnknown: true, IsStaleDescriptor: false,
                ClientFingerprint, ServerFingerprint,
                HasTrustedBaseline: true, HasCompatibleAdditiveDrift: false, HasSchemaIntegrityMismatch: false)),
            McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
                IsHiddenOrUnknown: false, IsStaleDescriptor: true,
                ClientFingerprint, ServerFingerprint,
                HasTrustedBaseline: true, HasCompatibleAdditiveDrift: false, HasSchemaIntegrityMismatch: false)),
            McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
                IsHiddenOrUnknown: false, IsStaleDescriptor: false,
                ClientFingerprint, ServerFingerprint,
                HasTrustedBaseline: true, HasCompatibleAdditiveDrift: false, HasSchemaIntegrityMismatch: true)),
            McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(
                IsHiddenOrUnknown: false, IsStaleDescriptor: false,
                ClientFingerprint, ServerFingerprint,
                HasTrustedBaseline: true, HasCompatibleAdditiveDrift: false, HasSchemaIntegrityMismatch: false)),
#pragma warning restore CS0618, HFC4001
        ];

        foreach (McpSchemaNegotiationResult result in results) {
            // Wire values are explicit compatibility mappings. Some public keys intentionally
            // share domain words with enum labels (for example schema.hidden-or-unknown), so the
            // important regression guard is that no raw PascalCase enum name or docs/category
            // value is generated from ToString().
            ExpectedWireValues(result.Kind).ShouldBe((result.AgentCategory, result.MessageKey, result.DocsCode));
            foreach (string enumName in enumNames) {
                foreach (string variant in EnumLabelVariants(enumName)) {
                    string.Equals(result.AgentCategory, variant, StringComparison.OrdinalIgnoreCase).ShouldBeFalse(
                        $"AC26: agent category must not be generated directly from enum-label variant '{variant}'.");
                }

                result.MessageKey.ShouldNotContain(enumName,
                    Case.Sensitive,
                    $"AC26: message key must not embed the raw enum name '{enumName}'.");
                result.DocsCode.ShouldNotContain(enumName,
                    Case.Sensitive,
                    $"AC26: docs code must not embed the raw enum name '{enumName}'.");
            }
        }
    }

    /// <summary>
    /// DN13 / AC26 — corpus aggregate fingerprint material does not depend on enum display
    /// labels either. Compute an aggregate with two manifests and prove that toggling the
    /// <see cref="McpSchemaNegotiationResultKind"/> enum names through a synthetic ToString
    /// shift would not influence the aggregate. Because <c>SchemaFingerprint</c> hashes only
    /// algorithm + value strings (not enum names), this is structurally guaranteed; the test
    /// pins it as a regression guard.
    /// </summary>
    [Fact]
    public void DN13_AggregateFingerprint_DoesNotDependOnEnumLabels() {
        SchemaFingerprint nestedA = new(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, new string('a', 64));
        SchemaFingerprint nestedB = new(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, new string('b', 64));

        McpManifest manifest = new(
            "frontcomposer.mcp.v1",
            [new McpCommandDescriptor(
                ProtocolName: "Billing.Pay.Execute",
                CommandTypeName: "Billing.PayInvoiceCommand",
                BoundedContext: "Billing",
                Title: "Pay invoice",
                Description: null,
                AuthorizationPolicyName: null,
                Parameters: [],
                DerivablePropertyNames: [],
                Fingerprint: nestedA)],
            []);

        SchemaContractDocument aggregateDocument = AggregateDocument([nestedA, nestedB]);
        SchemaCanonicalPayload canonical = CanonicalSchemaMaterial.CreatePayload(aggregateDocument);
        SchemaFingerprint firstPass = FrontComposerMcpRuntimeManifestAggregator.Compute([manifest], [nestedB]);
        SchemaFingerprint secondPass = FrontComposerMcpRuntimeManifestAggregator.Compute([manifest], [nestedB]);

        // Algorithm + value identity is the contract. No enum label or culture-sensitive
        // component participates in this material. Check the canonical JSON before hashing;
        // the final hash would be opaque even if a bad input accidentally included an enum label.
        secondPass.AlgorithmId.ShouldBe(firstPass.AlgorithmId);
        secondPass.Value.ShouldBe(firstPass.Value);
        canonical.Json.ShouldNotContain(nameof(McpSchemaNegotiationResultKind.CompatibleAdditive));
        canonical.Json.ShouldNotContain(nameof(McpSchemaNegotiationResultKind.Incompatible));
        canonical.Fingerprint.Value.ShouldBe(firstPass.Value);
    }

    private static FrontComposerMcpToolAdmissionService BuildAdmissionWithCorpus(
        SchemaFingerprint clientFingerprintHint,
        SchemaFingerprint descriptorFingerprint,
        CountingCorpusFingerprintProvider provider) {
        ServiceCollection services = [];
        services.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(new McpManifest(
            "frontcomposer.mcp.v1",
            [new McpCommandDescriptor(
                SafeRequestedToolName,
                "Hexalith.FrontComposer.Sample.Commands.PayInvoiceCommand",
                "Billing",
                "Pay invoice",
                "Pay invoice",
                null,
                [new McpParameterDescriptor("Amount", "Int32", "number", true, false, "Amount", null, [], false)],
                ["TenantId", "UserId", "MessageId"],
                Fingerprint: descriptorFingerprint)],
            [])));
        services.AddSingleton<ISkillCorpusFingerprintProvider>(provider);
        services.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new SchemaAwareStaticAccessor(clientFingerprintHint));
        services.AddSingleton<FrontComposerMcpToolAdmissionService>();

        return services.BuildServiceProvider().GetRequiredService<FrontComposerMcpToolAdmissionService>();
    }

    private static SchemaFingerprint FingerprintWithSentinel(string sentinel)
        => new(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, FingerprintValue(sentinel));

    private static string FingerprintValue(string seed)
        => (seed + new string('x', 64)).Substring(0, 64);

    private static SchemaBaselineSnapshot SnapshotWithFields(params string[] fieldNames) {
        SchemaContractDocument document = new(
            "frontcomposer.schema.contract.v1",
            SchemaContractFamily.CommandTool,
            "Billing.PayInvoiceCommand.Execute",
            "frontcomposer.command-tool.v1",
            "Billing",
            "Hexalith.FrontComposer.Sample.Commands.PayInvoiceCommand",
            "Billing.PayInvoiceCommand.Execute",
            [.. fieldNames.Select(name => new SchemaFieldContract(name, "String", "string", true, false))],
            [new SchemaCollectionContract("parameters", SchemaCollectionOrder.NonStructuralSorted, "name")],
            new Dictionary<string, string>());
        SchemaCanonicalPayload payload = CanonicalSchemaMaterial.CreatePayload(document);
        return new SchemaBaselineSnapshot(
            new SchemaBaselineProvenance(
                SchemaContractFamily.CommandTool,
                document.ContractSchemaVersion,
                payload.Fingerprint.AlgorithmId,
                "Hexalith.FrontComposer",
                "baseline-known-v1",
                false,
                payload.Fingerprint.CanonicalizerVersion,
                payload.Fingerprint.TestVectorId),
            payload.Document,
            payload.Fingerprint);
    }

    private static McpSchemaNegotiationResult[] CultureCases()
        => [
#pragma warning disable CS0618, HFC4001
            McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(true, false, ClientFingerprint, ServerFingerprint, true, false, false)),
            McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(false, true, ClientFingerprint, ServerFingerprint, true, false, false)),
            McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(false, false, ClientFingerprint, ServerFingerprint, true, false, true)),
            McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(false, false, new SchemaFingerprint("frontcomposer.schema.future", new string('c', 64)), ServerFingerprint, true, false, false)),
            McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(false, false, ClientFingerprint, null, true, false, false)),
            McpSchemaNegotiator.Negotiate(new McpSchemaNegotiationInput(false, false, ClientFingerprint, ServerFingerprint, true, false, false)),
#pragma warning restore CS0618, HFC4001
        ];

    private static string[] EnumLabelVariants(string enumName)
        => [
            enumName,
            enumName.ToLowerInvariant(),
            ToKebabCase(enumName),
            ToDotCase(enumName),
        ];

    private static (string AgentCategory, string MessageKey, string DocsCode) ExpectedWireValues(McpSchemaNegotiationResultKind kind)
        => kind switch {
            McpSchemaNegotiationResultKind.HiddenOrUnknown => ("unknown_resource", "schema.hidden-or-unknown", "HFC-MCP-UNKNOWN-RESOURCE"),
            McpSchemaNegotiationResultKind.StaleDescriptor => ("projection_unavailable", "schema.stale-descriptor", "HFC-MCP-STALE-DESCRIPTOR"),
            McpSchemaNegotiationResultKind.SchemaIntegrityMismatch => ("schema-unavailable", "schema.integrity-mismatch", "HFC-SCHEMA-INTEGRITY-MISMATCH"),
            McpSchemaNegotiationResultKind.Incompatible => ("schema-mismatch", "schema.incompatible", "HFC-SCHEMA-MISMATCH"),
            _ => throw new InvalidOperationException("Unexpected branch in Story 11.5 enum-label mapping test: " + kind),
        };

    private static string ToKebabCase(string value)
        => string.Concat(value.Select((ch, index) => index > 0 && char.IsUpper(ch) ? "-" + char.ToLowerInvariant(ch) : char.ToLowerInvariant(ch).ToString()));

    private static string ToDotCase(string value)
        => ToKebabCase(value).Replace('-', '.');

    private static SchemaContractDocument AggregateDocument(IReadOnlyList<SchemaFingerprint> fingerprints)
        => new(
            "frontcomposer.schema.contract.v1",
            SchemaContractFamily.AggregateMcpManifest,
            "frontcomposer://mcp/runtime-manifest",
            "frontcomposer.mcp-manifest.aggregate.v1",
            null,
            null,
            "frontcomposer://mcp/runtime-manifest",
            [.. fingerprints
                .OrderBy(f => f.AlgorithmId, StringComparer.Ordinal)
                .ThenBy(f => f.Value, StringComparer.Ordinal)
                .Select(f => new SchemaFieldContract(f.AlgorithmId + ":" + f.Value, "SchemaFingerprint", "string", true, false))],
            [new SchemaCollectionContract("fingerprints", SchemaCollectionOrder.NonStructuralSorted, "name")],
            new Dictionary<string, string> {
                ["corpusFingerprintCount"] = "1",
            });

    private sealed class SchemaAwareStaticAccessor(SchemaFingerprint clientFingerprintHint) : IFrontComposerMcpAgentContextAccessor {
        public FrontComposerMcpAgentContext GetContext()
            => new(
                "tenant-a",
                "agent-a",
                new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "test", nameType: "name", roleType: "role")));

        public SchemaFingerprint ClientFingerprintHint { get; } = clientFingerprintHint;
    }

    private sealed class StaticCorpusFingerprintProvider(IReadOnlyList<SchemaFingerprint> fingerprints) : ISkillCorpusFingerprintProvider {
        public IReadOnlyList<SchemaFingerprint> GetFingerprints() => fingerprints;
    }

    private sealed class CountingCorpusFingerprintProvider(IReadOnlyList<SchemaFingerprint> fingerprints) : ISkillCorpusFingerprintProvider {
        public int CallCount { get; private set; }

        public IReadOnlyList<SchemaFingerprint> GetFingerprints() {
            CallCount++;
            return fingerprints;
        }
    }
}
