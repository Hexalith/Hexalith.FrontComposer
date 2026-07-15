using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Contracts.Schema;
using Hexalith.FrontComposer.Mcp.Invocation;
using Hexalith.FrontComposer.Mcp.Schema;
using Hexalith.FrontComposer.Mcp.Tests.Logging;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Invocation;

/// <summary>
/// AC1 / AC5 / T3 — wire McpSchemaNegotiator into FrontComposerMcpCommandInvoker. The schema gate
/// runs between admission resolution and command dispatch. Sanitized failure categories surface
/// through the command failure adapter; CompatibleAdditive admits dispatch only after server-side
/// validation/defaulting/bounds re-run.
/// </summary>
public sealed class CommandInvokerSchemaGateTests {
    [Fact]
    public async Task SchemaMismatch_OnCommand_ReturnsSanitizedSchemaCategory_WithoutDispatching() {
        RecordingCommandService dispatcher = new();
        CountingUlidFactory ulids = new();
        CapturingLogger<SchemaNegotiationRuntimeGate> logger = new();
        PayInvoiceCommand.ResetConstructionCount();
        FrontComposerMcpCommandInvoker invoker = BuildInvoker(
            dispatcher,
            clientFingerprintHint: SchemaHintFor("stale-client"),
            ulidFactory: ulids,
            schemaLogger: logger);

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":1}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.SchemaMismatch);
        _ = result.StructuredContent.ShouldNotBeNull();
        result.StructuredContent!["category"]!.GetValue<string>().ShouldBe("schema-mismatch");
        result.StructuredContent!["docsCode"]!.GetValue<string>().ShouldStartWith("HFC-SCHEMA-");
        dispatcher.Dispatched.ShouldBeNull("AC1: schema-mismatch must short-circuit before command dispatch.");
        ulids.CallCount.ShouldBe(0, "AC1: schema-mismatch must short-circuit before server-side ULID allocation.");
        PayInvoiceCommand.ConstructionCount.ShouldBe(0, "AC1: schema-mismatch must short-circuit before command construction.");
        CapturedLogEntry entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Information);
        entry.EventId.Id.ShouldBe(8318);
        entry.EventId.Name.ShouldBe("McpSchemaDecision");
        entry.Exception.ShouldBeNull();
        entry.State.Keys.OrderBy(static key => key, StringComparer.Ordinal).ShouldBe([
            "Category",
            "DecisionKind",
            "DocsCode",
            "MessageKey",
            "{OriginalFormat}",
        ]);
        entry.Message.ShouldNotContain("stale-client");
    }

    [Fact]
    public async Task UnknownBaseline_OnCommand_BlocksDispatch() {
        RecordingCommandService dispatcher = new();
        FrontComposerMcpCommandInvoker invoker = BuildInvoker(
            dispatcher,
            clientFingerprintHint: SchemaHintFor("baseline-missing"));

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":1}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.UnknownSchemaBaseline);
        dispatcher.Dispatched.ShouldBeNull();
    }

    [Fact]
    public async Task CompatibleAdditive_OnCommand_AdmitsDispatch_AfterRevalidation() {
        // AC5: validation/defaulting/bounds re-run on CompatibleAdditive before any side effect.
        // The command type clamps Amount to [1, 100]; an out-of-bounds caller value (200) must be
        // either rejected or normalized — never silently bypass into the dispatcher untouched.
        // CK4-P1: send 200 (clearly above the [1,100] clamp range) so a SUT that skipped
        // revalidation entirely fails this test (it would dispatch Amount=200).
        RecordingCommandService dispatcher = new();
        FrontComposerMcpCommandInvoker invoker = BuildInvoker(
            dispatcher,
            clientFingerprintHint: SchemaHintFor("compatible-additive"));

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":200}"""),
            TestContext.Current.CancellationToken);

        // AC5 either path is acceptable: full validation rejection, or clamped/normalized dispatch.
        // What is NOT acceptable: silent dispatch with the out-of-bounds value preserved.
        if (result.IsError) {
            result.Category.ShouldBeOneOf(
                FrontComposerMcpFailureCategory.ValidationFailed,
                FrontComposerMcpFailureCategory.MalformedRequest);
            dispatcher.Dispatched.ShouldBeNull();
        }
        else {
            _ = dispatcher.Dispatched.ShouldNotBeNull();
            var command = (PayInvoiceCommand)dispatcher.Dispatched!;
            command.Amount.ShouldBeInRange(1, 100,
                "AC5: post-additive validation/defaulting must clamp/normalize bounds — never silently pass an out-of-bounds caller value through to the dispatcher.");
        }
    }

    private static SchemaFingerprint SchemaHintFor(string scenario)
        => new(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, scenario.PadRight(64, 'x')[..64]);

    private static Dictionary<string, JsonElement> Args(string json)
        => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    private static FrontComposerMcpCommandInvoker BuildInvoker(
        ICommandService dispatcher,
        SchemaFingerprint? clientFingerprintHint = null,
        IUlidFactory? ulidFactory = null,
        ILogger<SchemaNegotiationRuntimeGate>? schemaLogger = null) {
        ServiceCollection services = [];
        _ = services.AddSingleton(dispatcher);
        _ = services.AddSingleton(ulidFactory ?? new FixedUlidFactory());
        _ = services.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(Manifest(clientFingerprintHint)));
        _ = services.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        _ = services.AddSingleton<FrontComposerMcpToolAdmissionService>();
        _ = services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        _ = services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
        _ = services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        if (schemaLogger is not null) {
            _ = services.AddSingleton(schemaLogger);
        }
        _ = services.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new SchemaAwareStaticAccessor(clientFingerprintHint));
        ServiceProvider provider = services.BuildServiceProvider();
        return ActivatorUtilities.CreateInstance<FrontComposerMcpCommandInvoker>(provider);
    }

    private static McpManifest Manifest(SchemaFingerprint? clientFingerprintHint) {
        SchemaFingerprint? serverFingerprint = clientFingerprintHint?.Value.StartsWith("baseline-missing", StringComparison.Ordinal) == true
            ? null
            : clientFingerprintHint?.Value.StartsWith("compatible-additive", StringComparison.Ordinal) == true
                ? clientFingerprintHint
                : new SchemaFingerprint(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, "server-current".PadRight(64, 's')[..64]);
        return new("frontcomposer.mcp.v1", [
            new McpCommandDescriptor(
                "Billing.PayInvoiceCommand.Execute",
                typeof(PayInvoiceCommand).FullName!,
                "Billing",
                "Pay invoice",
                "Pay invoice",
                null,
                [new McpParameterDescriptor("Amount", "Int32", "number", true, false, "Amount", null, [], false)],
                ["TenantId", "UserId", "MessageId"],
                Fingerprint: serverFingerprint),
        ], []);
    }

    public sealed class PayInvoiceCommand {
        private static int s_constructionCount;

        public PayInvoiceCommand() => Interlocked.Increment(ref s_constructionCount);

        public static int ConstructionCount => Volatile.Read(ref s_constructionCount);

        public static void ResetConstructionCount() => Volatile.Write(ref s_constructionCount, 0);

        public string MessageId { get; set; } = "";
        public string TenantId { get; set; } = "";
        public string UserId { get; set; } = "";
        [Range(1, 100)]
        public int Amount { get; set; }
    }

    private sealed class RecordingCommandService : ICommandService {
        public object? Dispatched { get; private set; }

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class {
            Dispatched = command;
            return Task.FromResult(new CommandResult("message-a", "Accepted", "corr-a"));
        }
    }

    private sealed class FixedUlidFactory : IUlidFactory {
        private int _next;

        public string NewUlid() {
            int value = Interlocked.Increment(ref _next);
            return value switch {
                1 => "01JZ0R5K9N8W4Y7V3Q2P6C1A0C",
                2 => "01JZ0R5K9N8W4Y7V3Q2P6C1A0D",
                _ => "01JZ0R5K9N8W4Y7V3Q2P6C1A0E",
            };
        }
    }

    private sealed class CountingUlidFactory : IUlidFactory {
        private readonly FixedUlidFactory _inner = new();
        private int _callCount;

        public int CallCount => Volatile.Read(ref _callCount);

        public string NewUlid() {
            _ = Interlocked.Increment(ref _callCount);
            return _inner.NewUlid();
        }
    }

    /// <summary>
    /// Static accessor exposing a <see cref="ClientFingerprintHint"/> that T3 will plumb through
    /// to the negotiator. Until T3 lands, the hint is unused and the gate-bypassed result fails
    /// the assertions, providing meaningful red-phase signal.
    /// </summary>
    private sealed class SchemaAwareStaticAccessor(SchemaFingerprint? clientFingerprintHint) : IFrontComposerMcpAgentContextAccessor {
        public FrontComposerMcpAgentContext GetContext()
            => new(
                "tenant-a",
                "agent-a",
                new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "test", nameType: "name", roleType: "role")));

        public SchemaFingerprint? ClientFingerprintHint => clientFingerprintHint;
    }
}
