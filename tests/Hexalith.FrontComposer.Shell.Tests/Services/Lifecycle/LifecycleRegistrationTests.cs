using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Services.Lifecycle;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Lifecycle;

/// <summary>Story 2-3 Task 11.6 — 3 DI/registration tests.</summary>
public class LifecycleRegistrationTests {
    private static ServiceCollection BuildServices() {
        ServiceCollection services = new();
        services.AddHexalithFrontComposer();
        return services;
    }

    [Fact]
    public void AddHexalithFrontComposer_RegistersILifecycleStateService_Scoped() {
        ServiceCollection services = BuildServices();

        ServiceDescriptor descriptor = services.First(d => d.ServiceType == typeof(ILifecycleStateService));

        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        descriptor.ImplementationType.ShouldBe(typeof(LifecycleStateService));
    }

    [Fact]
    public void AddHexalithFrontComposer_RegistersIUlidFactory_Singleton() {
        ServiceCollection services = BuildServices();

        ServiceDescriptor descriptor = services.First(d => d.ServiceType == typeof(IUlidFactory));

        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
        descriptor.ImplementationType.ShouldBe(typeof(UlidFactory));
    }

    [Fact]
    public void AddHexalithFrontComposer_RegistersLifecycleBridgeRegistry_Scoped() {
        ServiceCollection services = BuildServices();

        ServiceDescriptor concrete = services.First(d => d.ServiceType == typeof(LifecycleBridgeRegistry));
        ServiceDescriptor iface = services.First(d => d.ServiceType == typeof(ILifecycleBridgeRegistry));

        concrete.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        iface.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }
}
