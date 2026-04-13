namespace Hexalith.FrontComposer.Shell.Tests.State.Theme;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using Xunit;

/// <summary>
/// Unit tests for <see cref="ThemeEffects"/>.
/// </summary>
public class ThemeEffectsTests
{
    [Fact]
    public async Task HandleThemeChanged_PersistsToStorage()
    {
        // Arrange
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        var storage = new InMemoryStorageService();
        ILogger<ThemeEffects> logger = Substitute.For<ILogger<ThemeEffects>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        var sut = new ThemeEffects(storage, logger);
        var action = new ThemeChangedAction("corr-1", ThemeValue.Dark);
        string key = StorageKeys.BuildKey(StorageKeys.DefaultTenantId, StorageKeys.DefaultUserId, "theme");

        // Act
        await sut.HandleThemeChanged(action, dispatcher);

        // Assert
        object? stored = await storage.GetAsync<object>(key, ct);
        stored.ShouldBe(ThemeValue.Dark);
    }

    [Fact]
    public async Task HandleThemeChanged_StorageServiceThrows_LogsWarning()
    {
        // Arrange
        IStorageService storage = Substitute.For<IStorageService>();
        storage.SetAsync(Arg.Any<string>(), Arg.Any<ThemeValue>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Storage failure"));
        ILogger<ThemeEffects> logger = Substitute.For<ILogger<ThemeEffects>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        var sut = new ThemeEffects(storage, logger);
        var action = new ThemeChangedAction("corr-1", ThemeValue.Dark);

        // Act — should not throw
        await sut.HandleThemeChanged(action, dispatcher);

        // Assert — logger was called (warning level)
        logger.ReceivedWithAnyArgs(1).Log(
            default,
            default,
            default!,
            default,
            default!);
    }

    [Fact]
    public async Task DispatchThemeChanged_StorageServiceThrows_StoreStillUpdatesState()
    {
        // Arrange
        IStorageService storage = Substitute.For<IStorageService>();
        storage.SetAsync(Arg.Any<string>(), Arg.Any<ThemeValue>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Storage failure"));

        using ServiceProvider provider = BuildProvider(storage);
        IStore store = provider.GetRequiredService<IStore>();
        await store.InitializeAsync();

        IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();
        IState<FrontComposerThemeState> themeState = provider.GetRequiredService<IState<FrontComposerThemeState>>();

        // Act
        dispatcher.Dispatch(new ThemeChangedAction("corr-1", ThemeValue.Dark));
        WaitFor(() => themeState.Value.CurrentTheme == ThemeValue.Dark).ShouldBeTrue();

        dispatcher.Dispatch(new ThemeChangedAction("corr-2", ThemeValue.System));
        WaitFor(() => themeState.Value.CurrentTheme == ThemeValue.System).ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAppInitialized_StorageContainsValue_DispatchesThemeChanged()
    {
        // Arrange
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        var storage = new InMemoryStorageService();
        string key = StorageKeys.BuildKey(StorageKeys.DefaultTenantId, StorageKeys.DefaultUserId, "theme");
        await storage.SetAsync(key, ThemeValue.Dark, ct);
        ILogger<ThemeEffects> logger = Substitute.For<ILogger<ThemeEffects>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        var sut = new ThemeEffects(storage, logger);
        var action = new AppInitializedAction("corr-init");

        // Act
        await sut.HandleAppInitialized(action, dispatcher);

        // Assert
        dispatcher.Received(1).Dispatch(
            Arg.Is<ThemeChangedAction>(a => a.NewTheme == ThemeValue.Dark && a.CorrelationId == "corr-init"));
    }

    [Fact]
    public async Task HandleAppInitialized_StorageEmpty_DoesNotDispatch()
    {
        // Arrange — empty storage, no seeding
        var storage = new InMemoryStorageService();
        ILogger<ThemeEffects> logger = Substitute.For<ILogger<ThemeEffects>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        var sut = new ThemeEffects(storage, logger);
        var action = new AppInitializedAction("corr-init");

        // Act
        await sut.HandleAppInitialized(action, dispatcher);

        // Assert — no dispatch because no stored theme preference exists
        dispatcher.DidNotReceiveWithAnyArgs().Dispatch(default!);
    }

    private static ServiceProvider BuildProvider(IStorageService storage)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddFluxor(o => o.ScanAssemblies(typeof(FrontComposerThemeState).Assembly));
        services.AddSingleton(storage);
        return services.BuildServiceProvider();
    }

    private static bool WaitFor(Func<bool> condition)
        => SpinWait.SpinUntil(condition, TimeSpan.FromSeconds(1));
}
