
using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State;
using Hexalith.FrontComposer.Shell.State.Density;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.Density;
/// <summary>
/// Unit tests for <see cref="DensityEffects"/>.
/// </summary>
public class DensityEffectsTests {
    [Fact]
    public async Task HandleDensityChanged_PersistsToStorage() {
        // Arrange
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        var storage = new InMemoryStorageService();
        ILogger<DensityEffects> logger = Substitute.For<ILogger<DensityEffects>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        var sut = new DensityEffects(storage, logger);
        var action = new DensityChangedAction("corr-1", DensityLevel.Compact);
        string key = StorageKeys.BuildKey(StorageKeys.DefaultTenantId, StorageKeys.DefaultUserId, "density");

        // Act
        await sut.HandleDensityChanged(action, dispatcher);

        // Assert
        object? stored = await storage.GetAsync<object>(key, ct);
        stored.ShouldBe(DensityLevel.Compact);
    }

    [Fact]
    public async Task HandleDensityChanged_StorageServiceThrows_LogsWarning() {
        // Arrange
        IStorageService storage = Substitute.For<IStorageService>();
        _ = storage.SetAsync(Arg.Any<string>(), Arg.Any<DensityLevel>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Storage failure"));
        ILogger<DensityEffects> logger = Substitute.For<ILogger<DensityEffects>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        var sut = new DensityEffects(storage, logger);
        var action = new DensityChangedAction("corr-1", DensityLevel.Compact);

        // Act — should not throw
        await sut.HandleDensityChanged(action, dispatcher);

        // Assert — logger was called
        logger.ReceivedWithAnyArgs(1).Log(
            default,
            default,
            default!,
            default,
            default!);
    }

    [Fact]
    public async Task DispatchDensityChanged_StorageServiceThrows_StoreStillUpdatesState() {
        // Arrange
        IStorageService storage = Substitute.For<IStorageService>();
        _ = storage.SetAsync(Arg.Any<string>(), Arg.Any<DensityLevel>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Storage failure"));

        using ServiceProvider provider = BuildProvider(storage);
        IStore store = provider.GetRequiredService<IStore>();
        await store.InitializeAsync();

        IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();
        IState<FrontComposerDensityState> densityState = provider.GetRequiredService<IState<FrontComposerDensityState>>();

        // Act
        dispatcher.Dispatch(new DensityChangedAction("corr-1", DensityLevel.Compact));
        WaitFor(() => densityState.Value.CurrentDensity == DensityLevel.Compact).ShouldBeTrue();

        dispatcher.Dispatch(new DensityChangedAction("corr-2", DensityLevel.Roomy));
        WaitFor(() => densityState.Value.CurrentDensity == DensityLevel.Roomy).ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAppInitialized_StorageContainsValue_DispatchesDensityChanged() {
        // Arrange
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        var storage = new InMemoryStorageService();
        string key = StorageKeys.BuildKey(StorageKeys.DefaultTenantId, StorageKeys.DefaultUserId, "density");
        await storage.SetAsync(key, DensityLevel.Compact, ct);
        ILogger<DensityEffects> logger = Substitute.For<ILogger<DensityEffects>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        var sut = new DensityEffects(storage, logger);
        var action = new AppInitializedAction("corr-init");

        // Act
        await sut.HandleAppInitialized(action, dispatcher);

        // Assert
        dispatcher.Received(1).Dispatch(
            Arg.Is<DensityChangedAction>(a => a.NewDensity == DensityLevel.Compact && a.CorrelationId == "corr-init"));
    }

    [Fact]
    public async Task HandleAppInitialized_StorageEmpty_DoesNotDispatch() {
        // Arrange — empty storage, no seeding
        var storage = new InMemoryStorageService();
        ILogger<DensityEffects> logger = Substitute.For<ILogger<DensityEffects>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        var sut = new DensityEffects(storage, logger);
        var action = new AppInitializedAction("corr-init");

        // Act
        await sut.HandleAppInitialized(action, dispatcher);

        // Assert — no dispatch because no stored density preference exists
        dispatcher.DidNotReceiveWithAnyArgs().Dispatch(default!);
    }

    private static ServiceProvider BuildProvider(IStorageService storage) {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddFluxor(o => o.ScanAssemblies(typeof(FrontComposerDensityState).Assembly));
        _ = services.AddSingleton(storage);
        return services.BuildServiceProvider();
    }

    private static bool WaitFor(Func<bool> condition)
        => SpinWait.SpinUntil(condition, TimeSpan.FromSeconds(1));
}
