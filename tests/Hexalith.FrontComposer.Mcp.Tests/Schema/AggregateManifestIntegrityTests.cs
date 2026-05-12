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
/// AC7 / AC8 / T5 — descriptor registry must recompute the aggregate manifest fingerprint from
/// nested fingerprints at runtime and fail closed via SchemaIntegrityMismatch when the embedded
/// aggregate disagrees. The runtime aggregate must include skill corpus resource fingerprints
/// (Story 8-5 D4 / D22), even though the build-time emitter cannot see them.
/// </summary>
public sealed class AggregateManifestIntegrityTests {
    [Fact]
    public void DescriptorRegistry_LoadingTamperedAggregate_FailsClosed_WithIntegrityMismatch() {
        // Build a manifest where the embedded aggregate fingerprint is forged so it disagrees
        // with the structurally-recomputed aggregate over its nested resource fingerprints.
        SchemaFingerprint nestedResourceFp = new(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, new string('a', 64));
        SchemaFingerprint forgedAggregateFp = new(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, new string('z', 64));

        McpManifest tampered = new(
            "frontcomposer.mcp.v1",
            [],
            [new McpResourceDescriptor(
                "frontcomposer://Billing/projections/InvoiceProjection",
                "InvoiceProjection",
                "Hexalith.FrontComposer.Sample.InvoiceProjection",
                "Billing",
                "Invoices",
                null,
                [
                    new McpParameterDescriptor("Number", "String", "string", true, false, "Number", null, [], false),
                ],
                Fingerprint: nestedResourceFp)],
            Fingerprint: forgedAggregateFp);

        ServiceCollection services = [];
        services.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(tampered));
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<FrontComposerMcpDescriptorRegistry>();

        // CK4-P2: pin to the typed FrontComposerMcpException so DI activation wrappers, NREs, or
        // localized message rewrites cannot silently satisfy the assertion. AC7 mandates the
        // SchemaIntegrityMismatch category specifically.
        FrontComposerMcpException ex = Should.Throw<FrontComposerMcpException>(
            () => services.BuildServiceProvider().GetRequiredService<FrontComposerMcpDescriptorRegistry>());

