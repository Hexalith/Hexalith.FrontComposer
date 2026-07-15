using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Authorization;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Authorization;

public sealed class FrontComposerAuthorizationPolicyCatalogValidatorTests {
    [Fact]
    public async Task StartAsync_NoCatalog_DoesNotFail() {
        FrontComposerAuthorizationPolicyCatalogValidator sut = Create(new FrontComposerAuthorizationOptions());

        await sut.StartAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task StartAsync_EmptyCatalogWithProtectedCommands_WarnsWithoutCommandMetadata() {
        TestLogger<FrontComposerAuthorizationPolicyCatalogValidator> logger = new();
        FrontComposerAuthorizationPolicyCatalogValidator sut = Create(new FrontComposerAuthorizationOptions(), logger);

        await sut.StartAsync(TestContext.Current.CancellationToken);

        (LogLevel _, EventId eventId, IReadOnlyDictionary<string, object?> state, string warning) = logger.Messages.Single(m => m.Level == LogLevel.Warning);
        eventId.Id.ShouldBe(5682);
        state["DeclaredPolicyCount"].ShouldBe(1);
        warning.ShouldContain("catalog is empty");
        warning.ShouldNotContain("Orders.ApproveOrderCommand");
        warning.ShouldNotContain("tenant-a");
        warning.ShouldNotContain("user-a");
        warning.ShouldNotContain("server-token");
    }

    [Fact]
    public async Task StartAsync_NonStrictMissingPolicy_WarnsWithCountOnly() {
        TestLogger<FrontComposerAuthorizationPolicyCatalogValidator> logger = new();
        FrontComposerAuthorizationOptions options = new();
        options.KnownPolicies.Add("OtherPolicy");
        FrontComposerAuthorizationPolicyCatalogValidator sut = Create(options, logger);

        await sut.StartAsync(TestContext.Current.CancellationToken);

        (LogLevel _, EventId eventId, IReadOnlyDictionary<string, object?> state, string warning) = logger.Messages.Single(m => m.Level == LogLevel.Warning);
        eventId.Id.ShouldBe(5683);
        state["MissingPolicyCount"].ShouldBe(1);
        warning.ShouldNotContain("OrderApprover");
        warning.ShouldNotContain("Orders.ApproveOrderCommand");
        warning.ShouldNotContain("tenant-a");
        warning.ShouldNotContain("user-a");
        warning.ShouldNotContain("server-token");
    }

    [Fact]
    public async Task StartAsync_StrictCatalogMissingPolicy_FailsClosedWithSanitizedPolicyNamesOnly() {
        // Pass 3 DN-7-3-3-4 — the strict-mode exception payload includes the missing policy NAMES only.
        // Command FQNs are deliberately omitted so that orchestration logs cannot leak command identifiers
        // for adopters whose policy names happen to be PII-free but whose command FQNs encode customer data.
        FrontComposerAuthorizationOptions options = new() { StrictPolicyCatalogValidation = true };
        options.KnownPolicies.Add("OtherPolicy");
        FrontComposerAuthorizationPolicyCatalogValidator sut = Create(options);

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            () => sut.StartAsync(TestContext.Current.CancellationToken));

        ex.Message.ShouldContain("OrderApprover");
        ex.Message.ShouldNotContain("Orders.ApproveOrderCommand");
        ex.Message.ShouldNotContain("tenant-a");
        ex.Message.ShouldNotContain("user-a");
    }

    [Fact]
    public async Task StartAsync_CatalogContainsPolicy_DoesNotFail() {
        FrontComposerAuthorizationOptions options = new();
        options.KnownPolicies.Add("OrderApprover");
        FrontComposerAuthorizationPolicyCatalogValidator sut = Create(options);

        await sut.StartAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task StartAsync_NullKnownPolicies_TreatedAsEmpty_DoesNotNullReference() {
        // Pass 3 (E3) — `"KnownPolicies": null` in appsettings binds the property to null. Validator
        // must coalesce to an empty enumerable rather than NRE during host startup.
        FrontComposerAuthorizationOptions options = new() { KnownPolicies = null! };
        FrontComposerAuthorizationPolicyCatalogValidator sut = Create(options);

        await sut.StartAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task StartAsync_StrictCatalogWithNullKnownPolicies_DoesNotThrow() {
        // Pass 3 (E3) follow-up — even with strict mode on, a null KnownPolicies binding is treated
        // identically to an empty list (no catalog configured). The Information/Warning path runs.
        FrontComposerAuthorizationOptions options = new() {
            KnownPolicies = null!,
            StrictPolicyCatalogValidation = true,
        };
        FrontComposerAuthorizationPolicyCatalogValidator sut = Create(options);

        await sut.StartAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void CommandAuthorizationRequest_ToString_RedactsCommandPayload() {
        var request = new CommandAuthorizationRequest(
            typeof(SensitiveCommand),
            "OrderApprover",
            new SensitiveCommand { Secret = "payload-secret-server-token" },
            "Orders",
            "Approve order",
            CommandAuthorizationSurface.GeneratedForm);

        string text = request.ToString();

        text.ShouldContain("Command = <redacted>");
        text.ShouldNotContain("payload-secret-server-token");
    }

    [Fact]
    public void CommandAuthorizationResource_ToString_RedactsTenantContext() {
        var resource = new CommandAuthorizationResource(
            typeof(SensitiveCommand),
            "OrderApprover",
            "Orders",
            "Approve order",
            CommandAuthorizationSurface.DirectDispatch,
            new TenantContextSnapshot("tenant-secret-claim", "user-secret-claim", true, "corr-tenant"));

        string text = resource.ToString();

        text.ShouldContain("TenantContext = <redacted>");
        text.ShouldNotContain("tenant-secret-claim");
        text.ShouldNotContain("user-secret-claim");
    }

    private static FrontComposerAuthorizationPolicyCatalogValidator Create(
        FrontComposerAuthorizationOptions options,
        ILogger<FrontComposerAuthorizationPolicyCatalogValidator>? logger = null) {
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns([
            new DomainManifest(
                "Orders",
                "Orders",
                Projections: [],
                Commands: ["Orders.ApproveOrderCommand"],
                CommandPolicies: new Dictionary<string, string>(StringComparer.Ordinal) {
                    ["Orders.ApproveOrderCommand"] = "OrderApprover",
                }),
        ]);

        return new FrontComposerAuthorizationPolicyCatalogValidator(
            registry,
            Microsoft.Extensions.Options.Options.Create(options),
            logger ?? new TestLogger<FrontComposerAuthorizationPolicyCatalogValidator>());
    }

    private sealed class SensitiveCommand {
        public string Secret { get; set; } = string.Empty;
    }

    private sealed class TestLogger<T> : ILogger<T> {
        public List<(LogLevel Level, EventId EventId, IReadOnlyDictionary<string, object?> State, string Message)> Messages { get; } = [];

        IDisposable? ILogger.BeginScope<TState>(TState state) => NullScope.Instance;

        bool ILogger.IsEnabled(LogLevel logLevel) => true;

        void ILogger.Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            IReadOnlyDictionary<string, object?> structuredState = state is IEnumerable<KeyValuePair<string, object?>> values
                ? values.ToDictionary(static value => value.Key, static value => value.Value, StringComparer.Ordinal)
                : new Dictionary<string, object?>(StringComparer.Ordinal);
            Messages.Add((logLevel, eventId, structuredState, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable {
            public static readonly NullScope Instance = new();

            public void Dispose() {
            }
        }
    }
}
