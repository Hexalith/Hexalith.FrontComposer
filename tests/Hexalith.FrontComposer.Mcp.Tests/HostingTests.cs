using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests;

public sealed class HostingTests {
    [Fact]
    public void AddFrontComposerMcp_RegistersManifestDescriptors() {
        McpManifest manifest = CreateManifest("billing.invoice.create");
        var services = new ServiceCollection();
        services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();

        services.AddFrontComposerMcp(options => options.Manifests.Add(manifest));

        using ServiceProvider provider = services.BuildServiceProvider();
        FrontComposerMcpDescriptorRegistry registry = provider.GetRequiredService<FrontComposerMcpDescriptorRegistry>();

        registry.Commands.Single().ProtocolName.ShouldBe("billing.invoice.create");
    }

    [Fact]
    public void AddFrontComposerMcp_RejectsDuplicateCommandNames() {
        var services = new ServiceCollection();
        services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();

        Should.Throw<FrontComposerMcpException>(() => services.AddFrontComposerMcp(options => {
            options.Manifests.Add(CreateManifest("billing.invoice.create"));
            options.Manifests.Add(CreateManifest("BILLING.INVOICE.CREATE"));
        })).Category.ShouldBe(FrontComposerMcpFailureCategory.DuplicateDescriptor);
    }

    [Fact]
    public void AddFrontComposerMcp_FailsClosed_WhenTenantGateNotRegistered() {
        // D1: tenant isolation is fail-closed by contract; the host MUST register a real gate
        // (or AllowAllMcpTenantToolGate explicitly for samples). Default registration was removed
        // to honor the project's fail-closed memory rule.
        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(() => services.AddFrontComposerMcp(options =>
            options.Manifests.Add(CreateManifest("billing.invoice.create"))));
    }

    [Fact]
    public void AddFrontComposerMcp_ValidatesProjectionMarkdownBounds() {
        var services = new ServiceCollection();
        services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        OptionsValidationException ex = Should.Throw<OptionsValidationException>(() => services.AddFrontComposerMcp(options => {
            options.Manifests.Add(CreateManifest("billing.invoice.create"));
            options.MaxProjectionCellCharacters = 0;
        }));
        ex.Failures.ShouldContain(f => f.Contains("Projection Markdown render limits", StringComparison.Ordinal));
    }

    [Fact]
    public void DescriptorRegistry_DoesNotReflectPostConstructionManifestMutation() {
        FrontComposerMcpOptions options = new();
        options.Manifests.Add(CreateManifest("billing.invoice.create"));
        var registry = new FrontComposerMcpDescriptorRegistry(Options.Create(options));

        options.Manifests.Add(CreateManifest("billing.invoice.update"));

        registry.Commands.Select(c => c.ProtocolName).ShouldBe(["billing.invoice.create"]);
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