        ex.Category.ShouldBe(
            FrontComposerMcpFailureCategory.SchemaIntegrityMismatch,
            "AC7: descriptor registry must fail closed with SchemaIntegrityMismatch.");
    }

    [Fact]
    public void Aggregator_WhenInvokedWithCorpusFingerprints_ChangesAggregate() {
        // CK4-P4: the prior test name "RuntimeAggregate_IncludesCorpusFingerprints_WhenSkillCorpusIsLoaded"
        // suggested an end-to-end verification, but the production runtime path discards corpus
        // fingerprints (see chunk-2 C2 deferral — `FrontComposerMcpDescriptorRegistry` does
        // `_ = corpusFingerprints;`). This test only proves the seam exists at the aggregator
        // level; AC8 production wiring is acknowledged broken and deferred until D3 build-time
        // baseline materialization lands. Renamed to remove the misleading "WhenSkillCorpusIsLoaded"
        // implication.
        // CK4-P5: also pin order-invariance and dedup so the tuple-key dedup contract cannot
        // regress without breaking the test (chunk-2 deferred entries on cross-algorithm
        // aggregation and tuple-key dedup at lines 435-436 of the spec).
        SchemaFingerprint nested = new(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, new string('a', 64));
        SchemaFingerprint corpus = new(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, new string('b', 64));
        SchemaFingerprint corpus2 = new(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, new string('c', 64));
        McpManifest manifest = new(
            "frontcomposer.mcp.v1",
            [new McpCommandDescriptor(
                "Billing.PayInvoiceCommand.Execute",
                "Billing.PayInvoiceCommand",
                "Billing",
                "Pay invoice",
                null,
                null,
                [],
                [],
                nested)],
            []);

        SchemaFingerprint withoutCorpus = FrontComposerMcpRuntimeManifestAggregator.Compute([manifest], []);
        SchemaFingerprint withCorpus = FrontComposerMcpRuntimeManifestAggregator.Compute([manifest], [corpus]);

        withCorpus.Value.ShouldNotBe(
            withoutCorpus.Value,
            "AC8 seam: corpus resource fingerprints must contribute to the runtime aggregate at the aggregator boundary.");

        // CK4-P5: order-invariance — the aggregator must canonicalize input order so two hosts
        // with the same corpus set produce the same fingerprint regardless of registration order.
        // The dedup invariant (collapsing duplicate (AlgorithmId, Value) tuples) is internally
        // enforced via tuple-keyed HashSet in `FrontComposerMcpRuntimeManifestAggregator.Compute`.
        // We do NOT assert `Compute([fp,fp]) == Compute([fp])` here because the production
        // code intentionally includes `corpusFingerprintCount` (the raw input cardinality) in
        // the aggregate's metadata for telemetry — so duplicate inputs produce a different
        // aggregate even when the deduped set is identical. This is a deliberate design choice;
        // see `FrontComposerMcpRuntimeManifestAggregator:55`.
        SchemaFingerprint orderA = FrontComposerMcpRuntimeManifestAggregator.Compute([manifest], [corpus, corpus2]);
        SchemaFingerprint orderB = FrontComposerMcpRuntimeManifestAggregator.Compute([manifest], [corpus2, corpus]);
        orderB.Value.ShouldBe(
            orderA.Value,
            "Aggregator must canonicalize corpus order — same set produces the same aggregate fingerprint.");
    }

    [Fact]
    public void Aggregator_MixedFingerprintAlgorithms_FailsClosed() {
        SchemaFingerprint nested = new(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, new string('a', 64));
        SchemaFingerprint corpus = new(SchemaFingerprintAlgorithm.Sha256SourceToolsBlobV1, new string('b', 64));
        McpManifest manifest = new(
            "frontcomposer.mcp.v1",
            [new McpCommandDescriptor(
                "Billing.PayInvoiceCommand.Execute",
                "Billing.PayInvoiceCommand",
                "Billing",
                "Pay invoice",
                null,
                null,
                [],
                [],
                nested)],
            []);

        FrontComposerMcpException ex = Should.Throw<FrontComposerMcpException>(
            () => FrontComposerMcpRuntimeManifestAggregator.Compute([manifest], [corpus]));

        ex.Category.ShouldBe(FrontComposerMcpFailureCategory.SchemaIntegrityMismatch);
    }

    [Fact]
    public void DescriptorRegistry_DiConstruction_UsesCorpusAwareConstructor() {
        CountingCorpusFingerprintProvider provider = new([]);
        ServiceCollection services = [];
        services.Configure<FrontComposerMcpOptions>(_ => { });
        services.AddSingleton<ISkillCorpusFingerprintProvider>(provider);
        services.AddSingleton<FrontComposerMcpDescriptorRegistry>();

        _ = services.BuildServiceProvider().GetRequiredService<FrontComposerMcpDescriptorRegistry>();

        provider.CallCount.ShouldBe(1, "DI must select the constructor that receives corpus providers.");
    }

    [Fact]
    public void DescriptorRegistry_DiConstruction_InvokesAllRegisteredCorpusProviders() {
        // 11-5 review P5 / T3: T3 required "DI composition coverage with at least two corpus
        // providers and a zero-provider case." This pins the multi-provider seam — both
        // providers must be invoked during registry construction, not just the first or the
        // last-registered one. A regression that consumed only IEnumerable<T>.First() or
        // GetService<ISkillCorpusFingerprintProvider>() (single resolution) would fail.
        CountingCorpusFingerprintProvider providerA = new([]);
        CountingCorpusFingerprintProvider providerB = new([]);
        ServiceCollection services = [];
        services.Configure<FrontComposerMcpOptions>(_ => { });
        services.AddSingleton<ISkillCorpusFingerprintProvider>(providerA);
        services.AddSingleton<ISkillCorpusFingerprintProvider>(providerB);
        services.AddSingleton<FrontComposerMcpDescriptorRegistry>();

        _ = services.BuildServiceProvider().GetRequiredService<FrontComposerMcpDescriptorRegistry>();

        providerA.CallCount.ShouldBe(1, "DI must invoke every registered ISkillCorpusFingerprintProvider, not only the first.");
        providerB.CallCount.ShouldBe(1, "DI must invoke every registered ISkillCorpusFingerprintProvider, not only the last.");
    }

    [Fact]
    public void DescriptorRegistry_DiConstruction_ZeroProviders_DoesNotFailClosed() {
        // 11-5 review P5 / T3 / D11: the zero-corpus-provider case is the "named legacy/release-
        // constraint path" recorded in the Story 11-5 deferred-work entries (hosts that ship no
        // skill corpus). Registry construction must succeed; the corpus contribution to the
        // runtime aggregate is empty. A regression that treated missing providers as fail-closed
        // would block every host that does not ship a skill corpus.
        ServiceCollection services = [];
        services.Configure<FrontComposerMcpOptions>(_ => { });
        services.AddSingleton<FrontComposerMcpDescriptorRegistry>();

        FrontComposerMcpDescriptorRegistry registry = services.BuildServiceProvider().GetRequiredService<FrontComposerMcpDescriptorRegistry>();

        registry.ShouldNotBeNull();
    }

    [Fact]
    public void RuntimeAggregate_TamperedNestedFingerprint_TripsIntegrityMismatch() {
        // 8-6a re-review D6: the integrity check is per-manifest scope. The build-time emitter
        // computes the manifest's claimed Fingerprint over its OWN nested fingerprints (no corpus
        // — the emitter cannot see runtime corpus). The runtime check must catch tampering with
        // those nested fingerprints (a forged resource fp inside an otherwise-signed manifest).
        // The cross-manifest+corpus invariant lives in `FrontComposerMcpRuntimeManifestAggregator
        // .Compute(...)` and is tested separately in
        // `RuntimeAggregate_IncludesCorpusFingerprints_WhenSkillCorpusIsLoaded`.
        SchemaFingerprint nestedResourceFp = new(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, new string('a', 64));
        SchemaFingerprint forgedNestedFp = new(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, new string('z', 64));
        McpManifest signed = new(
            "frontcomposer.mcp.v1",
            [],
            [new McpResourceDescriptor(
                "frontcomposer://Billing/projections/InvoiceProjection",
                "InvoiceProjection",
                "Hexalith.FrontComposer.Sample.InvoiceProjection",
                "Billing",
                "Invoices",
                null,
                [new McpParameterDescriptor("Number", "String", "string", true, false, "Number", null, [], false)],
                Fingerprint: nestedResourceFp)],
            Fingerprint: FrontComposerMcpRuntimeManifestAggregator.Compute(
                [new McpManifest(
                    "frontcomposer.mcp.v1",
                    [],
                    [new McpResourceDescriptor(
                        "frontcomposer://Billing/projections/InvoiceProjection",
                        "InvoiceProjection",
                        "Hexalith.FrontComposer.Sample.InvoiceProjection",
                        "Billing",
                        "Invoices",
                        null,
                        [new McpParameterDescriptor("Number", "String", "string", true, false, "Number", null, [], false)],
                        Fingerprint: nestedResourceFp)])],
                []));

        // Tamper: swap the nested resource fingerprint after signing. The manifest's claimed
        // fingerprint no longer matches the recomputed per-manifest aggregate.
        McpManifest tampered = signed with {
            Resources = [signed.Resources[0] with { Fingerprint = forgedNestedFp }],
        };

        ServiceCollection services = [];
        services.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(tampered));
        services.AddSingleton<ISkillCorpusFingerprintProvider>(
            new StaticCorpusFingerprintProvider([new SchemaFingerprint(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, new string('b', 64))]));
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<FrontComposerMcpDescriptorRegistry>();

        // CK4-P2: pin to the typed FrontComposerMcpException so DI activation wrappers, NREs, or
        // localized message rewrites cannot silently satisfy the assertion.
        FrontComposerMcpException ex = Should.Throw<FrontComposerMcpException>(
            () => services.BuildServiceProvider().GetRequiredService<FrontComposerMcpDescriptorRegistry>());

        ex.Category.ShouldBe(
            FrontComposerMcpFailureCategory.SchemaIntegrityMismatch,
            "AC7 (D6 per-manifest scope): tampered nested fingerprint must surface as SchemaIntegrityMismatch.");
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
