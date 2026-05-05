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

        Exception ex = Should.Throw<Exception>(
            () => services.BuildServiceProvider().GetRequiredService<FrontComposerMcpDescriptorRegistry>());

        // AC7: descriptor registry must throw a sanitized integrity-mismatch error before exposing the manifest.
        ex.Message.ToLowerInvariant().ShouldContain("integrity");
    }

    [Fact]
    public void RuntimeAggregate_IncludesCorpusFingerprints_WhenSkillCorpusIsLoaded() {
        // AC8: the runtime aggregate manifest fingerprint must include skill corpus resource
        // fingerprints. The build-time aggregate (emitted by SourceTools) cannot see corpus
        // fingerprints — it deliberately fingerprints generated code only — so the runtime
        SchemaFingerprint nested = new(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, new string('a', 64));
        SchemaFingerprint corpus = new(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, new string('b', 64));
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
            "AC8: corpus resource fingerprints must contribute to the runtime aggregate.");
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

        Exception ex = Should.Throw<Exception>(
            () => services.BuildServiceProvider().GetRequiredService<FrontComposerMcpDescriptorRegistry>());

        ex.Message.ToLowerInvariant().ShouldContain("integrity");
    }

    private sealed class StaticCorpusFingerprintProvider(IReadOnlyList<SchemaFingerprint> fingerprints) : ISkillCorpusFingerprintProvider {
        public IReadOnlyList<SchemaFingerprint> GetFingerprints() => fingerprints;
    }
}
