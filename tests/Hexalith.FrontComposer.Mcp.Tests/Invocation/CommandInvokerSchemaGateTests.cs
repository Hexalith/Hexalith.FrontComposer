using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Contracts.Schema;
using Hexalith.FrontComposer.Mcp.Invocation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;
using Xunit;

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
        FrontComposerMcpCommandInvoker invoker = BuildInvoker(
            dispatcher,
            clientFingerprintHint: SchemaHintFor("stale-client"));

        FrontComposerMcpResult result = await invoker.InvokeAsync(
            "Billing.PayInvoiceCommand.Execute",
            Args("""{"Amount":1}"""),
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(FrontComposerMcpFailureCategory.SchemaMismatch);
        result.StructuredContent.ShouldNotBeNull();
        result.StructuredContent!["category"]!.GetValue<string>().ShouldBe("schema-mismatch");
        result.StructuredContent!["docsCode"]!.GetValue<string>().ShouldStartWith("HFC-SCHEMA-");
        dispatcher.Dispatched.ShouldBeNull("AC1: schema-mismatch must short-circuit before command dispatch.");
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
        } else {
            dispatcher.Dispatched.ShouldNotBeNull();
            PayInvoiceCommand command = (PayInvoiceCommand)dispatcher.Dispatched!;
            command.Amount.ShouldBeInRange(1, 100,
                "AC5: post-additive validation/defaulting must clamp/normalize bounds — never silently pass an out-of-bounds caller value through to the dispatcher.");
        }
    }

    private static SchemaFingerprint SchemaHintFor(string scenario)
        => new(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, scenario.PadRight(64, 'x').Substring(0, 64));

    private static Dictionary<string, JsonElement> Args(string json)
        => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    private static FrontComposerMcpCommandInvoker BuildInvoker(
        ICommandService dispatcher,
        SchemaFingerprint? clientFingerprintHint = null) {
        ServiceCollection services = [];
        services.AddSingleton(dispatcher);
        services.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(Manifest(clientFingerprintHint)));
        services.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        services.AddSingleton<FrontComposerMcpToolAdmissionService>();
        services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new SchemaAwareStaticAccessor(clientFingerprintHint));
        ServiceProvider provider = services.BuildServiceProvider();
        return ActivatorUtilities.CreateInstance<FrontComposerMcpCommandInvoker>(provider);
    }

    private static McpManifest Manifest(SchemaFingerprint? clientFingerprintHint) {
        SchemaFingerprint? serverFingerprint = clientFingerprintHint?.Value.StartsWith("baseline-missing", StringComparison.Ordinal) == true
            ? null
            : clientFingerprintHint?.Value.StartsWith("compatible-additive", StringComparison.Ordinal) == true
                ? clientFingerprintHint
                : new SchemaFingerprint(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, "server-current".PadRight(64, 's').Substring(0, 64));
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
