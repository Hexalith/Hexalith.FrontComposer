using System.Security.Claims;

using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.Services.Authorization;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Authorization;

public sealed class CommandAuthorizationEvaluatorTests {
    [Fact]
    public async Task EvaluateAsync_NoPolicy_AllowsWithoutCallingAuthorizationService() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        CommandAuthorizationEvaluator sut = Create(authorization);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request(policy: null), TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
        result.Reason.ShouldBe(CommandAuthorizationReason.NoPolicy);
        await authorization.DidNotReceive().AuthorizeAsync(
            Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>());
    }

    [Fact]
    public async Task EvaluateAsync_WhitespacePolicy_TreatedAsNoPolicy() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        CommandAuthorizationEvaluator sut = Create(authorization);

        // NBSP-only is treated as whitespace and short-circuits to NoPolicy.
        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request(" "), TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
        result.Reason.ShouldBe(CommandAuthorizationReason.NoPolicy);
    }

    [Fact]
    public async Task EvaluateAsync_ProtectedCommand_Denied_FailsClosed() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(), "OrderApprover")
            .Returns(Task.FromResult(AuthorizationResult.Failed(
                AuthorizationFailure.Failed([new TestRequirement()]))));
        CommandAuthorizationEvaluator sut = Create(authorization);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.Kind.ShouldBe(CommandAuthorizationDecisionKind.Denied);
        result.Reason.ShouldBe(CommandAuthorizationReason.Denied);
    }

    [Fact]
    public async Task EvaluateAsync_ProtectedCommand_Allowed_PassesResourceWithTenantContext() {
        CommandAuthorizationResource? captured = null;
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Do<object>(r => captured = (CommandAuthorizationResource)r), "OrderApprover")
            .Returns(Task.FromResult(AuthorizationResult.Success()));
        CommandAuthorizationEvaluator sut = Create(authorization);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
        captured.ShouldNotBeNull();
        captured.CommandType.ShouldBe(typeof(TestCommand));
        captured.PolicyName.ShouldBe("OrderApprover");
        captured.BoundedContext.ShouldBe("Orders");
        captured.DisplayLabel.ShouldBe("Approve Order");
        captured.SourceSurface.ShouldBe(CommandAuthorizationSurface.DirectDispatch);
        captured.TenantContext.ShouldNotBeNull();
        captured.TenantContext.TenantId.ShouldBe("tenant-a");
    }

    [Fact]
    public async Task EvaluateAsync_UnauthenticatedPrincipal_FailsClosedBeforePolicyHandler() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        CommandAuthorizationEvaluator sut = Create(authorization, authenticated: false);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.Reason.ShouldBe(CommandAuthorizationReason.Unauthenticated);
        await authorization.DidNotReceive().AuthorizeAsync(
            Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>());
    }

    [Fact]
    public async Task EvaluateAsync_NullPrincipal_ReturnsPending() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        var stateProvider = new FakeAuthenticationStateProvider(state: null);
        CommandAuthorizationEvaluator sut = Create(authorization, stateProvider);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(CommandAuthorizationDecisionKind.Pending);
        result.Reason.ShouldBe(CommandAuthorizationReason.Pending);
    }

    [Fact]
    public async Task EvaluateAsync_AuthorizationService_ThrowsInvalidOperationException_FailsClosedAsMissingPolicy() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(), "MissingPolicy")
            .Returns<Task<AuthorizationResult>>(_ => throw new InvalidOperationException("No policy found: MissingPolicy"));
        CommandAuthorizationEvaluator sut = Create(authorization);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("MissingPolicy"), TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.Kind.ShouldBe(CommandAuthorizationDecisionKind.FailedClosed);
        result.Reason.ShouldBe(CommandAuthorizationReason.MissingPolicy);
    }

    [Fact]
    public async Task EvaluateAsync_AuthorizationService_ReturnsFailedWithEmptyRequirements_MapsToMissingPolicy() {
        // Synthesize the rare case of an AuthorizationFailure with FailCalled=false and no failed
        // requirements (a registered policy that evaluates nothing). ASP.NET's
        // AuthorizationResult.Failed() with no args sets FailCalled=true, which is Denied not
        // MissingPolicy — the only way to reach the MissingPolicy-via-Failure branch is to provide
        // an explicit AuthorizationFailure with empty requirements collection.
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(), "OrderApprover")
            .Returns(Task.FromResult(AuthorizationResult.Failed(
                AuthorizationFailure.Failed(Array.Empty<IAuthorizationRequirement>()))));
        CommandAuthorizationEvaluator sut = Create(authorization);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(CommandAuthorizationDecisionKind.FailedClosed);
        result.Reason.ShouldBe(CommandAuthorizationReason.MissingPolicy);
    }

    [Fact]
    public async Task EvaluateAsync_AuthorizationService_ReturnsNullResult_FailsClosedAsHandlerFailed() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(), "OrderApprover")
            .Returns(Task.FromResult<AuthorizationResult>(null!));
        CommandAuthorizationEvaluator sut = Create(authorization);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(CommandAuthorizationDecisionKind.FailedClosed);
        result.Reason.ShouldBe(CommandAuthorizationReason.HandlerFailed);
    }

    [Fact]
    public async Task EvaluateAsync_AuthorizationHandler_ThrowsGenericException_FailsClosedAsHandlerFailed() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(), "OrderApprover")
            .Returns<Task<AuthorizationResult>>(_ => throw new InvalidCastException("custom handler bug"));
        CommandAuthorizationEvaluator sut = Create(authorization);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(CommandAuthorizationDecisionKind.FailedClosed);
        result.Reason.ShouldBe(CommandAuthorizationReason.HandlerFailed);
    }

    [Fact]
    public async Task EvaluateAsync_AuthorizationHandler_ThrowsOperationCanceled_MapsToCanceled() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(), "OrderApprover")
            .Returns<Task<AuthorizationResult>>(_ => throw new OperationCanceledException());
        CommandAuthorizationEvaluator sut = Create(authorization);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(CommandAuthorizationDecisionKind.FailedClosed);
        result.Reason.ShouldBe(CommandAuthorizationReason.Canceled);
    }

    [Fact]
    public async Task EvaluateAsync_TenantAccessorThrows_FailsClosedAsStaleTenantContext() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        IFrontComposerTenantContextAccessor tenant = Substitute.For<IFrontComposerTenantContextAccessor>();
        tenant.TryGetContext(Arg.Any<string?>(), Arg.Any<string>())
            .Returns<TenantContextResult>(_ => throw new NotImplementedException("buggy adopter accessor"));
        CommandAuthorizationEvaluator sut = Create(authorization, tenant: tenant);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(CommandAuthorizationDecisionKind.FailedClosed);
        result.Reason.ShouldBe(CommandAuthorizationReason.StaleTenantContext);
    }

    [Fact]
    public async Task EvaluateAsync_TenantContextStale_FailsClosedAsStaleTenantContext() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        IFrontComposerTenantContextAccessor tenant = Substitute.For<IFrontComposerTenantContextAccessor>();
        tenant.TryGetContext(Arg.Any<string?>(), Arg.Any<string>())
            .Returns(TenantContextResult.Failure(TenantContextFailureCategory.SyntheticTenantRejected, "stale-corr"));
        CommandAuthorizationEvaluator sut = Create(authorization, tenant: tenant);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.Reason.ShouldBe(CommandAuthorizationReason.StaleTenantContext);
        result.CorrelationId.ShouldBe("stale-corr");
    }

    [Fact]
    public async Task EvaluateAsync_PreCancelled_ShortCircuitsAsCanceled() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        CommandAuthorizationEvaluator sut = Create(authorization);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), cts.Token);

        result.Reason.ShouldBe(CommandAuthorizationReason.Canceled);
        await authorization.DidNotReceive().AuthorizeAsync(
            Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>());
    }

    [Fact]
    public void RequestRecord_RedactsCommandPayload_FromPrintMembers() {
        // Sentinel-laden command + principal claims must never appear in the request's auto-generated
        // ToString. Records auto-format every property; PrintMembers override redacts the Command field.
        const string sentinelToken = "JWT_LIKE_SENTINEL_eyJhbGciOiJI";
        const string sentinelClaim = "sentinel-tenant-id-leak";
        var command = new TestCommand { SecretBlob = sentinelToken };
        var request = new CommandAuthorizationRequest(
            typeof(TestCommand),
            "OrderApprover",
            command,
            "Orders",
            "Approve Order",
            CommandAuthorizationSurface.GeneratedForm);

        string text = request.ToString();

        text.ShouldNotContain(sentinelToken);
        text.ShouldNotContain(sentinelClaim);
        text.ShouldContain("<redacted>");
    }

    private static CommandAuthorizationEvaluator Create(
        IAuthorizationService authorizationService,
        FakeAuthenticationStateProvider? stateProvider = null,
        IFrontComposerTenantContextAccessor? tenant = null,
        bool authenticated = true) {
        ArgumentNullException.ThrowIfNull(authorizationService);

        if (stateProvider is null) {
            ClaimsIdentity identity = authenticated
                ? new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-a")], "Test")
                : new ClaimsIdentity();
            stateProvider = new FakeAuthenticationStateProvider(new AuthenticationState(new ClaimsPrincipal(identity)));
        }

        tenant ??= DefaultTenant();

        return new CommandAuthorizationEvaluator(
            authorizationService,
            stateProvider,
            tenant,
            NullLogger<CommandAuthorizationEvaluator>.Instance);
    }

    private static IFrontComposerTenantContextAccessor DefaultTenant() {
        IFrontComposerTenantContextAccessor tenant = Substitute.For<IFrontComposerTenantContextAccessor>();
        tenant.TryGetContext(Arg.Any<string?>(), Arg.Any<string>()).Returns(
            TenantContextResult.Success(new TenantContextSnapshot("tenant-a", "user-a", true, "corr-1")));
        return tenant;
    }

    private static CommandAuthorizationRequest Request(string? policy)
        => new(typeof(TestCommand), policy, new TestCommand(), "Orders", "Approve Order");

    private sealed class TestCommand {
        public string? SecretBlob { get; set; }
    }

    private sealed class TestRequirement : IAuthorizationRequirement { }

    private sealed class FakeAuthenticationStateProvider(AuthenticationState? state) : AuthenticationStateProvider {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(state!);
    }
}
