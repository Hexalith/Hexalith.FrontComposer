using Counter.Domain;
using Counter.Web;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure.Tenancy;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Samples;

public sealed class CounterProjectionEffectsTests {
    [Fact]
    public async Task BatchIncrementConfirmed_UsesSubmittedAmount() {
        ServiceCollection services = [];
        _ = services.AddLogging();
        _ = services.AddHexalithFrontComposer(
            o => o.ScanAssemblies(typeof(CounterProjection).Assembly, typeof(CounterProjectionEffects).Assembly));
        _ = services.AddHexalithDomain<CounterDomain>();
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        services.Replace(ServiceDescriptor.Singleton<IFrontComposerTenantContextAccessor>(new TestTenantContextAccessor()));

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
            new BatchIncrementCommand {
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
        ServiceCollection services = [];
        _ = services.AddLogging();
        _ = services.AddHexalithFrontComposer(
            o => o.ScanAssemblies(typeof(CounterProjection).Assembly, typeof(CounterProjectionEffects).Assembly));
        _ = services.AddHexalithDomain<CounterDomain>();
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        services.Replace(ServiceDescriptor.Singleton<IFrontComposerTenantContextAccessor>(new TestTenantContextAccessor()));

        using ServiceProvider provider = services.BuildServiceProvider();
        IStore store = provider.GetRequiredService<IStore>();
        await store.InitializeAsync();

        IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();
        IState<CounterProjectionState> state = provider.GetRequiredService<IState<CounterProjectionState>>();

        string correlationId = Guid.NewGuid().ToString();
        dispatcher.Dispatch(new IncrementCommandActions.SubmittedAction(
            correlationId,
            new IncrementCommand {
                MessageId = "inc-1",
                TenantId = "counter-demo",
                Amount = 5,
            }));
        dispatcher.Dispatch(new IncrementCommandActions.ConfirmedAction(correlationId));

        SpinWait.SpinUntil(
            () => state.Value.Items?.SingleOrDefault()?.Count == 5,
            TimeSpan.FromSeconds(1)).ShouldBeTrue();
    }

    [Fact]
    public async Task IncrementConfirmed_AfterScopeSwitch_DoesNotRequestOrLoadProjection() {
        ServiceCollection services = [];
        _ = services.AddLogging();
        _ = services.AddHexalithFrontComposer(
            o => o.ScanAssemblies(typeof(CounterProjection).Assembly, typeof(CounterProjectionEffects).Assembly));
        _ = services.AddHexalithDomain<CounterDomain>();
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        TestTenantContextAccessor scope = new();
        services.Replace(ServiceDescriptor.Singleton<IFrontComposerTenantContextAccessor>(scope));

        using ServiceProvider provider = services.BuildServiceProvider();
        IStore store = provider.GetRequiredService<IStore>();
        await store.InitializeAsync();
        IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();
        IState<CounterProjectionState> state = provider.GetRequiredService<IState<CounterProjectionState>>();

        string correlationId = Guid.NewGuid().ToString();
        dispatcher.Dispatch(new IncrementCommandActions.SubmittedAction(
            correlationId,
            new IncrementCommand { MessageId = "inc-1", TenantId = "tenant-a", Amount = 5 }));
        scope.TenantId = "tenant-b";
        dispatcher.Dispatch(new ScopeChangedAction());
        dispatcher.Dispatch(new IncrementCommandActions.ConfirmedAction(correlationId));

        state.Value.Items.ShouldBeNull();
        state.Value.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public async Task IncrementConfirmed_AfterReturningToSubmittedScope_DoesNotLoadOldProjection() {
        ServiceCollection services = [];
        _ = services.AddLogging();
        _ = services.AddHexalithFrontComposer(
            o => o.ScanAssemblies(typeof(CounterProjection).Assembly, typeof(CounterProjectionEffects).Assembly));
        _ = services.AddHexalithDomain<CounterDomain>();
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        TestTenantContextAccessor scope = new();
        services.Replace(ServiceDescriptor.Singleton<IFrontComposerTenantContextAccessor>(scope));

        using ServiceProvider provider = services.BuildServiceProvider();
        IStore store = provider.GetRequiredService<IStore>();
        await store.InitializeAsync();
        IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();
        IState<CounterProjectionState> state = provider.GetRequiredService<IState<CounterProjectionState>>();
        CounterProjectionEffects effects = new(state, scope);

        string correlationId = Guid.NewGuid().ToString();
        await effects.OnIncrementSubmitted(new IncrementCommandActions.SubmittedAction(
            correlationId,
            new IncrementCommand { MessageId = "inc-old-a", TenantId = "tenant-a", Amount = 5 }), dispatcher);
        scope.TenantId = "tenant-b";
        dispatcher.Dispatch(new ScopeChangedAction());
        scope.TenantId = "tenant-a";
        dispatcher.Dispatch(new ScopeChangedAction());
        state.Value.ScopeGeneration.ShouldBe(2);

        await effects.OnCommandConfirmed(new IncrementCommandActions.ConfirmedAction(correlationId), dispatcher);

        state.Value.Items.ShouldBeNull();
        state.Value.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public async Task QueuedIncrementAndBatchConfirmationsApplyBothDeltas()
    {
        ServiceCollection services = [];
        _ = services.AddLogging();
        _ = services.AddHexalithFrontComposer(
            o => o.ScanAssemblies(typeof(CounterProjection).Assembly, typeof(CounterProjectionEffects).Assembly));
        _ = services.AddHexalithDomain<CounterDomain>();
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        TestTenantContextAccessor scope = new();
        services.Replace(ServiceDescriptor.Singleton<IFrontComposerTenantContextAccessor>(scope));

        using ServiceProvider provider = services.BuildServiceProvider();
        await provider.GetRequiredService<IStore>().InitializeAsync();
        IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();
        IState<CounterProjectionState> state = provider.GetRequiredService<IState<CounterProjectionState>>();
        dispatcher.Dispatch(new CounterProjectionLoadedAction("seed", [new CounterProjection { Count = 10 }]));

        List<object> queued = [];
        IDispatcher deferredDispatcher = Substitute.For<IDispatcher>();
        deferredDispatcher.When(d => d.Dispatch(Arg.Any<object>())).Do(call => queued.Add(call.Arg<object>()));
        CounterProjectionEffects effects = new(state, scope);
        await effects.OnIncrementSubmitted(new IncrementCommandActions.SubmittedAction(
            "single", new IncrementCommand { Amount = 3 }), deferredDispatcher);
        await effects.OnBatchIncrementSubmitted(new BatchIncrementCommandActions.SubmittedAction(
            "batch", new BatchIncrementCommand { Amount = 7 }), deferredDispatcher);

        await effects.OnCommandConfirmed(new IncrementCommandActions.ConfirmedAction("single"), deferredDispatcher);
        await effects.OnBatchIncrementConfirmed(new BatchIncrementCommandActions.ConfirmedAction("batch"), deferredDispatcher);
        state.Value.Items.ShouldNotBeNull().ShouldHaveSingleItem().Count.ShouldBe(10);

        queued.Count.ShouldBe(2);
        DateTimeOffset latest = new(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);
        var single = queued[0].ShouldBeOfType<CounterProjectionDeltaAction>() with { Timestamp = latest };
        var batch = queued[1].ShouldBeOfType<CounterProjectionDeltaAction>() with { Timestamp = latest.AddSeconds(-1) };
        dispatcher.Dispatch(single);
        dispatcher.Dispatch(batch);

        CounterProjection result = state.Value.Items.ShouldNotBeNull().ShouldHaveSingleItem();
        result.Count.ShouldBe(20);
        result.LastUpdated.ShouldBe(latest);
        state.Value.IsLoading.ShouldBeFalse();

        dispatcher.Dispatch(new ScopeChangedAction());
        dispatcher.Dispatch(single);
        state.Value.Items.ShouldBeNull();
        state.Value.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public async Task ConfigureConfirmed_AfterScopeSwitch_DoesNotRequestOrLoadProjection() {
        ServiceCollection services = [];
        _ = services.AddLogging();
        _ = services.AddHexalithFrontComposer(
            o => o.ScanAssemblies(typeof(CounterProjection).Assembly, typeof(CounterProjectionEffects).Assembly));
        _ = services.AddHexalithDomain<CounterDomain>();
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        TestTenantContextAccessor scope = new();
        services.Replace(ServiceDescriptor.Singleton<IFrontComposerTenantContextAccessor>(scope));

        using ServiceProvider provider = services.BuildServiceProvider();
        IStore store = provider.GetRequiredService<IStore>();
        await store.InitializeAsync();
        IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();
        IState<CounterProjectionState> state = provider.GetRequiredService<IState<CounterProjectionState>>();

        string correlationId = Guid.NewGuid().ToString();
        dispatcher.Dispatch(new ConfigureCounterCommandActions.SubmittedAction(
            correlationId,
            new ConfigureCounterCommand { MessageId = "configure-1", TenantId = "tenant-a" }));
        scope.TenantId = "tenant-b";
        dispatcher.Dispatch(new ScopeChangedAction());
        dispatcher.Dispatch(new ConfigureCounterCommandActions.ConfirmedAction(correlationId));

        state.Value.Items.ShouldBeNull();
        state.Value.IsLoading.ShouldBeFalse();
    }
}
