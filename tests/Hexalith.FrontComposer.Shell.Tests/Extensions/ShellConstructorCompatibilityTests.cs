using System.Collections.Immutable;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Infrastructure.ProjectionConnection;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Extensions;

/// <summary>Protects published constructor signatures and context-aware Shell activation.</summary>
public sealed class ShellConstructorCompatibilityTests
{
    [Fact]
    public void PublishedSixParameterConstructorsRemainAvailable()
    {
        typeof(ProjectionFallbackRefreshScheduler).GetConstructor([
            typeof(IProjectionConnectionState), typeof(IProjectionPageLoader), typeof(IDispatcher),
            typeof(IState<LoadedPageState>), typeof(IOptionsMonitor<FcShellOptions>),
            typeof(ILogger<ProjectionFallbackRefreshScheduler>),
        ]).ShouldNotBeNull();
        System.Reflection.ConstructorInfo constructor = typeof(LoadPageEffects).GetConstructor([
            typeof(IState<LoadedPageState>), typeof(IProjectionPageLoader), typeof(ILogger<LoadPageEffects>),
            typeof(IOptionsMonitor<FcShellOptions>), typeof(TimeProvider),
            typeof(IStringLocalizer<FcShellResources>),
        ]).ShouldNotBeNull();
        constructor.GetParameters()[4].IsOptional.ShouldBeTrue();
        constructor.GetParameters()[5].IsOptional.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ShellActivationPreservesPageOriginValidationWithOptionalLocalization(bool useLocalization)
    {
        IState<LoadedPageState> state = Substitute.For<IState<LoadedPageState>>();
        state.Value.Returns(new LoadedPageState());
        IUserContextAccessor identity = Substitute.For<IUserContextAccessor>();
        identity.TenantId.Returns("acme");
        identity.UserId.Returns("user-1");
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProjectionPageResult([], 0, null)));
        ServiceCollection services = new();
        _ = services.AddSingleton(state);
        _ = services.AddSingleton(identity);
        _ = services.AddSingleton(loader);
        if (useLocalization)
        {
            _ = services.AddLocalization();
        }
        _ = services.AddHexalithFrontComposer();
        await using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        LoadPageEffects effects = scope.ServiceProvider.GetRequiredService<LoadPageEffects>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        LoadPageAction action = new("acme:OrdersProjection", 0, 20, ImmutableDictionary<string, string>.Empty,
            null, false, null, new TaskCompletionSource<object>(), TestContext.Current.CancellationToken)
        {
            OriginTenantId = "acme",
            OriginUserId = "user-1",
        };

        await effects.HandleLoadPageAsync(action, dispatcher).ConfigureAwait(true);
        dispatcher.Received().Dispatch(Arg.Is<LoadPageSucceededAction>(result => result.ViewKey == action.ViewKey));

        dispatcher.ClearReceivedCalls();
        loader.ClearReceivedCalls();
        identity.UserId.Returns("user-2");
        await effects.HandleLoadPageAsync(action with { Completion = new TaskCompletionSource<object>() }, dispatcher)
            .ConfigureAwait(true);
        dispatcher.Received().Dispatch(Arg.Any<LoadPageCancelledAction>());
        await loader.DidNotReceiveWithAnyArgs().LoadPageAsync(default!, default, default, default!,
            default, default, default, TestContext.Current.CancellationToken).ConfigureAwait(true);
    }
}
