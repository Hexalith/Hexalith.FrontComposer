using System.Reflection;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Infrastructure.PendingCommands;
using Hexalith.FrontComposer.Shell.Infrastructure.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore.FaultInjection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Extensions;

/// <summary>Pins the production DI graph for the Story 11.9 infrastructure relocations.</summary>
public sealed class RelocatedInfrastructureRegistrationTests {
    [Fact]
    public async Task QuickstartEventStoreComposition_RelocatedImplementationsAreScopedPerCircuit() {
        ServiceCollection services = CreateProductionComposition();

        AssertScopedImplementation<IProjectionFallbackRefreshScheduler, ProjectionFallbackRefreshScheduler>(services);
        AssertScopedImplementation<ProjectionFallbackPollingDriver, ProjectionFallbackPollingDriver>(services);
        AssertScopedImplementation<PendingCommandPollingDriver, PendingCommandPollingDriver>(services);

        await using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });
        await using AsyncServiceScope firstScope = provider.CreateAsyncScope();
        await using AsyncServiceScope secondScope = provider.CreateAsyncScope();

        IProjectionFallbackRefreshScheduler firstScheduler = firstScope.ServiceProvider
            .GetRequiredService<IProjectionFallbackRefreshScheduler>();
        ProjectionFallbackPollingDriver firstFallbackDriver = firstScope.ServiceProvider
            .GetRequiredService<ProjectionFallbackPollingDriver>();
        PendingCommandPollingDriver firstCommandDriver = firstScope.ServiceProvider
            .GetRequiredService<PendingCommandPollingDriver>();

        firstScheduler.ShouldBeOfType<ProjectionFallbackRefreshScheduler>();
        firstScope.ServiceProvider.GetRequiredService<IProjectionFallbackRefreshScheduler>()
            .ShouldBeSameAs(firstScheduler);
        firstScope.ServiceProvider.GetRequiredService<ProjectionFallbackPollingDriver>()
            .ShouldBeSameAs(firstFallbackDriver);
        firstScope.ServiceProvider.GetRequiredService<PendingCommandPollingDriver>()
            .ShouldBeSameAs(firstCommandDriver);

        secondScope.ServiceProvider.GetRequiredService<IProjectionFallbackRefreshScheduler>()
            .ShouldNotBeSameAs(firstScheduler);
        secondScope.ServiceProvider.GetRequiredService<ProjectionFallbackPollingDriver>()
            .ShouldNotBeSameAs(firstFallbackDriver);
        secondScope.ServiceProvider.GetRequiredService<PendingCommandPollingDriver>()
            .ShouldNotBeSameAs(firstCommandDriver);
    }

    [Fact]
    public async Task ResolvedSubscription_ReceivesRelocatedWorkers_AndScopeDisposalStopsDrivers() {
        ServiceCollection services = CreateProductionComposition();
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 7, 12, 12, 0, 0, TimeSpan.Zero));
        IPendingCommandPollingCoordinator pendingCoordinator = Substitute.For<IPendingCommandPollingCoordinator>();
        _ = pendingCoordinator.PollOnceAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(0));

        var connected = new ProjectionConnectionSnapshot(
            ProjectionConnectionStatus.Connected,
            timeProvider.GetUtcNow(),
            ReconnectAttempt: 0,
            LastFailureCategory: null);
        IProjectionConnectionState connectionState = Substitute.For<IProjectionConnectionState>();
        connectionState.Current.Returns(connected);
        IDisposable stateSubscription = Substitute.For<IDisposable>();
        _ = connectionState
            .Subscribe(Arg.Any<Action<ProjectionConnectionSnapshot>>(), Arg.Any<bool>())
            .Returns(call => {
                if (call.ArgAt<bool>(1)) {
                    call.ArgAt<Action<ProjectionConnectionSnapshot>>(0)(connected);
                }

                return stateSubscription;
            });

        FaultInjectingProjectionHubConnection hubConnection = new();
        FaultInjectingProjectionHubConnectionFactory hubFactory = new(
            hubConnection,
            new Uri("https://eventstore.test/hubs/projection-changes"));

        services.Replace(ServiceDescriptor.Singleton<TimeProvider>(timeProvider));
        services.Replace(ServiceDescriptor.Scoped(_ => pendingCoordinator));
        services.Replace(ServiceDescriptor.Scoped(_ => connectionState));
        services.Replace(ServiceDescriptor.Scoped<IProjectionHubConnectionFactory>(_ => hubFactory));

        await using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });
        AsyncServiceScope scope = provider.CreateAsyncScope();
        IServiceProvider scopedProvider = scope.ServiceProvider;

        IProjectionFallbackRefreshScheduler scheduler = scopedProvider
            .GetRequiredService<IProjectionFallbackRefreshScheduler>();
        ProjectionFallbackPollingDriver fallbackDriver = scopedProvider
            .GetRequiredService<ProjectionFallbackPollingDriver>();
        PendingCommandPollingDriver commandDriver = scopedProvider
            .GetRequiredService<PendingCommandPollingDriver>();

        connectionState.DidNotReceiveWithAnyArgs().Subscribe(default!, default);

        ProjectionSubscriptionService subscription = scopedProvider.GetRequiredService<ProjectionSubscriptionService>();

        scopedProvider.GetRequiredService<IProjectionSubscription>().ShouldBeSameAs(subscription);
        GetInjectedField<IProjectionFallbackRefreshScheduler>(subscription, "_refreshScheduler")
            .ShouldBeSameAs(scheduler);
        GetInjectedField<ProjectionFallbackPollingDriver>(subscription, "_fallbackDriver")
            .ShouldBeSameAs(fallbackDriver);
        GetInjectedField<PendingCommandPollingDriver>(subscription, "_commandPollingDriver")
            .ShouldBeSameAs(commandDriver);
        _ = connectionState.Received(1).Subscribe(
            Arg.Any<Action<ProjectionConnectionSnapshot>>(),
            Arg.Any<bool>());

        timeProvider.Advance(TimeSpan.FromSeconds(1));
        _ = pendingCoordinator.Received(1).PollOnceAsync(Arg.Any<CancellationToken>());

        await scope.DisposeAsync().ConfigureAwait(true);

        stateSubscription.Received(1).Dispose();
        timeProvider.Advance(TimeSpan.FromSeconds(2));
        _ = pendingCoordinator.Received(1).PollOnceAsync(Arg.Any<CancellationToken>());
    }

    private static void AssertScopedImplementation<TService, TImplementation>(IServiceCollection services)
        where TService : class
        where TImplementation : class, TService {
        ServiceDescriptor descriptor = services.Last(service => service.ServiceType == typeof(TService));

        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        descriptor.ImplementationType.ShouldBe(typeof(TImplementation));
    }

    private static ServiceCollection CreateProductionComposition() {
        ServiceCollection services = [];
        _ = services.AddHexalithFrontComposerQuickstart();
        _ = services.AddHexalithEventStore(options => {
            options.BaseAddress = new Uri("https://eventstore.test");
            options.RequireAccessToken = false;
        });
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        return services;
    }

    private static T GetInjectedField<T>(ProjectionSubscriptionService subscription, string fieldName)
        where T : class {
        FieldInfo field = typeof(ProjectionSubscriptionService).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Expected injection field '{fieldName}' was not found.");

        object value = field.GetValue(subscription)
            ?? throw new InvalidOperationException($"Injection field '{fieldName}' was unexpectedly null.");
        return value.ShouldBeAssignableTo<T>();
    }
}
