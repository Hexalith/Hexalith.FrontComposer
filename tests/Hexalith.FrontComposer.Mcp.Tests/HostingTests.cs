using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp.Extensions;
using Hexalith.FrontComposer.Mcp.Invocation;
using Hexalith.FrontComposer.Mcp.Skills;

using Microsoft.AspNetCore.Builder;
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
        _ = services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        _ = services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();

        _ = services.AddFrontComposerMcp(options => options.Manifests.Add(manifest));

        using ServiceProvider provider = services.BuildServiceProvider();
        FrontComposerMcpDescriptorRegistry registry = provider.GetRequiredService<FrontComposerMcpDescriptorRegistry>();

        registry.Commands.Single().ProtocolName.ShouldBe("billing.invoice.create");
    }

    [Fact]
    public void AddFrontComposerMcp_RegistersProjectionAndSkillResourcesWithSdkCollection() {
        McpManifest manifest = CreateManifest("billing.invoice.create", DescriptorUri: "frontcomposer://Billing/projections/InvoiceProjection");
        var services = new ServiceCollection();
        _ = services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        _ = services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();

        _ = services.AddFrontComposerMcp(options => options.Manifests.Add(manifest));

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
        _ = services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        _ = services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();

        _ = services.AddFrontComposerMcp(options => {
            options.Manifests.Add(CreateManifest("billing.invoice.create"));
            options.Manifests.Add(CreateManifest("BILLING.INVOICE.CREATE"));
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        Should.Throw<FrontComposerMcpException>(() => provider.GetRequiredService<FrontComposerMcpDescriptorRegistry>())
            .Category.ShouldBe(FrontComposerMcpFailureCategory.DuplicateDescriptor);
    }

    [Theory]
    [InlineData("frontcomposer://skills/manifest")]
    [InlineData("frontcomposer://skills/index")]
    [InlineData("frontcomposer://skills/not-yet-a-corpus-resource")]
    public void AddFrontComposerMcp_RejectsProjectionResourcesUnderReservedSkillUriPrefix(string descriptorUri) {
        var services = new ServiceCollection();
        _ = services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        _ = services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();

        _ = services.AddFrontComposerMcp(options => options.Manifests.Add(CreateManifest(
                "billing.invoice.create",
                DescriptorUri: descriptorUri)));

        using ServiceProvider provider = services.BuildServiceProvider();
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(() =>
            provider.GetRequiredService<IOptions<McpServerOptions>>().Value);
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
        _ = services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();

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
        _ = services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        _ = services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();

        _ = services.AddFrontComposerMcp(options => options.Manifests.Add(CreateManifest("billing.invoice.create")));

        ServiceDescriptor tenantGate = services.Single(d => d.ServiceType == typeof(IFrontComposerMcpTenantToolGate));
        tenantGate.ImplementationType.ShouldBe(typeof(AllowAllMcpTenantToolGate));
        ServiceDescriptor resourceGate = services.Single(d => d.ServiceType == typeof(IFrontComposerMcpResourceVisibilityGate));
        resourceGate.ImplementationType.ShouldBe(typeof(AllowAllResourceVisibilityGate));
    }

    [Fact]
    public void AddFrontComposerMcp_ValidatesProjectionMarkdownBounds() {
        var services = new ServiceCollection();
        _ = services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        _ = services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
        _ = services.AddFrontComposerMcp(options => {
            options.Manifests.Add(CreateManifest("billing.invoice.create"));
            options.MaxProjectionCellCharacters = 0;
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        OptionsValidationException ex = Should.Throw<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<FrontComposerMcpOptions>>().Value);
        ex.Failures.ShouldContain(f => f.Contains("Projection Markdown render limits", StringComparison.Ordinal));
    }

    [Fact]
    public void AddFrontComposerMcp_RegistersLifecycleStoreAsSingletonAndFacadeAsScoped() {
        var services = new ServiceCollection();
        _ = services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        _ = services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();

        _ = services.AddFrontComposerMcp(options => options.Manifests.Add(CreateManifest("billing.invoice.create")));

        services.Single(d => d.ServiceType == typeof(FrontComposerMcpLifecycleStore)).Lifetime.ShouldBe(ServiceLifetime.Singleton);
        services.Single(d => d.ServiceType == typeof(FrontComposerMcpLifecycleTracker)).Lifetime.ShouldBe(ServiceLifetime.Scoped);
        services.Single(d => d.ServiceType == typeof(IFrontComposerMcpAgentContextAccessor)).Lifetime.ShouldBe(ServiceLifetime.Scoped);
        services.Single(d => d.ServiceType == typeof(FrontComposerMcpCommandInvoker)).Lifetime.ShouldBe(ServiceLifetime.Scoped);
        services.Single(d => d.ServiceType == typeof(FrontComposerMcpProjectionReader)).Lifetime.ShouldBe(ServiceLifetime.Scoped);
        services.Single(d => d.ServiceType == typeof(FrontComposerMcpToolAdmissionService)).Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddFrontComposerMcp_DoesNotInstantiateServicesThroughTemporaryProvider() {
        int skillProviderConstructions = 0;
        var services = new ServiceCollection();
        _ = services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        _ = services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
        _ = services.AddSingleton(_ => {
            skillProviderConstructions++;
            return new FrontComposerSkillResourceProvider(new SkillCorpusSnapshot([], []));
        });

        _ = services.AddFrontComposerMcp(options => options.Manifests.Add(CreateManifest("billing.invoice.create")));

        skillProviderConstructions.ShouldBe(0);
        using ServiceProvider provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IOptions<McpServerOptions>>().Value;
        skillProviderConstructions.ShouldBe(1);
    }

    [Theory]
    [InlineData("frontcomposer://lifecycle")]
    [InlineData("https://user:secret@example.test/lifecycle/")]
    [InlineData("file:///tmp/frontcomposer/lifecycle/")]
    [InlineData("http://example.test/lifecycle/")]
    public void AddFrontComposerMcp_ValidatesLifecycleUriPrefix(string lifecycleUriPrefix) {
        var services = new ServiceCollection();
        _ = services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        _ = services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
        _ = services.AddFrontComposerMcp(options => {
            options.Manifests.Add(CreateManifest("billing.invoice.create"));
            options.LifecycleUriPrefix = lifecycleUriPrefix;
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        OptionsValidationException ex = Should.Throw<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<FrontComposerMcpOptions>>().Value);
        ex.Failures.ShouldContain(f => f.Contains(nameof(FrontComposerMcpOptions.LifecycleUriPrefix), StringComparison.Ordinal));
    }

    [Fact]
    public void MapFrontComposerMcp_MaterializesSdkResourcesAndRejectsReservedSkillUriCollisionAtStartup() {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        _ = builder.Services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        _ = builder.Services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
        _ = builder.Services.AddFrontComposerMcp(options => options.Manifests.Add(CreateManifest(
            "billing.invoice.create",
            DescriptorUri: "frontcomposer://skills/manifest")));
        using WebApplication app = builder.Build();

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => app.MapFrontComposerMcp());

        ex.Message.ShouldContain("URI collision");
        ex.Message.ShouldContain("frontcomposer://skills/");
    }

    [Fact]
    public void AddFrontComposerMcp_ValidatesLifecycleRetryBounds() {
        var services = new ServiceCollection();
        _ = services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        _ = services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
        _ = services.AddFrontComposerMcp(options => {
            options.Manifests.Add(CreateManifest("billing.invoice.create"));
            options.MinLifecycleRetryAfterMs = 1000;
            options.DefaultLifecycleRetryAfterMs = 100;
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        OptionsValidationException ex = Should.Throw<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<FrontComposerMcpOptions>>().Value);
        ex.Failures.ShouldContain(f => f.Contains("Lifecycle bounds", StringComparison.Ordinal));
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
