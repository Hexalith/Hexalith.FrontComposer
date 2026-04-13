namespace Hexalith.FrontComposer.Shell.Tests.State;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State;
using Hexalith.FrontComposer.Shell.State.Density;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

/// <summary>
/// Integration tests for state hydration round-trips via Fluxor store.
/// </summary>
public class HydrationTests : FrontComposerTestBase
{
    [Fact]
    public async Task ThemeHydration_StorageContainsValue_DispatchesRestoredTheme()
    {
        // Arrange — pre-seed storage before store init
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        IStorageService storage = Services.GetRequiredService<IStorageService>();
        await storage.SetAsync("default:anonymous:theme", ThemeValue.Dark, ct);
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        IState<FrontComposerThemeState> themeState = Services.GetRequiredService<IState<FrontComposerThemeState>>();

        // Act
        dispatcher.Dispatch(new AppInitializedAction("hydrate-1"));
        await Task.Delay(100, ct);

        // Assert
        themeState.Value.CurrentTheme.ShouldBe(ThemeValue.Dark);
    }

    [Fact]
    public async Task ThemeHydration_StorageEmpty_UsesDefaultLight()
    {
        // Arrange — no seeding
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        IState<FrontComposerThemeState> themeState = Services.GetRequiredService<IState<FrontComposerThemeState>>();

        // Act
        dispatcher.Dispatch(new AppInitializedAction("hydrate-2"));
        await Task.Delay(100, ct);

        // Assert
        themeState.Value.CurrentTheme.ShouldBe(ThemeValue.Light);
    }

    [Fact]
    public async Task DensityHydration_StorageContainsValue_DispatchesRestoredDensity()
    {
        // Arrange
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        IStorageService storage = Services.GetRequiredService<IStorageService>();
        await storage.SetAsync("default:anonymous:density", DensityLevel.Compact, ct);
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        IState<FrontComposerDensityState> densityState = Services.GetRequiredService<IState<FrontComposerDensityState>>();

        // Act
        dispatcher.Dispatch(new AppInitializedAction("hydrate-3"));
        await Task.Delay(100, ct);

        // Assert
        densityState.Value.CurrentDensity.ShouldBe(DensityLevel.Compact);
    }

    [Fact]
    public async Task DensityHydration_StorageEmpty_UsesDefaultComfortable()
    {
        // Arrange — no seeding
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        IState<FrontComposerDensityState> densityState = Services.GetRequiredService<IState<FrontComposerDensityState>>();

        // Act
        dispatcher.Dispatch(new AppInitializedAction("hydrate-4"));
        await Task.Delay(100, ct);

        // Assert
        densityState.Value.CurrentDensity.ShouldBe(DensityLevel.Comfortable);
    }
}
