using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

public sealed class EventStoreRegistrationTests {
    [Fact]
    public async Task AddHexalithEventStore_ReplacesFrontComposerStubDefaults() {
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposer();
        _ = services.AddHexalithEventStore(options => {
            options.BaseAddress = new Uri("https://eventstore.test");
            options.RequireAccessToken = false;
        });
        // Story 5-2 — EventStoreQueryClient now depends on IETagCache → IStorageService.
        // The default LocalStorageService needs IJSRuntime; swap to InMemoryStorageService
        // so the registration test stays a pure container-shape assertion.
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());

        await using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICommandService>().ShouldBeOfType<EventStoreCommandClient>();
        provider.GetRequiredService<IQueryService>().ShouldBeOfType<EventStoreQueryClient>();
        provider.GetRequiredService<IProjectionSubscription>().ShouldBeAssignableTo<IProjectionSubscription>();
        provider.GetRequiredService<IProjectionChangeNotifier>().ShouldNotBeNull();
    }

    [Fact]
    public async Task ConsumerReplacementWinsAfterEventStoreRegistration() {
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposer();
        _ = services.AddHexalithEventStore(options => {
            options.BaseAddress = new Uri("https://eventstore.test");
            options.RequireAccessToken = false;
        });
        services.Replace(ServiceDescriptor.Scoped<ICommandService, ReplacementCommandService>());

        await using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICommandService>().ShouldBeOfType<ReplacementCommandService>();
    }

    [Fact]
    public void InvalidOptionsFailConsistently() {
        ServiceCollection services = new();
        _ = services.AddHexalithEventStore();

        using ServiceProvider provider = services.BuildServiceProvider();

        _ = Should.Throw<Microsoft.Extensions.Options.OptionsValidationException>(
            () => _ = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<EventStoreOptions>>().Value);
    }

    private sealed class ReplacementCommandService : ICommandService {
        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class
            => Task.FromResult(new CommandResult("replacement", "Accepted"));
    }
}
