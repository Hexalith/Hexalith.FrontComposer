using System.Security.Claims;

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
/// AC1 / T3 — wire McpSchemaNegotiator into FrontComposerMcpToolAdmissionService. The schema gate
/// runs after admission resolves the tool but before any acceptance leaves the service. Listing
/// the tool catalog must remain unaffected (so agents can self-correct), but a stale client
/// fingerprint must demote the resolution to a sanitized rejection.
/// </summary>
public sealed class ToolAdmissionSchemaGateTests {
    [Fact]
    public async Task ResolveAsync_StaleClientFingerprint_RejectsWithSanitizedSchemaCategory() {
        FrontComposerMcpToolAdmissionService admission = BuildAdmission(SchemaHintFor("stale-client"));

        McpToolResolutionResult resolution = await admission.ResolveAsync(
            "Billing.PayInvoiceCommand.Execute",
            TestContext.Current.CancellationToken);

        resolution.Accepted.ShouldBeFalse(
            "AC1: a stale client schema fingerprint must demote the resolution to rejected even when the tool name resolves exactly.");
        resolution.Tool.ShouldBeNull();
        // CK4-P8: pin the sanitized failure category so a regression to UnknownTool or
        // DownstreamFailed (instead of a schema-specific category) fails the test. AC1 forbids
        // collapsing schema rejections into the generic taxonomy.
        resolution.Category.ShouldBeOneOf(
            FrontComposerMcpFailureCategory.SchemaMismatch,
            FrontComposerMcpFailureCategory.UnknownSchemaBaseline);
    }

    [Fact]
    public async Task ResolveAsync_UnsupportedAlgorithm_RejectsBeforeReturning() {
        FrontComposerMcpToolAdmissionService admission = BuildAdmission(
            new SchemaFingerprint("frontcomposer.schema.sha512.future", new string('z', 64)));

        McpToolResolutionResult resolution = await admission.ResolveAsync(
            "Billing.PayInvoiceCommand.Execute",
            TestContext.Current.CancellationToken);

        resolution.Accepted.ShouldBeFalse();
        resolution.Tool.ShouldBeNull();
        // CK4-P8: pin the sanitized failure category — the unsupported-algorithm path must surface
        // as UnsupportedSchemaAlgorithm specifically, never DownstreamFailed.
        resolution.Category.ShouldBe(
            FrontComposerMcpFailureCategory.UnsupportedSchemaAlgorithm,
            "AC1: unsupported algorithm rejection must surface the dedicated UnsupportedSchemaAlgorithm category.");
    }

    private static SchemaFingerprint SchemaHintFor(string scenario)
        => new(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, scenario.PadRight(64, 'x').Substring(0, 64));

    private static FrontComposerMcpToolAdmissionService BuildAdmission(SchemaFingerprint clientFingerprintHint) {
        ServiceCollection services = [];
        services.Configure<FrontComposerMcpOptions>(o => o.Manifests.Add(Manifest()));
        services.AddSingleton<FrontComposerMcpDescriptorRegistry>();
        services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddScoped<IFrontComposerMcpAgentContextAccessor>(_ => new SchemaAwareStaticAccessor(clientFingerprintHint));
        services.AddSingleton<FrontComposerMcpToolAdmissionService>();
        ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<FrontComposerMcpToolAdmissionService>();
    }

    private static McpManifest Manifest()
        => new("frontcomposer.mcp.v1", [
            new McpCommandDescriptor(
                "Billing.PayInvoiceCommand.Execute",
                "Hexalith.FrontComposer.Sample.Commands.PayInvoiceCommand",
                "Billing",
                "Pay invoice",
                "Pay invoice",
                null,
                [new McpParameterDescriptor("Amount", "Int32", "number", true, false, "Amount", null, [], false)],
                ["TenantId", "UserId", "MessageId"],
                Fingerprint: new SchemaFingerprint(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, "server-current".PadRight(64, 's').Substring(0, 64))),
        ], []);

    private sealed class SchemaAwareStaticAccessor(SchemaFingerprint clientFingerprintHint) : IFrontComposerMcpAgentContextAccessor {
        public FrontComposerMcpAgentContext GetContext()
            => new(
                "tenant-a",
                "agent-a",
                new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "test", nameType: "name", roleType: "role")));

        public SchemaFingerprint ClientFingerprintHint { get; } = clientFingerprintHint;
    }
}
