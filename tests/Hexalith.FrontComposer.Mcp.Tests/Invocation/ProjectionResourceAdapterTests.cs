using System.Security.Claims;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp.Invocation;

using Microsoft.Extensions.DependencyInjection;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Invocation;

public sealed class ProjectionResourceAdapterTests {
    [Fact]
    public async Task ReadAsync_ValidUri_RoutesThroughProjectionReaderAndReturnsMarkdownContent() {
        RecordingQueryService query = new();
        await using ServiceProvider provider = BuildServices(query);
        McpResourceDescriptor descriptor = Descriptor();
        FrontComposerMcpResource resource = new(descriptor);

        ReadResourceResult result = await resource.ReadAsync(
            Request(provider, descriptor.ProtocolUri),
            TestContext.Current.CancellationToken);

        TextResourceContents text = result.Contents.Single().ShouldBeOfType<TextResourceContents>();
        text.Uri.ShouldBe(descriptor.ProtocolUri);
        text.MimeType.ShouldBe("text/markdown");
        text.Text.ShouldContain("# Invoices");
        text.Text.ShouldContain("INV-1");
        query.Request.ShouldNotBeNull();
        query.Request!.TenantId.ShouldBe("tenant-a");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a uri")]
    [InlineData("frontcomposer://Billing/projections/MissingProjection?token=eyJabc.def#secret")]
    public async Task ReadAsync_InvalidRequests_ReturnSanitizedPlainTextWithoutRawUri(string? uri) {
        RecordingQueryService query = new();
        await using ServiceProvider provider = BuildServices(query);
        McpResourceDescriptor descriptor = Descriptor();
        FrontComposerMcpResource resource = new(descriptor);

        ReadResourceResult result = await resource.ReadAsync(
            Request(provider, uri),
            TestContext.Current.CancellationToken);

        TextResourceContents text = result.Contents.Single().ShouldBeOfType<TextResourceContents>();
        text.Uri.ShouldBe(descriptor.ProtocolUri);
        text.MimeType.ShouldBe("text/plain");
        text.Text.ShouldNotContain("eyJabc");
        text.Text.ShouldNotContain("secret");
        text.Text.ShouldNotContain("MissingProjection");
        query.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task ReadAsync_MissingRequestServices_ReturnsSanitizedFailure() {
        McpResourceDescriptor descriptor = Descriptor();
        FrontComposerMcpResource resource = new(descriptor);

        ReadResourceResult result = await resource.ReadAsync(
            Request(null, descriptor.ProtocolUri),
            TestContext.Current.CancellationToken);

        TextResourceContents text = result.Contents.Single().ShouldBeOfType<TextResourceContents>();
        text.Uri.ShouldBe(descriptor.ProtocolUri);
        text.MimeType.ShouldBe("text/plain");
        text.Text.ShouldBe("Request failed.");
    }

    [Fact]
    public async Task ReadAsync_HiddenProjection_ReturnsSanitizedUnknownResourceAtAdapterEdge() {
        RecordingQueryService query = new();
        await using ServiceProvider provider = BuildServices(
            query,
            services => services.AddSingleton<IFrontComposerMcpResourceVisibilityGate>(new DenyingResourceVisibilityGate()));
        McpResourceDescriptor descriptor = Descriptor();
        FrontComposerMcpResource resource = new(descriptor);

        ReadResourceResult result = await resource.ReadAsync(
            Request(provider, descriptor.ProtocolUri),
            TestContext.Current.CancellationToken);

        TextResourceContents text = result.Contents.Single().ShouldBeOfType<TextResourceContents>();
        text.Uri.ShouldBe(descriptor.ProtocolUri);
        text.MimeType.ShouldBe("text/plain");
        text.Text.ShouldBe("Projection resource is not available.");
        text.Text.ShouldNotContain("tenant-a");
        text.Text.ShouldNotContain("InvoiceProjection");
        query.CallCount.ShouldBe(0);
    }

    [Fact]
    public void ProtocolResource_AdvertisesExactCanonicalDescriptorAndRejectsTemplates() {
        McpResourceDescriptor descriptor = Descriptor();
        FrontComposerMcpResource resource = new(descriptor);

        resource.ProtocolResource.Uri.ShouldBe(descriptor.ProtocolUri);
        resource.ProtocolResource.MimeType.ShouldBe("text/markdown");
        resource.Metadata.Single().ShouldBeSameAs(descriptor);
        resource.IsMatch(descriptor.ProtocolUri).ShouldBeTrue();
        resource.IsMatch(descriptor.ProtocolUri.ToLowerInvariant()).ShouldBeFalse();
        Should.Throw<NotSupportedException>(() => _ = resource.ProtocolResourceTemplate)
            .Message.ShouldContain("resource templates");
    }

    private static ServiceProvider BuildServices(
        IQueryService queryService,
        Action<IServiceCollection>? configureServices = null) {
        ServiceCollection services = [];
        services.AddSingleton(queryService);
        services.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(Manifest()));
        services.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        services.AddScoped<IFrontComposerMcpAgentContextAccessor, StaticAgentContextAccessor>();
        services.AddScoped<FrontComposerMcpProjectionReader>();
        services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
        configureServices?.Invoke(services);
        return services.BuildServiceProvider();
    }

    private static RequestContext<ReadResourceRequestParams> Request(
        IServiceProvider? services,
        string? uri) {
        var request = new JsonRpcRequest {
            Id = new RequestId("test-read"),
            Method = RequestMethods.ResourcesRead,
        };
        var context = new RequestContext<ReadResourceRequestParams>(
            Substitute.For<McpServer>(),
            request,
            new ReadResourceRequestParams {
                Uri = uri!,
            }) {
            Services = services,
        };
        return context;
    }

    private static McpManifest Manifest()
        => new("frontcomposer.mcp.v1", [], [Descriptor()]);

    private static McpResourceDescriptor Descriptor()
        => new(
            "frontcomposer://Billing/projections/InvoiceProjection",
            "InvoiceProjection",
            typeof(InvoiceProjection).FullName!,
            "Billing",
            "Invoices",
            "Invoices",
            [
                new McpParameterDescriptor("Number", "String", "string", true, false, "Number", null, [], false),
                new McpParameterDescriptor("Amount", "Int32", "number", true, false, "Amount", null, [], false),
            ]);

    public sealed record InvoiceProjection(string Number, int Amount);

    private sealed class RecordingQueryService : IQueryService {
        public QueryRequest? Request { get; private set; }

        public int CallCount { get; private set; }

        public Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default) {
            Request = request;
            CallCount++;
            object[] items = [new InvoiceProjection("INV-1", 42)];
            return Task.FromResult(new QueryResult<T>(items.Cast<T>().ToArray(), 1, null));
        }
    }

    private sealed class StaticAgentContextAccessor : IFrontComposerMcpAgentContextAccessor {
        public FrontComposerMcpAgentContext GetContext()
            => new("tenant-a", "agent-a", new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "test")));
    }

    private sealed class DenyingResourceVisibilityGate : IFrontComposerMcpResourceVisibilityGate {
        public ValueTask<bool> IsVisibleAsync(
            McpResourceDescriptor descriptor,
            FrontComposerMcpAgentContext context,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(false);
    }
}
