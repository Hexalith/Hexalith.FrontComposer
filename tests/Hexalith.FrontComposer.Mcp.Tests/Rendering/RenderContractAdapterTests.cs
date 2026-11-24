using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Schema;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Rendering;

/// <summary>
/// AC14 / T7 — at least one .Mcp adapter produces a FrontComposerRenderContract per Markdown
/// projection resource and registers it through the descriptor registry. Web/Blazor adapters
/// remain placeholders pending future stories.
/// </summary>
public sealed class RenderContractAdapterTests {
    [Fact]
    public void DescriptorRegistry_ExposesRenderContract_PerMarkdownProjectionResource() {
        FrontComposerMcpDescriptorRegistry registry = BuildRegistry(SampleResource());

        IReadOnlyList<FrontComposerRenderContract> contracts = registry.GetRenderContracts();

        contracts.ShouldNotBeEmpty("AC14: at least one Markdown projection resource must publish a render contract.");
        contracts.ShouldContain(c => c.Surface == RenderSurfaceKind.McpMarkdown);
    }

    [Fact]
    public void RenderContract_BoundsMatchOptions_AndCarrySanitizedTaxonomy() {
        const int maxRows = 17;
        const int maxColumns = 5;
        const int maxCharacters = 4200;
        const int maxCellCharacters = 121;
        FrontComposerMcpDescriptorRegistry registry = BuildRegistry(SampleResource(), options => {
            options.MaxRowsPerResource = maxRows;
            options.MaxFieldsPerResource = maxColumns;
            options.MaxProjectionMarkdownCharacters = maxCharacters;
            options.MaxProjectionCellCharacters = maxCellCharacters;
        });

        FrontComposerRenderContract contract = registry.GetRenderContracts()
            .First(c => c.Surface == RenderSurfaceKind.McpMarkdown);

        contract.OutputContentType.ShouldBe("text/markdown");
        contract.Capabilities.ShouldContain(RenderCapability.BoundedMarkdown);
        contract.Capabilities.ShouldContain(RenderCapability.SanitizedInertText);
        contract.Bounds.ShouldBe(new RenderBounds(maxRows, maxColumns, maxCharacters, maxCellCharacters));
        contract.Fingerprint.AlgorithmId.ShouldBeOneOf(
            SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1,
            SchemaFingerprintAlgorithm.Sha256SourceToolsBlobV1);
    }

    [Fact]
    public void WebBlazorAdapter_RemainsPlaceholder_DoesNotRegisterContract() {
        // Story 8-6a scope guard: only the .Mcp adapter ships in this story; Web/Blazor adapters
        // remain placeholders. The registry must not yet expose a WebBlazor render contract.
        FrontComposerMcpDescriptorRegistry registry = BuildRegistry(SampleResource());

        registry.GetRenderContracts()
            .ShouldNotContain(c => c.Surface == RenderSurfaceKind.WebBlazor,
                "AC14 scope guard: WebBlazor adapter is out of scope for Story 8-6a.");
    }

    private static FrontComposerMcpDescriptorRegistry BuildRegistry(
        McpResourceDescriptor resource,
        Action<FrontComposerMcpOptions>? configure = null) {
        ServiceCollection services = [];
        _ = services.Configure<FrontComposerMcpOptions>(options => {
            options.Manifests.Add(new McpManifest("frontcomposer.mcp.v1", [], [resource]));
            configure?.Invoke(options);
        });
        _ = services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        _ = services.AddSingleton<FrontComposerMcpDescriptorRegistry>();
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
}
