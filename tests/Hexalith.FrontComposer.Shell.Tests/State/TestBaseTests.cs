using Fluxor;

using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State.CommandPalette;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
using Hexalith.FrontComposer.Shell.State.Density;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State;

/// <summary>
/// Tests verifying <see cref="FrontComposerTestBase"/> infrastructure.
/// </summary>
public class TestBaseTests : FrontComposerTestBase {

    [Fact]
    public void FrontComposerTestBase_ServicesRegistered_AllDependenciesResolve() {
        // Assert
        _ = Services.GetService<IStorageService>().ShouldNotBeNull();
        _ = Services.GetService<IOverrideRegistry>().ShouldNotBeNull();
        _ = Services.GetService<IDispatcher>().ShouldNotBeNull();
    }

    [Fact]
    public async Task FrontComposerTestBase_StoreInitialized_DispatchDoesNotThrow() {
        // Arrange
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

        // Act & Assert — should not throw
        dispatcher.Dispatch(new ThemeChangedAction("test-base-1", ThemeValue.Dark));
    }

    [Fact]
    public async Task FrontComposerTestBase_StoreInitialized_DoesNotPreSeedHydrationCompletion() {
        await InitializeStoreAsync();

        Services.GetRequiredService<IState<FrontComposerNavigationState>>().Value.HydrationState
            .ShouldBe(NavigationHydrationState.Idle);
        Services.GetRequiredService<IState<FrontComposerThemeState>>().Value.HydrationState
            .ShouldBe(ThemeHydrationState.Idle);
        Services.GetRequiredService<IState<FrontComposerDensityState>>().Value.HydrationState
            .ShouldBe(DensityHydrationState.Idle);
        Services.GetRequiredService<IState<DataGridNavigationState>>().Value.HydrationState
            .ShouldBe(DataGridNavigationHydrationState.Idle);
        Services.GetRequiredService<IState<FrontComposerCommandPaletteState>>().Value.HydrationState
            .ShouldBe(CommandPaletteHydrationState.Idle);
    }
}
