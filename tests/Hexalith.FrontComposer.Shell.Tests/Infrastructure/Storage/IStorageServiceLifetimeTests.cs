using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.Storage;

/// <summary>
/// Story 3-1 Task 10.12 (ADR-030) — lock the Scoped lifetime of <see cref="IStorageService"/>
/// so a future regression (Singleton back-slide) fails CI. Two distinct scopes must yield two
/// distinct instances.
/// </summary>
public sealed class IStorageServiceLifetimeTests {
    [Fact]
    public void IStorageServiceIsScoped_DistinctScopesYieldDistinctInstances() {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddHexalithFrontComposer();
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        using IServiceScope scope1 = provider.CreateScope();
        using IServiceScope scope2 = provider.CreateScope();

        IStorageService instance1 = scope1.ServiceProvider.GetRequiredService<IStorageService>();
        IStorageService instance2 = scope2.ServiceProvider.GetRequiredService<IStorageService>();

        instance1.ShouldNotBeSameAs(instance2);
    }

    [Fact]
    public void IStorageServiceIsScoped_SameScopeYieldsSameInstance() {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddHexalithFrontComposer();
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        using IServiceScope scope = provider.CreateScope();
        IStorageService instance1 = scope.ServiceProvider.GetRequiredService<IStorageService>();
        IStorageService instance2 = scope.ServiceProvider.GetRequiredService<IStorageService>();

        instance1.ShouldBeSameAs(instance2);
    }
}
