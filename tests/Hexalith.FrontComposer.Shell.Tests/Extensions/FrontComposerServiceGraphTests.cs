using Counter.Domain;

using System.Net;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Badges;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.Services.Authorization;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Extensions;

/// <summary>
/// Story 1.1 AC1 — pins the service graph the three-call bootstrap registers. Mirrors the
/// Story 1.0 spike fixture (ValidateScopes = true, <see cref="InMemoryStorageService"/> swap) so the
/// ADR-030 scoped-lifetime discipline stays enforced. These tests lock the registrations + lifetimes
/// listed in AC1 so they cannot silently regress.
/// </summary>
public sealed class FrontComposerServiceGraphTests {
    [Fact]
    public async Task Quickstart_CustomLegacyLifecycleServiceAdaptsToTypedUnknownWithoutBlockingDispatch() {
        DateTimeOffset now = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);
        ServiceCollection services = [];
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(now));
        services.AddScoped<ICommandServiceWithLifecycle, LegacyOnlyCommandService>();
        _ = services.AddHexalithFrontComposerQuickstart();
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope scope = provider.CreateScope();
        ICommandServiceWithLifecycleObservations service = scope.ServiceProvider.GetRequiredService<ICommandServiceWithLifecycleObservations>();
        List<CommandLifecycleObservation> observations = [];

        CommandResult result = await service.DispatchAsync(
            new object(),
            onLifecycleObservation: observations.Add,
            cancellationToken: TestContext.Current.CancellationToken);

        result.Status.ShouldBe("Accepted");
        observations.Single().Materiality.ShouldBe(CommandMateriality.Unknown);
        observations.Single().ObservedAt.ShouldBe(now);
    }

    [Fact]
    public void Quickstart_RegistersStubCommandPathAndCoreServices_WithCorrectLifetimes() {
        ServiceCollection services = [];
        _ = services.AddHexalithFrontComposerQuickstart();
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());

        // ADR-030 lifetimes: registry is the authoritative Singleton; storage is Scoped.
        LifetimeOf(services, typeof(IFrontComposerRegistry)).ShouldBe(ServiceLifetime.Singleton);
        LifetimeOf(services, typeof(IStorageService)).ShouldBe(ServiceLifetime.Scoped);

        // The three projection-customization registries are Singletons (immutable descriptor metadata).
        LifetimeOf(services, typeof(IProjectionSlotRegistry)).ShouldBe(ServiceLifetime.Singleton);
        LifetimeOf(services, typeof(IProjectionTemplateRegistry)).ShouldBe(ServiceLifetime.Singleton);
        LifetimeOf(services, typeof(IProjectionViewOverrideRegistry)).ShouldBe(ServiceLifetime.Singleton);

        // Badge + lifecycle services are per-circuit Scoped.
        LifetimeOf(services, typeof(IBadgeCountService)).ShouldBe(ServiceLifetime.Scoped);
        LifetimeOf(services, typeof(ILifecycleStateService)).ShouldBe(ServiceLifetime.Scoped);
        LifetimeOf(services, typeof(ILifecycleBridgeRegistry)).ShouldBe(ServiceLifetime.Scoped);

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        // Quickstart installs the stub command path (ADR-010) before any EventStore swap.
        _ = provider.GetRequiredService<IFrontComposerRegistry>().ShouldNotBeNull();
        _ = provider.GetRequiredService<IProjectionSlotRegistry>().ShouldNotBeNull();
        _ = provider.GetRequiredService<IProjectionTemplateRegistry>().ShouldNotBeNull();
        _ = provider.GetRequiredService<IProjectionViewOverrideRegistry>().ShouldNotBeNull();

        using IServiceScope scope = provider.CreateScope();
        IServiceProvider sp = scope.ServiceProvider;
        _ = sp.GetRequiredService<IStorageService>().ShouldNotBeNull();
        _ = sp.GetRequiredService<StubCommandService>().ShouldNotBeNull();
        ICommandService commandService = sp.GetRequiredService<ICommandService>();
        ICommandServiceWithLifecycle lifecycleCommandService = sp.GetRequiredService<ICommandServiceWithLifecycle>();
        ICommandServiceWithLifecycleObservations observationCommandService =
            sp.GetRequiredService<ICommandServiceWithLifecycleObservations>();
        commandService.ShouldNotBeOfType<StubCommandService>(
            "Stub dispatch must resolve through AuthorizingCommandServiceDecorator before side effects");
        lifecycleCommandService.ShouldNotBeOfType<StubCommandService>(
            "lifecycle dispatch must use the same decorated Stub path");
        lifecycleCommandService.ShouldBeSameAs(commandService);
        observationCommandService.ShouldBeSameAs(commandService);
        _ = sp.GetRequiredService<IAuthorizationService>().ShouldNotBeNull();
        _ = sp.GetRequiredService<ICommandAuthorizationEvaluator>().ShouldNotBeNull();
        _ = sp.GetRequiredService<ICommandDispatchAuthorizationGate>().ShouldNotBeNull();
        _ = sp.GetRequiredService<IBadgeCountService>().ShouldNotBeNull();
        _ = sp.GetRequiredService<ILifecycleStateService>().ShouldNotBeNull();
        _ = sp.GetRequiredService<ILifecycleBridgeRegistry>().ShouldNotBeNull();
        IPendingCommandOutcomeResolver resolver = sp.GetRequiredService<IPendingCommandOutcomeResolver>();
        IPendingCommandOutcomeCoordinator coordinator = sp.GetRequiredService<IPendingCommandOutcomeCoordinator>();
        coordinator.ShouldBeSameAs(resolver);
        coordinator.ShouldBeSameAs(sp.GetRequiredService<PendingCommandOutcomeResolver>());
    }

    [Fact]
    public void Quickstart_RetainsExactlyOnePreRegisteredScopedAdmissionGate() {
        ServiceCollection services = [];
        services.AddScoped<ICommandExecutionAdmissionGate, CommandExecutionAdmissionGate>();

        _ = services.AddHexalithFrontComposerQuickstart();

        ServiceDescriptor descriptor = services.Single(static descriptor =>
            descriptor.ServiceType == typeof(ICommandExecutionAdmissionGate));
        descriptor.IsKeyedService.ShouldBeFalse();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        descriptor.ImplementationType.ShouldBe(typeof(CommandExecutionAdmissionGate));
    }

    [Theory]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Transient)]
    public void Quickstart_RejectsPreRegisteredNonScopedAdmissionGate(ServiceLifetime lifetime) {
        ServiceCollection services = [];
        services.Add(new ServiceDescriptor(
            typeof(ICommandExecutionAdmissionGate),
            typeof(CommandExecutionAdmissionGate),
            lifetime));

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            services.AddHexalithFrontComposerQuickstart());

        exception.Message.ShouldContain(nameof(ICommandExecutionAdmissionGate));
        exception.Message.ShouldContain("non-keyed Scoped");
    }

    [Fact]
    public void Quickstart_RejectsDuplicatePreRegisteredAdmissionGates() {
        ServiceCollection services = [];
        services.AddScoped<ICommandExecutionAdmissionGate, CommandExecutionAdmissionGate>();
        services.AddScoped<ICommandExecutionAdmissionGate, CommandExecutionAdmissionGate>();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            services.AddHexalithFrontComposerQuickstart());

        exception.Message.ShouldContain("exactly one");
    }

    [Fact]
    public void Quickstart_RejectsKeyedPreRegisteredAdmissionGate() {
        ServiceCollection services = [];
        services.AddKeyedScoped<ICommandExecutionAdmissionGate, CommandExecutionAdmissionGate>("custom");

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            services.AddHexalithFrontComposerQuickstart());

        exception.Message.ShouldContain("non-keyed");
    }

    [Fact]
    public void Quickstart_CustomCoordinatorOnlyRegistrationMapsResolverToSameInstance() {
        ServiceCollection services = [];
        services.AddScoped<IPendingCommandOutcomeCoordinator, CustomOutcomeCoordinator>();
        _ = services.AddHexalithFrontComposerQuickstart();
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope scope = provider.CreateScope();

        IPendingCommandOutcomeCoordinator coordinator = scope.ServiceProvider
            .GetRequiredService<IPendingCommandOutcomeCoordinator>();
        IPendingCommandOutcomeResolver resolver = scope.ServiceProvider
            .GetRequiredService<IPendingCommandOutcomeResolver>();

        resolver.ShouldBeSameAs(coordinator);
        coordinator.ShouldBeOfType<CustomOutcomeCoordinator>();
    }

    [Fact]
    public void Quickstart_CompatibleResolverOnlyRegistrationMapsCoordinatorToSameInstance() {
        ServiceCollection services = [];
        services.AddScoped<CustomOutcomeCoordinator>();
        services.AddScoped<IPendingCommandOutcomeResolver>(provider =>
            provider.GetRequiredService<CustomOutcomeCoordinator>());
        _ = services.AddHexalithFrontComposerQuickstart();
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope scope = provider.CreateScope();

        IPendingCommandOutcomeResolver resolver = scope.ServiceProvider
            .GetRequiredService<IPendingCommandOutcomeResolver>();
        IPendingCommandOutcomeCoordinator coordinator = scope.ServiceProvider
            .GetRequiredService<IPendingCommandOutcomeCoordinator>();

        coordinator.ShouldBeSameAs(resolver);
        resolver.ShouldBeOfType<CustomOutcomeCoordinator>();
    }

    [Fact]
    public void Quickstart_SeparateResolverAndCoordinatorRegistrationsFailClearly() {
        ServiceCollection services = [];
        services.AddScoped<IPendingCommandOutcomeResolver, ResolverOnly>();
        services.AddScoped<IPendingCommandOutcomeCoordinator, CustomOutcomeCoordinator>();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            services.AddHexalithFrontComposerQuickstart());

        exception.Message.ShouldContain(nameof(IPendingCommandOutcomeResolver));
        exception.Message.ShouldContain(nameof(IPendingCommandOutcomeCoordinator));
        exception.Message.ShouldContain("exactly one");
        services.ShouldNotContain(descriptor =>
            descriptor.ServiceType == typeof(PendingCommandOutcomeResolver));
    }

    [Theory]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Transient)]
    public void Quickstart_CustomOutcomeBoundaryMustBeScoped(ServiceLifetime lifetime) {
        ServiceCollection services = [];
        services.Add(new ServiceDescriptor(
            typeof(IPendingCommandOutcomeCoordinator),
            typeof(CustomOutcomeCoordinator),
            lifetime));

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            services.AddHexalithFrontComposerQuickstart());

        exception.Message.ShouldContain("Scoped");
        exception.Message.ShouldContain(nameof(IPendingCommandOutcomeCoordinator));
    }

    [Fact]
    public void Quickstart_DuplicateCustomOutcomeRegistrationsFailClearly() {
        ServiceCollection services = [];
        services.AddScoped<IPendingCommandOutcomeCoordinator, CustomOutcomeCoordinator>();
        services.AddScoped<IPendingCommandOutcomeCoordinator, CustomOutcomeCoordinator>();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            services.AddHexalithFrontComposerQuickstart());

        exception.Message.ShouldContain("exactly one");
    }

    [Fact]
    public void Quickstart_ReentryRejectsOutcomeOverrideAfterFrameworkMarker() {
        ServiceCollection services = [];
        _ = services.AddHexalithFrontComposerQuickstart();
        services.AddScoped<IPendingCommandOutcomeCoordinator, CustomOutcomeCoordinator>();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            services.AddHexalithFrontComposerQuickstart());

        exception.Message.ShouldContain("replaced or duplicated");
    }

    [Fact]
    public void Quickstart_LateResolverReplacementCannotSplitFrameworkPollingFromCoordinator() {
        ServiceCollection services = [];
        _ = services.AddHexalithFrontComposerQuickstart();
        services.Replace(ServiceDescriptor.Scoped<IPendingCommandOutcomeResolver, ResolverOnly>());
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope scope = provider.CreateScope();

        IPendingCommandOutcomeCoordinator coordinator = scope.ServiceProvider
            .GetRequiredService<IPendingCommandOutcomeCoordinator>();
        IPendingCommandPollingCoordinator polling = scope.ServiceProvider
            .GetRequiredService<IPendingCommandPollingCoordinator>();
        object frameworkResolver = typeof(PendingCommandPollingCoordinator)
            .GetField("_resolver", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .ShouldNotBeNull()
            .GetValue(polling)
            .ShouldNotBeNull();

        frameworkResolver.ShouldBeSameAs(coordinator);
        scope.ServiceProvider.GetRequiredService<IPendingCommandOutcomeResolver>()
            .ShouldBeOfType<ResolverOnly>();
    }

    [Fact]
    public void Quickstart_RepeatedDefaultRegistrationPreservesOneSharedOutcomeBoundary() {
        ServiceCollection services = [];
        _ = services.AddHexalithFrontComposerQuickstart();
        _ = services.AddHexalithFrontComposerQuickstart();
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope scope = provider.CreateScope();

        IPendingCommandOutcomeResolver resolver = scope.ServiceProvider
            .GetRequiredService<IPendingCommandOutcomeResolver>();
        IPendingCommandOutcomeCoordinator coordinator = scope.ServiceProvider
            .GetRequiredService<IPendingCommandOutcomeCoordinator>();

        coordinator.ShouldBeSameAs(resolver);
        resolver.ShouldBeSameAs(scope.ServiceProvider.GetRequiredService<PendingCommandOutcomeResolver>());
    }

    [Fact]
    public void Quickstart_LegacyResolverOnlyRegistrationIsAdaptedWithoutActivationFailure() {
        ServiceCollection services = [];
        ResolverOnly legacyResolver = new();
        services.AddScoped<IPendingCommandOutcomeResolver>(_ => legacyResolver);
        _ = services.AddHexalithFrontComposerQuickstart();
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IPendingCommandOutcomeResolver>()
            .ShouldBeSameAs(legacyResolver);
        IPendingCommandOutcomeCoordinator coordinator = scope.ServiceProvider
            .GetRequiredService<IPendingCommandOutcomeCoordinator>();

        coordinator.ShouldNotBeSameAs(legacyResolver);
        coordinator.Resolve(new PendingCommandOutcomeObservation(
            PendingCommandOutcomeSource.LiveNudgeRefresh,
            PendingCommandTerminalOutcome.Confirmed,
            MessageId: "01ARZ3NDEKTSV4RRFFQ69G5FAV"));
        legacyResolver.ResolveCount.ShouldBe(1);
    }

    [Fact]
    public void ThreeCallGraph_ResolvesEndToEndUnderScopeValidation() {
        // The full AC1 ordering: Quickstart → Domain → EventStore. EventStore swaps the stub command
        // path for the real client; the rest of the graph must still resolve cleanly.
        ServiceCollection services = [];
        _ = services.AddHexalithFrontComposerQuickstart();
        _ = services.AddHexalithDomain<CounterDomain>();
        _ = services.AddHexalithEventStore(o => {
            o.BaseAddress = new Uri("http://localhost:9/");
            o.RequireAccessToken = false;
        });
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());

        // EventStore only TryAdds the registry, so the Quickstart-installed authoritative Singleton
        // (now holding the Counter manifest) survives.
        LifetimeOf(services, typeof(IFrontComposerRegistry)).ShouldBe(ServiceLifetime.Singleton);
        LifetimeOf(services, typeof(IStorageService)).ShouldBe(ServiceLifetime.Scoped);

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        IFrontComposerRegistry registry = provider.GetRequiredService<IFrontComposerRegistry>();
        registry.GetManifests().ShouldContain(m => m.BoundedContext == "Counter");

        using IServiceScope scope = provider.CreateScope();
        IServiceProvider sp = scope.ServiceProvider;
        _ = sp.GetRequiredService<IStorageService>().ShouldNotBeNull();
        ICommandService commandService = sp.GetRequiredService<ICommandService>();
        ICommandServiceWithLifecycle lifecycleCommandService = sp.GetRequiredService<ICommandServiceWithLifecycle>();
        ICommandServiceWithLifecycleObservations observationCommandService =
            sp.GetRequiredService<ICommandServiceWithLifecycleObservations>();
        commandService.ShouldNotBeOfType<EventStoreCommandClient>(
            "EventStore dispatch must resolve through AuthorizingCommandServiceDecorator before HTTP side effects");
        lifecycleCommandService.ShouldNotBeOfType<EventStoreCommandClient>(
            "lifecycle dispatch must use the same decorated EventStore path");
        lifecycleCommandService.ShouldBeSameAs(commandService);
        observationCommandService.ShouldBeSameAs(commandService);
        _ = sp.GetRequiredService<IAuthorizationService>().ShouldNotBeNull();
        _ = sp.GetRequiredService<ICommandAuthorizationEvaluator>().ShouldNotBeNull();
        _ = sp.GetRequiredService<ICommandDispatchAuthorizationGate>().ShouldNotBeNull();
        // AC1 names the "command/query stub path" — pin the query half too (EventStore swaps in the
        // real IQueryService client; only the command half was previously asserted).
        _ = sp.GetRequiredService<IQueryService>().ShouldNotBeNull();
        _ = sp.GetRequiredService<IBadgeCountService>().ShouldNotBeNull();
        _ = sp.GetRequiredService<ILifecycleStateService>().ShouldNotBeNull();
        _ = sp.GetRequiredService<ILifecycleBridgeRegistry>().ShouldNotBeNull();
        _ = sp.GetRequiredService<IProjectionSlotRegistry>().ShouldNotBeNull();
        _ = sp.GetRequiredService<IProjectionTemplateRegistry>().ShouldNotBeNull();
        _ = sp.GetRequiredService<IProjectionViewOverrideRegistry>().ShouldNotBeNull();
    }

    [Fact]
    public void EventStoreOnlyGraph_ResolvesOneDecoratedTypedCommandServiceInstance() {
        ServiceCollection services = [];
        _ = services.AddHexalithEventStore(options => {
            options.BaseAddress = new Uri("http://localhost:9/");
            options.RequireAccessToken = false;
        });
        using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope scope = provider.CreateScope();

        ICommandService commandService = scope.ServiceProvider.GetRequiredService<ICommandService>();
        ICommandServiceWithLifecycle lifecycleCommandService = scope.ServiceProvider
            .GetRequiredService<ICommandServiceWithLifecycle>();
        ICommandServiceWithLifecycleObservations observationCommandService = scope.ServiceProvider
            .GetRequiredService<ICommandServiceWithLifecycleObservations>();

        commandService.ShouldNotBeOfType<EventStoreCommandClient>();
        lifecycleCommandService.ShouldBeSameAs(commandService);
        observationCommandService.ShouldBeSameAs(commandService);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EventStoreComposition_TypedDispatchInvokesObservationCallback(bool includeQuickstart) {
        RecordingAcceptedHandler handler = new();
        ServiceCollection services = [];
        if (includeQuickstart) {
            _ = services.AddHexalithFrontComposerQuickstart();
        }

        _ = services.AddHexalithEventStore(options => {
            options.BaseAddress = new Uri("https://eventstore.test/");
            options.RequireAccessToken = false;
        });
        services.AddHttpClient(EventStoreCommandClient.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        services.Replace(ServiceDescriptor.Scoped<IUserContextAccessor>(_ =>
            new FixedUserContextAccessor("tenant-1", "user-1")));
        using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope scope = provider.CreateScope();
        ICommandServiceWithLifecycleObservations service = scope.ServiceProvider
            .GetRequiredService<ICommandServiceWithLifecycleObservations>();
        List<CommandLifecycleObservation> observations = [];

        CommandResult result = await service.DispatchAsync(
            new GraphCommand { Name = "aggregate-1" },
            observations.Add,
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(CommandResultStatus.Accepted);
        observations.Single().State.ShouldBe(CommandLifecycleState.Syncing);
        observations.Single().Materiality.ShouldBe(CommandMateriality.Unknown);
        handler.RequestCount.ShouldBe(1);
    }

    private static ServiceLifetime LifetimeOf(IServiceCollection services, Type serviceType)
        => services.Last(d => d.ServiceType == serviceType).Lifetime;

    private sealed class LegacyOnlyCommandService : ICommandServiceWithLifecycle {
        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class => DispatchAsync(command, onLifecycleChange: null, cancellationToken);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleState, string?>? onLifecycleChange,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            const string messageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
            onLifecycleChange?.Invoke(CommandLifecycleState.Confirmed, messageId);
            return Task.FromResult(new CommandResult(messageId, "Accepted"));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [BoundedContext("Graph")]
    private sealed class GraphCommand {
        public string Name { get; init; } = string.Empty;
    }

    private sealed class FixedUserContextAccessor(string tenantId, string userId) : IUserContextAccessor {
        public string? TenantId => tenantId;

        public string? UserId => userId;
    }

    private sealed class RecordingAcceptedHandler : HttpMessageHandler {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        }
    }

    private sealed class CustomOutcomeCoordinator : IPendingCommandOutcomeCoordinator {
        public PendingCommandOutcomeResolutionResult BufferBeforeAccepted(
            string ownerId,
            PendingCommandOutcomeObservation observation) =>
            new(PendingCommandOutcomeResolutionStatus.Buffered);

        public PendingCommandRegistrationResult AssociateAccepted(PendingCommandRegistration registration) =>
            PendingCommandRegistrationResult.Disposed();

        public void DiscardBuffered(string? messageId) {
        }

        public void DiscardBufferedByOwner(string ownerId) {
        }

        public PendingCommandOutcomeResolutionResult Resolve(PendingCommandOutcomeObservation observation) =>
            new(PendingCommandOutcomeResolutionStatus.Unknown);
    }

    private sealed class ResolverOnly : IPendingCommandOutcomeResolver {
        public int ResolveCount { get; private set; }

        public PendingCommandOutcomeResolutionResult Resolve(PendingCommandOutcomeObservation observation) =>
            ResolveCore();

        private PendingCommandOutcomeResolutionResult ResolveCore() {
            ResolveCount++;
            return new(PendingCommandOutcomeResolutionStatus.Unknown);
        }
    }
}
