using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

using Microsoft.Extensions.DependencyInjection;

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

        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<ICommandService>().ShouldBeOfType<EventStoreCommandClient>();
        scope.ServiceProvider.GetRequiredService<ICommandServiceWithLifecycle>().ShouldBeOfType<EventStoreCommandClient>();
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
