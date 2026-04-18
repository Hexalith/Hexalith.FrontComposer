using Counter.Domain;
using Counter.Web;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Samples;

public sealed class CounterProjectionEffectsTests {
    [Fact]
    public async Task BatchIncrementConfirmed_UsesSubmittedAmount() {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddHexalithFrontComposer(
            o => o.ScanAssemblies(typeof(CounterProjection).Assembly, typeof(CounterProjectionEffects).Assembly));
        _ = services.AddHexalithDomain<CounterDomain>();
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());

        using ServiceProvider provider = services.BuildServiceProvider();
        IStore store = provider.GetRequiredService<IStore>();
        await store.InitializeAsync();

        IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();
        IState<CounterProjectionState> state = provider.GetRequiredService<IState<CounterProjectionState>>();

        dispatcher.Dispatch(new CounterProjectionLoadedAction(
            Guid.NewGuid().ToString(),
            [
                new CounterProjection
                {
                    Id = "counter-1",
                    Count = 10,
                    LastUpdated = DateTimeOffset.UtcNow,
                },
            ]));

        string correlationId = Guid.NewGuid().ToString();
        dispatcher.Dispatch(new BatchIncrementCommandActions.SubmittedAction(
            correlationId,
            new BatchIncrementCommand
            {
                MessageId = "batch-1",
                TenantId = "counter-demo",
                Amount = 7,
                Note = "bulk",
                EffectiveDate = DateTime.UtcNow,
            }));
        dispatcher.Dispatch(new BatchIncrementCommandActions.ConfirmedAction(correlationId));

        SpinWait.SpinUntil(
            () => state.Value.Items?.SingleOrDefault()?.Count == 17,
            TimeSpan.FromSeconds(1)).ShouldBeTrue();
    }

    [Fact]
    public async Task IncrementConfirmed_UsesSubmittedAmount() {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddHexalithFrontComposer(
            o => o.ScanAssemblies(typeof(CounterProjection).Assembly, typeof(CounterProjectionEffects).Assembly));
        _ = services.AddHexalithDomain<CounterDomain>();
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());

        using ServiceProvider provider = services.BuildServiceProvider();
        IStore store = provider.GetRequiredService<IStore>();
        await store.InitializeAsync();

        IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();
        IState<CounterProjectionState> state = provider.GetRequiredService<IState<CounterProjectionState>>();

        string correlationId = Guid.NewGuid().ToString();
        dispatcher.Dispatch(new IncrementCommandActions.SubmittedAction(
            correlationId,
            new IncrementCommand
            {
                MessageId = "inc-1",
                TenantId = "counter-demo",
                Amount = 5,
            }));
        dispatcher.Dispatch(new IncrementCommandActions.ConfirmedAction(correlationId));

        SpinWait.SpinUntil(
            () => state.Value.Items?.SingleOrDefault()?.Count == 5,
            TimeSpan.FromSeconds(1)).ShouldBeTrue();
    }
}
