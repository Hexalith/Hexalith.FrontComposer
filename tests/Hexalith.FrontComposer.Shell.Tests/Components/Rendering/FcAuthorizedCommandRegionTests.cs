using System.Security.Claims;

using Bunit;

using Hexalith.FrontComposer.Shell.Components.Rendering;
using Hexalith.FrontComposer.Shell.Services.Authorization;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

public sealed class FcAuthorizedCommandRegionTests : BunitContext {
    private sealed class ProtectedCommand { }

    private readonly ControllableAuthenticationStateProvider _authStateProvider = new();
    private readonly ControllableCommandAuthorizationEvaluator _evaluator = new();

    public FcAuthorizedCommandRegionTests() {
        Services.AddSingleton<AuthenticationStateProvider>(_authStateProvider);
        Services.AddSingleton<ICommandAuthorizationEvaluator>(_evaluator);
    }

    [Fact]
    public void Render_PendingDecision_RendersPendingBranch() {
        TaskCompletionSource<CommandAuthorizationDecision> pending = NewDecisionSource();
        _evaluator.Enqueue(pending.Task);

        IRenderedComponent<FcAuthorizedCommandRegion> cut = RenderRegion();

        cut.Markup.ShouldContain("auth-pending");
        cut.Markup.ShouldNotContain("auth-allowed");
        cut.Markup.ShouldNotContain("auth-denied");
    }

    [Fact]
    public void Render_AllowedDecision_RendersAuthorizedBranch() {
        _evaluator.Enqueue(Task.FromResult(CommandAuthorizationDecision.Allowed("corr-allowed")));

        IRenderedComponent<FcAuthorizedCommandRegion> cut = RenderRegion();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("auth-allowed"));
        cut.Markup.ShouldNotContain("auth-pending");
        cut.Markup.ShouldNotContain("auth-denied");
    }

    [Fact]
    public void Render_DeniedDecision_RendersNotAuthorizedBranch() {
        _evaluator.Enqueue(Task.FromResult(CommandAuthorizationDecision.Denied("corr-denied")));

        IRenderedComponent<FcAuthorizedCommandRegion> cut = RenderRegion();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("auth-denied"));
        cut.Markup.ShouldNotContain("auth-allowed");
    }

    [Fact]
    public void Render_FailedClosedDecision_RendersNotAuthorizedBranch() {
        _evaluator.Enqueue(Task.FromResult(CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.HandlerFailed, "corr-blocked")));

        IRenderedComponent<FcAuthorizedCommandRegion> cut = RenderRegion();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("auth-denied"));
        cut.Markup.ShouldNotContain("auth-allowed");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Render_MissingPolicy_RendersNotAuthorizedAndDoesNotCallEvaluator(string? policyName) {
        IRenderedComponent<FcAuthorizedCommandRegion> cut = RenderRegion(policyName: policyName);

        cut.Markup.ShouldContain("auth-denied");
        _evaluator.Requests.ShouldBeEmpty();
    }

    [Fact]
    public void Render_EvaluatorThrows_FailsClosedWithoutEscaping() {
        _evaluator.Enqueue(Task.FromException<CommandAuthorizationDecision>(new InvalidOperationException("broken evaluator")));

        IRenderedComponent<FcAuthorizedCommandRegion> cut = RenderRegion();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("auth-denied"));
        cut.Markup.ShouldNotContain("auth-allowed");
    }

    [Fact]
    public void Render_EvaluatorReturnsNull_FailsClosedWithoutEscaping() {
        _evaluator.Enqueue(Task.FromResult<CommandAuthorizationDecision>(null!));

        IRenderedComponent<FcAuthorizedCommandRegion> cut = RenderRegion();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("auth-denied"));
        cut.Markup.ShouldNotContain("auth-allowed");
    }

    [Fact]
    public void AuthenticationStateChanged_ReevaluatesAndDropsStaleCompletion() {
        TaskCompletionSource<CommandAuthorizationDecision> first = NewDecisionSource();
        TaskCompletionSource<CommandAuthorizationDecision> second = NewDecisionSource();
        _evaluator.Enqueue(first.Task);
        _evaluator.Enqueue(second.Task);

        IRenderedComponent<FcAuthorizedCommandRegion> cut = RenderRegion();
        cut.WaitForAssertion(() => _evaluator.Requests.Count.ShouldBe(1));

        _authStateProvider.TriggerAuthenticatedUser("second-user");
        cut.WaitForAssertion(() => _evaluator.Requests.Count.ShouldBe(2));

        first.SetResult(CommandAuthorizationDecision.Denied("corr-stale"));
        cut.Markup.ShouldContain("auth-pending");
        cut.Markup.ShouldNotContain("auth-denied");

        second.SetResult(CommandAuthorizationDecision.Allowed("corr-fresh"));
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("auth-allowed"));
        cut.Markup.ShouldNotContain("auth-denied");
    }

    private IRenderedComponent<FcAuthorizedCommandRegion> RenderRegion(string? policyName = "Orders.Approve")
        => Render<FcAuthorizedCommandRegion>(parameters => parameters
            .Add(p => p.CommandType, typeof(ProtectedCommand))
            .Add(p => p.PolicyName, policyName!)
            .Add(p => p.BoundedContext, "Orders")
            .Add(p => p.DisplayLabel, "Approve order")
            .Add(p => p.Pending, builder => builder.AddMarkupContent(0, "<span class=\"auth-pending\">pending</span>"))
            .Add(p => p.Authorized, builder => builder.AddMarkupContent(0, "<button class=\"auth-allowed\">allowed</button>"))
            .Add(p => p.NotAuthorized, builder => builder.AddMarkupContent(0, "<span class=\"auth-denied\">denied</span>")));

    private static TaskCompletionSource<CommandAuthorizationDecision> NewDecisionSource()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class ControllableCommandAuthorizationEvaluator : ICommandAuthorizationEvaluator {
        private readonly Queue<Task<CommandAuthorizationDecision>> _decisions = new();

        public List<CommandAuthorizationRequest> Requests { get; } = [];

        public void Enqueue(Task<CommandAuthorizationDecision> decision) => _decisions.Enqueue(decision);

        public Task<CommandAuthorizationDecision> EvaluateAsync(
            CommandAuthorizationRequest request,
            CancellationToken cancellationToken = default) {
            Requests.Add(request);
            return _decisions.Count == 0
                ? Task.FromResult(CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.HandlerFailed, "corr-empty"))
                : _decisions.Dequeue();
        }
    }

    private sealed class ControllableAuthenticationStateProvider : AuthenticationStateProvider {
        private AuthenticationState _state = AuthenticatedState("first-user");

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(_state);

        public void TriggerAuthenticatedUser(string name) {
            _state = AuthenticatedState(name);
            NotifyAuthenticationStateChanged(Task.FromResult(_state));
        }

        private static AuthenticationState AuthenticatedState(string name)
            => new(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, name)], "Test")));
    }
}
