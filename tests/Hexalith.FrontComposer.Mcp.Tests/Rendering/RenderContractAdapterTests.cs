using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Schema;
using Hexalith.FrontComposer.Mcp.Invocation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;
using Xunit;

namespace Hexalith.FrontComposer.Mcp.Tests.Rendering;

/// <summary>
/// AC14 / T7 — at least one .Mcp adapter produces a FrontComposerRenderContract per Markdown
/// projection resource and registers it through the descriptor registry. Web/Blazor adapters
/// remain placeholders pending future stories.
/// </summary>
public sealed class RenderContractAdapterTests {
    private const string SkipReason = "RED-PHASE: T7 — Mcp render-contract adapter pending.";

    [Fact(Skip = SkipReason)]
    public void DescriptorRegistry_ExposesRenderContract_PerMarkdownProjectionResource() {
        FrontComposerMcpDescriptorRegistry registry = BuildRegistry(SampleResource());

        IReadOnlyList<FrontComposerRenderContract> contracts = ResolveRenderContracts(registry);

        contracts.ShouldNotBeEmpty("AC14: at least one Markdown projection resource must publish a render contract.");
        contracts.ShouldContain(c => c.Surface == RenderSurfaceKind.McpMarkdown);
    }

    [Fact(Skip = SkipReason)]
    public void RenderContract_BoundsMatchOptions_AndCarrySanitizedTaxonomy() {
        FrontComposerMcpDescriptorRegistry registry = BuildRegistry(SampleResource());

        FrontComposerRenderContract contract = ResolveRenderContracts(registry)
            .First(c => c.Surface == RenderSurfaceKind.McpMarkdown);

        contract.OutputContentType.ShouldBe("text/markdown");
        contract.Capabilities.ShouldContain(RenderCapability.BoundedMarkdown);
        contract.Capabilities.ShouldContain(RenderCapability.SanitizedInertText);
        contract.Bounds.MaxCharacters.ShouldBeGreaterThan(0,
            "AC9 / T6 ties bounds to FrontComposerMcpOptions / SkillResourceReadOptions; the adapter must surface the live values.");
        contract.Fingerprint.AlgorithmId.ShouldBeOneOf(
            SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1,
            SchemaFingerprintAlgorithm.Sha256SourceToolsBlobV1);
    }

    [Fact(Skip = SkipReason)]
    public void WebBlazorAdapter_RemainsPlaceholder_DoesNotRegisterContract() {
        // Story 8-6a scope guard: only the .Mcp adapter ships in this story; Web/Blazor adapters
        // remain placeholders. The registry must not yet expose a WebBlazor render contract.
        FrontComposerMcpDescriptorRegistry registry = BuildRegistry(SampleResource());

        ResolveRenderContracts(registry)
            .ShouldNotContain(c => c.Surface == RenderSurfaceKind.WebBlazor,
                "AC14 scope guard: WebBlazor adapter is out of scope for Story 8-6a.");
    }

    private static FrontComposerMcpDescriptorRegistry BuildRegistry(McpResourceDescriptor resource) {
        ServiceCollection services = [];
        services.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(new McpManifest("frontcomposer.mcp.v1", [], [resource])));
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        return services.BuildServiceProvider().GetRequiredService<FrontComposerMcpDescriptorRegistry>();
    }

    private static McpResourceDescriptor SampleResource()
        => new(
            "frontcomposer://Billing/projections/InvoiceProjection",
            "InvoiceProjection",
            "Hexalith.FrontComposer.Sample.InvoiceProjection",
            "Billing",
            "Invoices",
            null,
            [
                new McpParameterDescriptor("Number", "String", "string", true, false, "Number", null, [], false),
            ],
            RenderStrategy: McpProjectionRenderStrategy.Default,
            Fingerprint: new SchemaFingerprint(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, new string('a', 64)));

    /// <summary>
    /// Resolves render contracts from the registry through the seam T7 will introduce. Until T7
    /// lands, the seam doesn't exist and the assertion fails meaningfully when unskipped.
    /// </summary>
    private static IReadOnlyList<FrontComposerRenderContract> ResolveRenderContracts(FrontComposerMcpDescriptorRegistry registry) {
        // Expected accessor shape: registry.GetRenderContracts() or a sibling provider.
        System.Reflection.MethodInfo? accessor = registry.GetType()
            .GetMethod("GetRenderContracts", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        accessor.ShouldNotBeNull(
            "AC14 / T7 require a public registry accessor for render contracts. Implement T7 before unskipping.");

        object? result = accessor!.Invoke(registry, []);
        return (IReadOnlyList<FrontComposerRenderContract>)result!;
    }
}
