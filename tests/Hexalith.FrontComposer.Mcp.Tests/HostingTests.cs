using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp.Extensions;
using Hexalith.FrontComposer.Mcp.Skills;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests;

public sealed class HostingTests {
    [Fact]
    public void AddFrontComposerMcp_RegistersManifestDescriptors() {
        McpManifest manifest = CreateManifest("billing.invoice.create");
        var services = new ServiceCollection();
        services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();

        services.AddFrontComposerMcp(options => options.Manifests.Add(manifest));

        using ServiceProvider provider = services.BuildServiceProvider();
        FrontComposerMcpDescriptorRegistry registry = provider.GetRequiredService<FrontComposerMcpDescriptorRegistry>();

        registry.Commands.Single().ProtocolName.ShouldBe("billing.invoice.create");
    }

    [Fact]
    public void AddFrontComposerMcp_RegistersProjectionAndSkillResourcesWithSdkCollection() {
        McpManifest manifest = CreateManifest("billing.invoice.create", DescriptorUri: "frontcomposer://Billing/projections/InvoiceProjection");
        var services = new ServiceCollection();
        services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();

        services.AddFrontComposerMcp(options => options.Manifests.Add(manifest));

        using ServiceProvider provider = services.BuildServiceProvider();
        McpServerOptions options = provider.GetRequiredService<IOptions<McpServerOptions>>().Value;
        McpServerResource[] resources = [.. options.ResourceCollection.ShouldNotBeNull()];

        resources.Any(IsProjectionResource).ShouldBeTrue();
        resources.Any(IsSkillManifestResource).ShouldBeTrue();
        resources.Any(IsSkillIndexResource).ShouldBeTrue();
    }

    [Fact]
    public void AddFrontComposerMcp_RejectsDuplicateCommandNames() {
        var services = new ServiceCollection();
        services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();

        Should.Throw<FrontComposerMcpException>(() => services.AddFrontComposerMcp(options => {
            options.Manifests.Add(CreateManifest("billing.invoice.create"));
            options.Manifests.Add(CreateManifest("BILLING.INVOICE.CREATE"));
        })).Category.ShouldBe(FrontComposerMcpFailureCategory.DuplicateDescriptor);
    }

    [Theory]
    [InlineData("frontcomposer://skills/manifest")]
    [InlineData("frontcomposer://skills/index")]
    [InlineData("frontcomposer://skills/not-yet-a-corpus-resource")]
    public void AddFrontComposerMcp_RejectsProjectionResourcesUnderReservedSkillUriPrefix(string descriptorUri) {
        var services = new ServiceCollection();
        services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(() =>
            services.AddFrontComposerMcp(options => options.Manifests.Add(CreateManifest(
                "billing.invoice.create",
                DescriptorUri: descriptorUri))));

        ex.Message.ShouldContain("URI collision");
        ex.Message.ShouldContain("frontcomposer://skills/");
    }

    [Fact]
    public void AddFrontComposerMcp_FailsClosed_WhenTenantGateNotRegistered() {
        // D1: tenant isolation is fail-closed by contract; the host MUST register a real gate
        // (or AllowAllMcpTenantToolGate explicitly for samples). Default registration was removed
        // to honor the project's fail-closed memory rule.
        var services = new ServiceCollection();

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => services.AddFrontComposerMcp(options =>
            options.Manifests.Add(CreateManifest("billing.invoice.create"))));

        ex.Message.ShouldContain(nameof(IFrontComposerMcpTenantToolGate));
        ex.Message.ShouldContain("Register a host-supplied gate before AddFrontComposerMcp");
        ex.Message.ShouldContain(nameof(AllowAllMcpTenantToolGate));
        services.Any(d => d.ServiceType == typeof(IFrontComposerMcpTenantToolGate)).ShouldBeFalse();
    }

    [Fact]
    public void AddFrontComposerMcp_FailsClosed_WhenResourceVisibilityGateNotRegistered() {
        var services = new ServiceCollection();
        services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => services.AddFrontComposerMcp(options =>
            options.Manifests.Add(CreateManifest("billing.invoice.create"))));

        ex.Message.ShouldContain(nameof(IFrontComposerMcpResourceVisibilityGate));
        ex.Message.ShouldContain("Register a host-supplied gate before AddFrontComposerMcp");
        ex.Message.ShouldContain(nameof(AllowAllResourceVisibilityGate));
        ex.Message.ShouldContain("Skill corpus resources are framework-global");
        services.Any(d => d.ServiceType == typeof(IFrontComposerMcpResourceVisibilityGate)).ShouldBeFalse();
    }

    [Fact]
    public void AddFrontComposerMcp_PreservesExplicitSampleDevAllowAllGateRegistrations() {
        var services = new ServiceCollection();
        services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();

        services.AddFrontComposerMcp(options => options.Manifests.Add(CreateManifest("billing.invoice.create")));

        ServiceDescriptor tenantGate = services.Single(d => d.ServiceType == typeof(IFrontComposerMcpTenantToolGate));
        tenantGate.ImplementationType.ShouldBe(typeof(AllowAllMcpTenantToolGate));
        ServiceDescriptor resourceGate = services.Single(d => d.ServiceType == typeof(IFrontComposerMcpResourceVisibilityGate));
        resourceGate.ImplementationType.ShouldBe(typeof(AllowAllResourceVisibilityGate));
    }

    [Fact]
    public void AddFrontComposerMcp_ValidatesProjectionMarkdownBounds() {
        var services = new ServiceCollection();
        services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
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

    private static McpManifest CreateManifest(string protocolName, string? DescriptorUri = null)
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
            DescriptorUri is null ? [] : [
                new McpResourceDescriptor(
                    DescriptorUri,
                    "InvoiceProjection",
                    typeof(SampleProjection).FullName!,
                    "Billing",
                    "Invoices",
                    "Invoices",
                    [
                        new McpParameterDescriptor("Number", "String", "string", true, false, "Number", null, [], false),
                    ]),
            ]);

    private static bool IsProjectionResource(McpServerResource resource) {
        Resource? protocol = resource.ProtocolResource;
        return resource is FrontComposerMcpResource
            && protocol is not null
            && protocol.Uri == "frontcomposer://Billing/projections/InvoiceProjection"
            && protocol.MimeType == "text/markdown"
            && resource.Metadata.Single() is McpResourceDescriptor;
    }

    private static bool IsSkillManifestResource(McpServerResource resource) {
        Resource? protocol = resource.ProtocolResource;
        return resource is FrontComposerSkillMcpResource
            && protocol is not null
            && protocol.Uri == "frontcomposer://skills/manifest"
            && protocol.MimeType == "text/markdown"
            && resource.Metadata.Single() is SkillResourceDescriptor;
    }

    private static bool IsSkillIndexResource(McpServerResource resource) {
        Resource? protocol = resource.ProtocolResource;
        return resource is FrontComposerSkillMcpResource
            && protocol is not null
            && protocol.Uri == "frontcomposer://skills/index"
            && protocol.MimeType == "text/markdown"
            && resource.Metadata.Single() is SkillResourceDescriptor descriptor
            && descriptor.Fingerprint is not null;
    }

    private sealed class SampleCommand;

    private sealed class SampleProjection;
}
