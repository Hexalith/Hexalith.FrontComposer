using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MsOptions = Microsoft.Extensions.Options.Options;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.Tenancy;

public sealed class TenantContextValidationMatrixTests {
    [Fact]
    public void TryGetContext_ValidTenantAndUser_PreservesCaseAndReturnsSnapshot() {
        FrontComposerTenantContextAccessor sut = NewAccessor("Acme_Corp", "User-A");

        TenantContextResult result = sut.TryGetContext("Acme_Corp", "test");

        result.Succeeded.ShouldBeTrue();
        result.Context.ShouldNotBeNull();
        result.Context!.TenantId.ShouldBe("Acme_Corp");
        result.Context.UserId.ShouldBe("User-A");
        result.Context.IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public void TryGetContext_MixedCaseTenantMismatch_FailsOrdinally() {
        FrontComposerTenantContextAccessor sut = NewAccessor("Acme", "alice");

        TenantContextResult result = sut.TryGetContext("acme", "test");

        result.Succeeded.ShouldBeFalse();
        result.FailureCategory.ShouldBe(TenantContextFailureCategory.TenantMismatch);
    }

    [Theory]
    [InlineData(null, "user", TenantContextFailureCategory.TenantMissing)]
    [InlineData("   ", "user", TenantContextFailureCategory.TenantMissing)]
    [InlineData("tenant", null, TenantContextFailureCategory.UserMissing)]
    [InlineData("tenant", " ", TenantContextFailureCategory.UserMissing)]
    [InlineData("tenant:evil", "user", TenantContextFailureCategory.MalformedSegment)]
    [InlineData("tenant", "user\u0001evil", TenantContextFailureCategory.MalformedSegment)]
    public void TryGetContext_InvalidTenantOrUser_FailsClosed(
        string? tenant,
        string? user,
        TenantContextFailureCategory expected) {
        FrontComposerTenantContextAccessor sut = NewAccessor(tenant, user);

        TenantContextResult result = sut.TryGetContext(operationKind: "test");

        result.Succeeded.ShouldBeFalse();
        result.FailureCategory.ShouldBe(expected);
    }

    [Fact]
    public void TryGetContext_SyntheticTenantRejectedUnlessExplicitlyAllowed() {
        FrontComposerTenantContextAccessor production = NewAccessor("counter-demo", "demo-user");
        FrontComposerTenantContextAccessor demo = NewAccessor("counter-demo", "demo-user", allowDemo: true);

        production.TryGetContext(operationKind: "test").FailureCategory
            .ShouldBe(TenantContextFailureCategory.SyntheticTenantRejected);
        demo.TryGetContext(operationKind: "test").Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void TryGetContext_FailureLogOmitsRawTenantAndUserValues() {
        CapturingLogger<FrontComposerTenantContextAccessor> logger = new();
        FrontComposerTenantContextAccessor sut = NewAccessor(
            "tenant-secret",
            "user-secret",
            logger: logger);

        TenantContextResult result = sut.TryGetContext("tenant-other-secret", "command-dispatch");

        result.Succeeded.ShouldBeFalse();
        logger.Entries.ShouldNotBeEmpty();
        foreach (string message in logger.Entries) {
            message.ShouldNotContain("tenant-secret");
            message.ShouldNotContain("tenant-other-secret");
            message.ShouldNotContain("user-secret");
            message.ShouldContain("TenantMismatch");
            message.ShouldContain("CorrelationId=");
        }
    }

    [Fact]
    public void ManifestGate_ReturnsNoContext_WhenTenantContextInvalid() {
        ITenantScopedManifestGate gate = new TenantScopedManifestGate(NewAccessor(null, "user"));

        TenantContextResult result = gate.TryAuthorizeEnumeration();

        result.Succeeded.ShouldBeFalse();
        result.Context.ShouldBeNull();
    }

    private static FrontComposerTenantContextAccessor NewAccessor(
        string? tenant,
        string? user,
        bool allowDemo = false,
        CapturingLogger<FrontComposerTenantContextAccessor>? logger = null)
        => new(
            new TestUserContextAccessor(tenant, user),
            MsOptions.Create(new FcShellOptions { AllowDemoTenantContext = allowDemo }),
            logger ?? new CapturingLogger<FrontComposerTenantContextAccessor>());

    private sealed class TestUserContextAccessor(string? tenantId, string? userId) : IUserContextAccessor {
        public string? TenantId { get; } = tenantId;
        public string? UserId { get; } = userId;
    }

    private sealed class CapturingLogger<T> : ILogger<T> {
        public List<string> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add(formatter(state, exception));
    }
}
