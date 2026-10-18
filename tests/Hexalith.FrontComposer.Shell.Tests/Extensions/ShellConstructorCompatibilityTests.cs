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
using Microsoft.Extensions.Logging.Abstractions;
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
    [InlineData(false, null, false)]
    [InlineData(true, null, false)]
    [InlineData(false, ServiceLifetime.Scoped, false)]
    [InlineData(false, ServiceLifetime.Singleton, false)]
    [InlineData(false, ServiceLifetime.Transient, false)]
    [InlineData(false, null, true)]
    [InlineData(true, ServiceLifetime.Scoped, true)]
    public async Task ShellActivationPreservesPageOriginValidationWithOptionalLocalization(
        bool useLocalization, ServiceLifetime? registeredLifetime, bool addKeyedService)
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
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton(state);
        _ = services.AddSingleton(identity);
        _ = services.AddSingleton(loader);
        if (useLocalization)
        {
            _ = services.AddLocalization();
        }
        if (registeredLifetime is { } lifetime)
        {
            services.Add(new ServiceDescriptor(typeof(LoadPageEffects), typeof(LoadPageEffects), lifetime));
        }
        LoadPageEffects keyedEffects = CreateEffects(state, loader, identity);
        if (addKeyedService)
        {
            _ = services.AddKeyedSingleton("custom", keyedEffects);
        }
        for (int initialization = 0; initialization < 2; initialization++)
        {
            _ = services.AddHexalithFrontComposer();
            ServiceDescriptor registration = services.Where(static descriptor =>
                descriptor.ServiceType == typeof(LoadPageEffects) && !descriptor.IsKeyedService).ShouldHaveSingleItem();
            registration.Lifetime.ShouldBe(registeredLifetime ?? ServiceLifetime.Scoped);
            services.Count(static descriptor => descriptor.ServiceType == typeof(LoadPageEffects) && descriptor.IsKeyedService)
                .ShouldBe(addKeyedService ? 1 : 0);
        }
        await using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        LoadPageEffects effects = scope.ServiceProvider.GetRequiredService<LoadPageEffects>();
        if (addKeyedService)
        {
            scope.ServiceProvider.GetRequiredKeyedService<LoadPageEffects>("custom").ShouldBeSameAs(keyedEffects);
        }
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
        TaskCompletionSource<ProjectionPageResult> heldRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        loader.LoadPageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(heldRead.Task);
        Task pending = effects.HandleLoadPageAsync(action with { Completion = new TaskCompletionSource<object>() }, dispatcher);
        pending.IsCompleted.ShouldBeFalse();
        identity.UserId.Returns("user-2");
        heldRead.SetResult(new ProjectionPageResult(["stale"], 1, null));
        await pending.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(true);
        dispatcher.Received().Dispatch(Arg.Any<LoadPageCancelledAction>());
        dispatcher.DidNotReceive().Dispatch(Arg.Any<LoadPageSucceededAction>());

        dispatcher.ClearReceivedCalls();
        loader.ClearReceivedCalls();
        await effects.HandleLoadPageAsync(action with { Completion = new TaskCompletionSource<object>() }, dispatcher)
            .ConfigureAwait(true);
        dispatcher.Received().Dispatch(Arg.Any<LoadPageCancelledAction>());
        await loader.DidNotReceiveWithAnyArgs().LoadPageAsync(default!, default, default, default!,
            default, default, default, TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ShellInitializationPreservesCustomFactoryOrInstanceAndKeyedServices(bool useFactory)
    {
        IState<LoadedPageState> state = Substitute.For<IState<LoadedPageState>>();
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        IUserContextAccessor identity = Substitute.For<IUserContextAccessor>();
        LoadPageEffects custom = CreateEffects(state, loader, identity);
        LoadPageEffects keyed = CreateEffects(state, loader, identity);
        ServiceDescriptor customRegistration = useFactory
            ? ServiceDescriptor.Scoped<LoadPageEffects>(_ => custom)
            : ServiceDescriptor.Singleton(custom);
        IServiceCollection services = new ServiceCollection();
        _ = services.AddScoped<LoadPageEffects>();
        services.Add(customRegistration);
        _ = services.AddKeyedSingleton("custom", keyed);

        for (int initialization = 0; initialization < 2; initialization++)
        {
            _ = services.AddHexalithFrontComposer();
            services.Where(static descriptor => descriptor.ServiceType == typeof(LoadPageEffects) && !descriptor.IsKeyedService)
                .ShouldHaveSingleItem().ShouldBeSameAs(customRegistration);
            services.Count(static descriptor => descriptor.ServiceType == typeof(LoadPageEffects) && descriptor.IsKeyedService)
                .ShouldBe(1);
        }
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<LoadPageEffects>().ShouldBeSameAs(custom);
        scope.ServiceProvider.GetRequiredKeyedService<LoadPageEffects>("custom").ShouldBeSameAs(keyed);
    }

    private static LoadPageEffects CreateEffects(
        IState<LoadedPageState> state, IProjectionPageLoader loader, IUserContextAccessor identity)
    {
        IOptionsMonitor<FcShellOptions> options = Substitute.For<IOptionsMonitor<FcShellOptions>>();
        options.CurrentValue.Returns(new FcShellOptions());
        return new LoadPageEffects(state, loader, NullLogger<LoadPageEffects>.Instance, options, null, null, identity);
    }
}
