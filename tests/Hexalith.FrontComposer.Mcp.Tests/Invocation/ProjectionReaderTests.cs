using System.Security.Claims;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp.Invocation;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Invocation;

public sealed class ProjectionReaderTests {
    [Fact]
    public async Task ReadAsync_RoutesToQueryService_WithTenantContext_AndMarkdown() {
        RecordingQueryService query = new();
        ServiceProvider provider = Services(query).BuildServiceProvider();
        var reader = ActivatorUtilities.CreateInstance<FrontComposerMcpProjectionReader>(provider);

        FrontComposerMcpResult result = await reader.ReadAsync("frontcomposer://Billing/projections/InvoiceProjection", TestContext.Current.CancellationToken);

        result.IsError.ShouldBeFalse();
        query.Request!.TenantId.ShouldBe("tenant-a");
        result.Text.ShouldContain("# Invoices");
        result.Text.ShouldContain("| Number | Amount |");
        result.Text.ShouldContain("INV-1");
    }

    [Fact]
    public async Task ReadAsync_EmptyProjection_UsesVisibleCatalogForCtaSuggestions() {
        ServiceCollection services = [];
        services.AddLogging();
        services.AddSingleton<IQueryService>(new EmptyQueryService());
        services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        services.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(ManifestWithCreateCommand()));
        services.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        services.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new StaticAgentContextAccessor());
        services.AddScoped<FrontComposerMcpToolAdmissionService>();
        services.AddScoped<FrontComposerMcpProjectionReader>();
        ServiceProvider provider = services.BuildServiceProvider();
        FrontComposerMcpProjectionReader reader = provider.GetRequiredService<FrontComposerMcpProjectionReader>();

        FrontComposerMcpResult result = await reader.ReadAsync("frontcomposer://Billing/projections/InvoiceProjection", TestContext.Current.CancellationToken);

        result.IsError.ShouldBeFalse();
        result.Text.ShouldContain("No invoices found.");
        result.Text.ShouldContain("Suggestions:");
        result.Text.ShouldContain("- Create invoice");
    }

    private static IServiceCollection Services(IQueryService queryService) {
        var services = new ServiceCollection();
        services.AddSingleton(queryService);
        services.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(Manifest()));
        services.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        services.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new StaticAgentContextAccessor());
        services.AddScoped<FrontComposerMcpProjectionReader>();
        return services;
    }

    private static McpManifest Manifest()
        => new("frontcomposer.mcp.v1", [], [
            new McpResourceDescriptor(
                "frontcomposer://Billing/projections/InvoiceProjection",
                "InvoiceProjection",
                typeof(InvoiceProjection).FullName!,
                "Billing",
                "Invoices",
                "Invoices",
                [
                    new McpParameterDescriptor("Number", "String", "string", true, false, "Number", null, [], false),
                    new McpParameterDescriptor("Amount", "Int32", "number", true, false, "Amount", null, [], false),
                ]),
        ]);

    private static McpManifest ManifestWithCreateCommand()
        => new(
            "frontcomposer.mcp.v1",
            [
                new McpCommandDescriptor(
                    "Billing.CreateInvoiceCommand.Execute",
                    "Billing.CreateInvoiceCommand",
                    "Billing",
                    "Create invoice",
                    null,
                    null,
                    [],
                    []),
            ],
            [
                new McpResourceDescriptor(
                    "frontcomposer://Billing/projections/InvoiceProjection",
                    "InvoiceProjection",
                    typeof(InvoiceProjection).FullName!,
                    "Billing",
                    "Invoices",
                    "Invoices",
                    [
                        new McpParameterDescriptor("Number", "String", "string", true, false, "Number", null, [], false),
                        new McpParameterDescriptor("Amount", "Int32", "number", true, false, "Amount", null, [], false),
                    ],
                    EntityPluralLabel: "Invoices",
                    EmptyStateCtaCommandName: "CreateInvoiceCommand"),
            ]);

    public sealed class InvoiceProjection {
        public string Number { get; set; } = "";
        public int Amount { get; set; }
    }

    private sealed class RecordingQueryService : IQueryService {
        public QueryRequest? Request { get; private set; }

        public Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default) {
            Request = request;
            object[] items = [new InvoiceProjection { Number = "INV-1", Amount = 42 }];
            return Task.FromResult(new QueryResult<T>(items.Cast<T>().ToArray(), 1, null));
        }
    }

    private sealed class EmptyQueryService : IQueryService {
        public Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new QueryResult<T>([], 0, null));
    }

    private sealed class StaticAgentContextAccessor : IFrontComposerMcpAgentContextAccessor {
        public FrontComposerMcpAgentContext GetContext()
            => new("tenant-a", "agent-a", new ClaimsPrincipal(new ClaimsIdentity("test")));
    }
}
