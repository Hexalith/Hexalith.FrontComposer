using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

/// <summary>
/// Story 5-1 T6 smoke test: every EventStore service the public DI extensions
/// register must be resolvable end-to-end without contacting a live EventStore.
/// </summary>
public sealed class SeamExtractionSmokeTests {
    [Fact]
    public async Task AddHexalithEventStore_ResolvesEverySeam_WithoutLiveEventStore() {
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposer();
        _ = services.AddHexalithEventStore(options => {
            options.BaseAddress = new Uri("https://eventstore.test");
            options.RequireAccessToken = false;
        });
        // Story 5-2 — EventStoreQueryClient now depends on IETagCache → IStorageService.
        // The default LocalStorageService needs IJSRuntime; swap to InMemoryStorageService
        // so the seam-extraction smoke test stays a pure container-shape assertion.
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());

        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        // Story 7-3 Pass 4 DN-7-3-4-2: ICommandService / ICommandServiceWithLifecycle resolve
        // through AuthorizingCommandServiceDecorator. Assert the inner concrete is also
        // independently resolvable so adopters can still inspect it directly.
        scope.ServiceProvider.GetRequiredService<EventStoreCommandClient>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<ICommandService>().ShouldNotBeOfType<EventStoreCommandClient>(
            "ICommandService is wrapped by AuthorizingCommandServiceDecorator (Pass 4 DN-7-3-4-2)");
        scope.ServiceProvider.GetRequiredService<ICommandServiceWithLifecycle>().ShouldNotBeOfType<EventStoreCommandClient>();
        scope.ServiceProvider.GetRequiredService<IQueryService>().ShouldBeOfType<EventStoreQueryClient>();
        scope.ServiceProvider.GetRequiredService<IProjectionSubscription>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IProjectionChangeNotifier>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IProjectionChangeNotifierWithTenant>().ShouldNotBeNull();

        // The single concrete notifier must back both notifier interface registrations
        // so that producers and consumers observe the same event surface within a circuit.
        scope.ServiceProvider.GetRequiredService<IProjectionChangeNotifier>()
            .ShouldBeSameAs(scope.ServiceProvider.GetRequiredService<IProjectionChangeNotifierWithTenant>());
    }
}
