using System.Security.Claims;

using Bunit;

using Hexalith.FrontComposer.Shell.Components.Layout;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

public sealed class FcAnyAuthorizedTests : BunitContext
{
    private static readonly string[] _oldPolicy = ["old"];
    private static readonly string[] _newPolicy = ["new"];

    [Fact]
    public async Task SupersededPolicyEvaluationCannotRevealAnOldMenu()
    {
        DelayedAuthorization authorization = new();
        Services.AddSingleton<IAuthorizationService>(authorization);
        Task<AuthenticationState> state = Task.FromResult(new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity("test"))));

        IRenderedComponent<CascadingValue<Task<AuthenticationState>>> wrapper =
            Render<CascadingValue<Task<AuthenticationState>>>(parameters => parameters
                .Add(component => component.Value, state)
                .Add(component => component.ChildContent, builder =>
                {
                    builder.OpenComponent<FcAnyAuthorized>(0);
                    builder.AddAttribute(1, nameof(FcAnyAuthorized.Policies), _oldPolicy);
                    builder.AddAttribute(2, nameof(FcAnyAuthorized.ChildContent),
                        (RenderFragment)(content => content.AddMarkupContent(0, "<span data-testid='menu'>More</span>")));
                    builder.CloseComponent();
                }));
        IRenderedComponent<FcAnyAuthorized> menu = wrapper.FindComponent<FcAnyAuthorized>();

        menu.Render(parameters => parameters.Add(component => component.Policies, _newPolicy));
        authorization.CompleteOld();
        await Task.Yield();

        wrapper.FindAll("[data-testid='menu']").ShouldBeEmpty();
    }

    private sealed class DelayedAuthorization : IAuthorizationService
    {
        private readonly TaskCompletionSource<AuthorizationResult> _old =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void CompleteOld() => _old.TrySetResult(AuthorizationResult.Success());

        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user, object? resource, string policyName)
            => policyName == "old" ? _old.Task : Task.FromResult(AuthorizationResult.Failed());

        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
            => Task.FromResult(AuthorizationResult.Failed());
    }
}
