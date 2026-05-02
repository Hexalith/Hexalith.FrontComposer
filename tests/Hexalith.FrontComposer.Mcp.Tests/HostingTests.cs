using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp.Extensions;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests;

public sealed class HostingTests {
    [Fact]
    public void AddFrontComposerMcp_RegistersManifestDescriptors() {
        McpManifest manifest = CreateManifest("billing.invoice.create");
        var services = new ServiceCollection();

        services.AddFrontComposerMcp(options => options.Manifests.Add(manifest));

        using ServiceProvider provider = services.BuildServiceProvider();
        FrontComposerMcpDescriptorRegistry registry = provider.GetRequiredService<FrontComposerMcpDescriptorRegistry>();

        registry.Commands.Single().ProtocolName.ShouldBe("billing.invoice.create");
    }

    [Fact]
    public void AddFrontComposerMcp_RejectsDuplicateCommandNames() {
        var services = new ServiceCollection();

        Should.Throw<FrontComposerMcpException>(() => services.AddFrontComposerMcp(options => {
            options.Manifests.Add(CreateManifest("billing.invoice.create"));
            options.Manifests.Add(CreateManifest("BILLING.INVOICE.CREATE"));
        })).Category.ShouldBe(FrontComposerMcpFailureCategory.DuplicateDescriptor);
    }

    private static McpManifest CreateManifest(string protocolName)
        => new(
            "frontcomposer.mcp.v1",
            [
                new McpCommandDescriptor(
                    protocolName,
                    typeof(SampleCommand).FullName!,
                    "Billing",
                    "Create invoice",
                    "Creates an invoice.",
                    null,
                    [],
                    []),
            ],
            []);

    private sealed class SampleCommand;
}
