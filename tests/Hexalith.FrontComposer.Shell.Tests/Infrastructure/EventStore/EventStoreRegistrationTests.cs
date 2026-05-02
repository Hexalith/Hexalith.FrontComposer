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

        // Story 7-3 Pass 4 DN-7-3-4-2: ICommandService now resolves through
        // AuthorizingCommandServiceDecorator wrapping the inner concrete (EventStoreCommandClient
        // here). The decorator type is internal; assert the inner concrete is still resolvable
        // separately and that the decorated service round-trips the inner type through the
        // ICommandServiceWithLifecycle interface contract.
        provider.GetRequiredService<EventStoreCommandClient>().ShouldNotBeNull();
        provider.GetRequiredService<ICommandService>().ShouldNotBeOfType<EventStoreCommandClient>(
            "ICommandService is wrapped by AuthorizingCommandServiceDecorator (Pass 4 DN-7-3-4-2)");
        provider.GetRequiredService<ICommandServiceWithLifecycle>().ShouldNotBeOfType<EventStoreCommandClient>();
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
