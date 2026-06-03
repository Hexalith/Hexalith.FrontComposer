using Counter.Domain;

using Hexalith.FrontComposer.Contracts.Badges;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Services;

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
public sealed class FrontComposerServiceGraphTests
{
    [Fact]
    public void Quickstart_RegistersStubCommandPathAndCoreServices_WithCorrectLifetimes()
    {
        ServiceCollection services = new();
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
        _ = sp.GetRequiredService<ICommandService>().ShouldNotBeNull();
        _ = sp.GetRequiredService<ICommandServiceWithLifecycle>().ShouldNotBeNull();
        _ = sp.GetRequiredService<IBadgeCountService>().ShouldNotBeNull();
        _ = sp.GetRequiredService<ILifecycleStateService>().ShouldNotBeNull();
        _ = sp.GetRequiredService<ILifecycleBridgeRegistry>().ShouldNotBeNull();
    }

    [Fact]
    public void ThreeCallGraph_ResolvesEndToEndUnderScopeValidation()
    {
        // The full AC1 ordering: Quickstart → Domain → EventStore. EventStore swaps the stub command
        // path for the real client; the rest of the graph must still resolve cleanly.
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposerQuickstart();
        _ = services.AddHexalithDomain<CounterDomain>();
        _ = services.AddHexalithEventStore(o =>
        {
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
        _ = sp.GetRequiredService<ICommandService>().ShouldNotBeNull();
        _ = sp.GetRequiredService<ICommandServiceWithLifecycle>().ShouldNotBeNull();
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

    private static ServiceLifetime LifetimeOf(IServiceCollection services, Type serviceType)
        => services.Last(d => d.ServiceType == serviceType).Lifetime;
}
