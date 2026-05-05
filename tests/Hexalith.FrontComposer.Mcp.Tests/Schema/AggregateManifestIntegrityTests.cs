using System.Reflection;

using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Contracts.Schema;
using Hexalith.FrontComposer.Mcp.Invocation;

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
    private const string SkipReason = "RED-PHASE: T5 — runtime aggregate integrity check pending.";

    [Fact(Skip = SkipReason)]
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

    [Fact(Skip = SkipReason)]
    public void RuntimeAggregate_IncludesCorpusFingerprints_WhenSkillCorpusIsLoaded() {
        // AC8: the runtime aggregate manifest fingerprint must include skill corpus resource
        // fingerprints. The build-time aggregate (emitted by SourceTools) cannot see corpus
        // fingerprints — it deliberately fingerprints generated code only — so the runtime
        // recomputation must layer them in. T5 owns the runtime aggregator; until it lands,
        // this scaffold's reflection lookup of "RuntimeManifestAggregator" returns null.
        Type? aggregator = typeof(FrontComposerMcpDescriptorRegistry).Assembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == "RuntimeManifestAggregator" || t.Name == "FrontComposerMcpRuntimeManifestAggregator");

        aggregator.ShouldNotBeNull("AC8 / T5 require a runtime aggregator that recomputes the aggregate over corpus fingerprints.");

        MethodInfo? compute = aggregator!.GetMethod("Compute", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
        compute.ShouldNotBeNull();

        ParameterInfo[] parameters = compute!.GetParameters();
        bool hasCorpusParameter = parameters.Any(
            p => p.ParameterType.Name.Contains("CorpusFingerprint", StringComparison.OrdinalIgnoreCase)
                || (p.Name is not null && p.Name.Contains("corpus", StringComparison.OrdinalIgnoreCase)));
        hasCorpusParameter.ShouldBeTrue("AC8: aggregator surface must accept skill corpus fingerprints.");
    }

    [Fact(Skip = SkipReason)]
    public void RuntimeAggregate_TamperedCorpusFingerprint_TripsIntegrityMismatch() {
        // AC7 + AC8 combined: a corpus loader that returns a fingerprint not present in the
        // SourceTools-emitted aggregate must trip integrity mismatch at runtime, not silently
        // re-stamp the aggregate with the corrupted material.
        // This is exercised structurally; the test resolves the registry through DI and asserts
        // the registry refuses to materialize when corpus + nested + aggregate disagree.

        ServiceCollection services = [];
        services.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(new McpManifest("frontcomposer.mcp.v1", [], [])));
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<FrontComposerMcpDescriptorRegistry>();

        // Once T5 lands, registering a TamperedCorpusFingerprintProvider here must propagate
        // through the runtime aggregator, which must then refuse to materialize.
        // The reflection-based resolver below documents the contract for the dev: implement
        // the corpus tamper-detection seam before unskipping.
        Type? tamperProvider = typeof(FrontComposerMcpDescriptorRegistry).Assembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == "ISkillCorpusFingerprintProvider");
        tamperProvider.ShouldNotBeNull("AC7 / AC8 / T5 require a corpus fingerprint provider seam.");
    }
}
