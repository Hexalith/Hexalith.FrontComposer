using System.Security.Claims;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp.Invocation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Invocation;

public sealed class ProjectionReaderTests {
    [Fact]
    public async Task ReadAsync_RoutesToQueryService_WithTenantContext_AndMarkdown() {
        RecordingQueryService query = new();
        ServiceProvider provider = Services(query).BuildServiceProvider();
        FrontComposerMcpProjectionReader reader = ActivatorUtilities.CreateInstance<FrontComposerMcpProjectionReader>(provider);

        FrontComposerMcpResult result = await reader.ReadAsync("frontcomposer://Billing/projections/InvoiceProjection", TestContext.Current.CancellationToken);

        result.IsError.ShouldBeFalse();
        query.Request!.TenantId.ShouldBe("tenant-a");
        query.Request.Criteria.ProjectionType.ShouldBe(typeof(InvoiceProjection).FullName);
        query.Request.Criteria.Take.ShouldBe(50);
        result.Text.ShouldContain("# Invoices");
        result.Text.ShouldContain("| Number | Amount |");
        result.Text.ShouldContain("INV-1");
    }

    [Fact]
    public async Task ReadAsync_EmptyProjection_UsesVisibleCatalogForCtaSuggestions() {
        ServiceCollection services = [];
        _ = services.AddLogging();
        _ = services.AddSingleton<IQueryService>(new EmptyQueryService());
        _ = services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        _ = services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
        _ = services.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(ManifestWithCreateCommand()));
        _ = services.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        _ = services.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new StaticAgentContextAccessor());
        _ = services.AddScoped<FrontComposerMcpToolAdmissionService>();
        _ = services.AddScoped<FrontComposerMcpProjectionReader>();
        ServiceProvider provider = services.BuildServiceProvider();
        FrontComposerMcpProjectionReader reader = provider.GetRequiredService<FrontComposerMcpProjectionReader>();

        FrontComposerMcpResult result = await reader.ReadAsync("frontcomposer://Billing/projections/InvoiceProjection", TestContext.Current.CancellationToken);

        result.IsError.ShouldBeFalse();
        result.Text.ShouldContain("No invoices found.");
        result.Text.ShouldContain("- Create invoice");
        // The empty-state suggestion list emits bullets directly under the empty-state line;
        // the previous "Suggestions:" inline label was not part of the canonical document grammar.
        result.Text.ShouldNotContain("Suggestions:");
    }

    [Fact]
    public async Task ReadAsync_QueryCatchAll_LogsSanitizedFailure() {
        CapturingLogger<FrontComposerMcpProjectionReader> logger = new();
        IServiceCollection services = Services(new ThrowingQueryService());
        _ = services.AddSingleton<ILogger<FrontComposerMcpProjectionReader>>(logger);
        ServiceProvider provider = services.BuildServiceProvider();
        FrontComposerMcpProjectionReader reader = provider.GetRequiredService<FrontComposerMcpProjectionReader>();

        FrontComposerMcpResult result = await reader.ReadAsync("frontcomposer://Billing/projections/InvoiceProjection", TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.DownstreamFailed);
        LogEntry entry = logger.Entries.Single();
        entry.Message.ShouldContain("MCP projection reader failed closed");
        entry.Message.ShouldContain(FrontComposerMcpFailureCategory.DownstreamFailed.ToString());
        entry.Message.ShouldContain(nameof(InvalidOperationException));
        entry.Message.ShouldNotContain("frontcomposer://Billing/projections/InvoiceProjection");
        entry.Message.ShouldNotContain("tenant-a");
        entry.Message.ShouldNotContain("agent-a");
        entry.Message.ShouldNotContain("raw query failure");
        entry.Exception.ShouldBeNull();
    }

    private static IServiceCollection Services(IQueryService queryService) {
        var services = new ServiceCollection();
        _ = services.AddSingleton(queryService);
        _ = services.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(Manifest()));
        _ = services.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        _ = services.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new StaticAgentContextAccessor());
        _ = services.AddScoped<FrontComposerMcpProjectionReader>();
        _ = services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
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

    private sealed class ThrowingQueryService : IQueryService {
        public Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException(
                "raw query failure for frontcomposer://Billing/projections/InvoiceProjection tenant-a agent-a");
    }

    private sealed class StaticAgentContextAccessor : IFrontComposerMcpAgentContextAccessor {
        public FrontComposerMcpAgentContext GetContext()
            => new("tenant-a", "agent-a", new ClaimsPrincipal(new ClaimsIdentity("test")));
    }

    private sealed class CapturingLogger<T> : ILogger<T> {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class NullScope : IDisposable {
        public static readonly NullScope Instance = new();

        public void Dispose() {
        }
    }
}
