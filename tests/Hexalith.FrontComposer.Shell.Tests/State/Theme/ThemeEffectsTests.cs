using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace Hexalith.FrontComposer.Shell.Tests.State.Theme;

/// <summary>
/// Unit tests for <see cref="ThemeEffects"/>.
/// </summary>
public class ThemeEffectsTests
{
    private const string TestTenant = "tenant-a";
    private const string TestUser = "user-1";

    [Fact]
    public async Task DispatchThemeChanged_StorageServiceThrows_StoreStillUpdatesState()
    {
        // Arrange
        IStorageService storage = Substitute.For<IStorageService>();
        _ = storage.SetAsync(Arg.Any<string>(), Arg.Any<ThemeValue>(), Arg.Any<CancellationToken>())
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
        IThemeService themeService = Substitute.For<IThemeService>();
        string key = StorageKeys.BuildKey(TestTenant, TestUser, "theme");
        await storage.SetAsync(key, ThemeValue.Dark, ct);
        ILogger<ThemeEffects> logger = Substitute.For<ILogger<ThemeEffects>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        var sut = new ThemeEffects(storage, MsOptions.Create(new Contracts.FcShellOptions()), StubAccessor(TestTenant, TestUser), logger, themeService);
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
        IThemeService themeService = Substitute.For<IThemeService>();
        ILogger<ThemeEffects> logger = Substitute.For<ILogger<ThemeEffects>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        var sut = new ThemeEffects(storage, MsOptions.Create(new Contracts.FcShellOptions()), StubAccessor(TestTenant, TestUser), logger, themeService);
        var action = new AppInitializedAction("corr-init");

        // Act
        await sut.HandleAppInitialized(action, dispatcher);

        // Assert — no dispatch because no stored theme preference exists
        dispatcher.DidNotReceiveWithAnyArgs().Dispatch(default!);
    }

    [Fact]
    public async Task HandleThemeChanged_PersistsToStorage()
    {
        // Arrange
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        var storage = new InMemoryStorageService();
        IThemeService themeService = Substitute.For<IThemeService>();
        ILogger<ThemeEffects> logger = Substitute.For<ILogger<ThemeEffects>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        var sut = new ThemeEffects(storage, MsOptions.Create(new Contracts.FcShellOptions()), StubAccessor(TestTenant, TestUser), logger, themeService);
        var action = new ThemeChangedAction("corr-1", ThemeValue.Dark);
        string key = StorageKeys.BuildKey(TestTenant, TestUser, "theme");

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
        _ = storage.SetAsync(Arg.Any<string>(), Arg.Any<ThemeValue>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Storage failure"));
        IThemeService themeService = Substitute.For<IThemeService>();
        ILogger<ThemeEffects> logger = Substitute.For<ILogger<ThemeEffects>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        var sut = new ThemeEffects(storage, MsOptions.Create(new Contracts.FcShellOptions()), StubAccessor(TestTenant, TestUser), logger, themeService);
        var action = new ThemeChangedAction("corr-1", ThemeValue.Dark);

        // Act — should not throw
        await sut.HandleThemeChanged(action, dispatcher);

        // Assert — logger was called (warning level)
        logger.ReceivedWithAnyArgs(1).Log(
            LogLevel.Warning,
            default,
            default!,
            default,
            default!);
    }

    private static IUserContextAccessor StubAccessor(string? tenantId, string? userId)
    {
        IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
        accessor.TenantId.Returns(tenantId);
        accessor.UserId.Returns(userId);
        return accessor;
    }

    private static ServiceProvider BuildProvider(IStorageService storage)
    {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddFluxor(o => o.ScanAssemblies(typeof(FrontComposerThemeState).Assembly));
        _ = services.AddScoped(_ => storage);
        _ = services.AddScoped(_ => StubAccessor(TestTenant, TestUser));
        _ = services.AddScoped<IThemeService>(_ => Substitute.For<IThemeService>());
        _ = services.AddOptions<Contracts.FcShellOptions>();
        // Story 3-5 — CapabilityDiscoveryEffects auto-registers via Fluxor scan; supply the
        // dependencies it needs even though this test never exercises the badge feature.
        _ = services.AddSingleton<Hexalith.FrontComposer.Contracts.Badges.IActionQueueProjectionCatalog>(_ =>
            new EmptyActionQueueProjectionCatalog());
        _ = services.AddScoped<Hexalith.FrontComposer.Contracts.Badges.IActionQueueCountReader,
            Hexalith.FrontComposer.Shell.Badges.NullActionQueueCountReader>();
        _ = services.AddScoped<Hexalith.FrontComposer.Contracts.Badges.IBadgeCountService,
            Hexalith.FrontComposer.Shell.Badges.BadgeCountService>();
        _ = services.AddScoped<Hexalith.FrontComposer.Shell.State.CapabilityDiscovery.CapabilityDiscoveryEffects>();
        _ = services.AddSingleton(TimeProvider.System);
        return services.BuildServiceProvider();
    }

    private sealed class EmptyActionQueueProjectionCatalog : Hexalith.FrontComposer.Contracts.Badges.IActionQueueProjectionCatalog
    {
        public IReadOnlyList<Type> ActionQueueTypes { get; } = Array.Empty<Type>();
    }

    private static bool WaitFor(Func<bool> condition)
        => SpinWait.SpinUntil(condition, TimeSpan.FromSeconds(1));
}
