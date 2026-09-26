using System.Collections.Immutable;
using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Badges;
using Hexalith.FrontComposer.Shell.Badges;
using Hexalith.FrontComposer.Shell.State.CapabilityDiscovery;
using Hexalith.FrontComposer.Shell.State.CommandPalette;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State;
using Hexalith.FrontComposer.Shell.State.Density;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State;

/// <summary>
/// Integration tests for state hydration round-trips via Fluxor store.
/// </summary>
public class HydrationTests : FrontComposerTestBase {

    [Fact]
    public async Task DensityHydration_StorageContainsValue_DispatchesRestoredDensity() {
        // Arrange
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        IStorageService storage = Services.GetRequiredService<IStorageService>();
        await storage.SetAsync($"{TestTenantId}:{TestUserId}:density", DensityLevel.Compact, ct);
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        IState<FrontComposerDensityState> densityState = Services.GetRequiredService<IState<FrontComposerDensityState>>();

        // Act
        dispatcher.Dispatch(new AppInitializedAction("hydrate-3"));
        await Task.Delay(100, ct);

        // Assert — bootstrap caps placeholder Desktop to Comfortable until the breakpoint watcher
        // emits the measured tier, preventing an initial mobile/desktop mismatch.
        densityState.Value.EffectiveDensity.ShouldBe(DensityLevel.Comfortable);
    }

    [Fact]
    public async Task DensityHydration_StorageEmpty_UsesBootstrapComfortableUntilViewportMeasured() {
        // Arrange — no seeding
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        IState<FrontComposerDensityState> densityState = Services.GetRequiredService<IState<FrontComposerDensityState>>();

        // Act
        dispatcher.Dispatch(new AppInitializedAction("hydrate-4"));
        await Task.Delay(100, ct);

        // Assert
        densityState.Value.EffectiveDensity.ShouldBe(DensityLevel.Comfortable);
    }

    [Fact]
    public async Task DensityHydration_StorageEmpty_DesktopViewportRecomputesToCompact() {
        // Arrange — no seeding
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        IState<FrontComposerDensityState> densityState = Services.GetRequiredService<IState<FrontComposerDensityState>>();

        // Act
        dispatcher.Dispatch(new AppInitializedAction("hydrate-5"));
        await Task.Delay(100, ct);
        dispatcher.Dispatch(new ViewportTierChangedAction(ViewportTier.Desktop));
        await Task.Delay(100, ct);

        // Assert — real Desktop measurement falls through to the new Compact factory default.
        densityState.Value.UserPreference.ShouldBeNull();
        densityState.Value.EffectiveDensity.ShouldBe(DensityLevel.Compact);
    }

    [Fact]
    public async Task ThemeHydration_StorageContainsValue_DispatchesRestoredTheme() {
        // Arrange — pre-seed storage before store init
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        IStorageService storage = Services.GetRequiredService<IStorageService>();
        await storage.SetAsync($"{TestTenantId}:{TestUserId}:theme", ThemeValue.Dark, ct);
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
    public async Task ThemeHydration_StorageEmpty_UsesDefaultLight() {
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
}

/// <summary>Dispatch can cross a scope boundary after the effect's final scope validation.</summary>
public sealed class HydrationScopeGenerationRaceTests {
    private sealed class EmptyCatalog : IActionQueueProjectionCatalog {
        public IReadOnlyList<Type> ActionQueueTypes { get; } = [];
    }

    private static IUserContextAccessor Accessor(Func<string> tenant) {
        IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
        accessor.TenantId.Returns(_ => tenant());
        accessor.UserId.Returns("user");
        return accessor;
    }

    [Fact]
    public void NavigationHydrated_PriorGeneration_IsIgnoredAfterScopeChange() {
        FrontComposerNavigationState b = NavigationReducers.ReduceScopeChanged(
            new FrontComposerNavigationState(false, ImmutableDictionary<string, bool>.Empty, ViewportTier.Desktop),
            new ScopeChangedAction());
        NavigationHydratedAction stale = new(true, ImmutableDictionary<string, bool>.Empty.Add("Orders", true)) {
            OriginScopeGeneration = b.ScopeGeneration - 1,
        };

        NavigationReducers.ReduceNavigationHydrated(b, stale).ShouldBeSameAs(b);
        NavigationReducers.ReduceNavigationHydrated(b, stale with { OriginScopeGeneration = null }).ShouldBeSameAs(b);
        NavigationReducers.ReduceNavigationHydrated(b, stale with { OriginScopeGeneration = b.ScopeGeneration })
            .SidebarCollapsed.ShouldBeTrue();
    }

    [Fact]
    public void LastActiveRouteHydrated_PriorGeneration_IsIgnoredAfterScopeChange() {
        FrontComposerNavigationState b = NavigationReducers.ReduceScopeChanged(
            new FrontComposerNavigationState(false, ImmutableDictionary<string, bool>.Empty, ViewportTier.Desktop),
            new ScopeChangedAction());
        LastActiveRouteHydratedAction stale = new("/tenant-a/orders") { OriginScopeGeneration = b.ScopeGeneration - 1 };

        NavigationReducers.ReduceLastActiveRouteHydrated(b, stale).ShouldBeSameAs(b);
        NavigationReducers.ReduceLastActiveRouteHydrated(b, stale with { OriginScopeGeneration = null }).ShouldBeSameAs(b);
        NavigationReducers.ReduceLastActiveRouteHydrated(b, stale with { OriginScopeGeneration = b.ScopeGeneration })
            .LastActiveRoute.ShouldBe("/tenant-a/orders");
    }

    [Fact]
    public void DensityHydrated_PriorGeneration_IsIgnoredAfterScopeChange() {
        FrontComposerDensityState b = DensityReducers.ReduceScopeChanged(
            new FrontComposerDensityState(null, DensityLevel.Comfortable),
            new ScopeChangedAction());
        DensityHydratedAction stale = new(DensityLevel.Compact, DensityLevel.Compact) { OriginScopeGeneration = b.ScopeGeneration - 1 };

        DensityReducers.ReduceDensityHydrated(b, stale).ShouldBeSameAs(b);
        DensityReducers.ReduceDensityHydrated(b, stale with { OriginScopeGeneration = null }).ShouldBeSameAs(b);
        DensityReducers.ReduceDensityHydrated(b, stale with { OriginScopeGeneration = b.ScopeGeneration })
            .UserPreference.ShouldBe(DensityLevel.Compact);
    }

    [Fact]
    public void GridViewHydrated_PriorGeneration_IsIgnoredAfterScopeChange() {
        DataGridNavigationState b = DataGridNavigationReducers.ReduceScopeChanged(
            new DataGridNavigationState(ImmutableDictionary<string, GridViewSnapshot>.Empty, Cap: 50),
            new ScopeChangedAction());
        GridViewSnapshot snapshot = new(
            scrollTop: 0,
            filters: ImmutableDictionary<string, string>.Empty.Add("Status", "tenant-a"),
            sortColumn: null, sortDescending: false,
            expandedRowId: null, selectedRowId: null,
            capturedAt: DateTimeOffset.UtcNow);
        GridViewHydratedAction stale = new("Orders.Row", snapshot) { OriginScopeGeneration = b.ScopeGeneration - 1 };

        DataGridNavigationReducers.ReduceGridViewHydrated(b, stale).ShouldBeSameAs(b);
        DataGridNavigationReducers.ReduceGridViewHydrated(b, stale with { OriginScopeGeneration = null }).ShouldBeSameAs(b);
        DataGridNavigationReducers.ReduceGridViewHydrated(b, stale with { OriginScopeGeneration = b.ScopeGeneration })
            .ViewStates.ShouldContainKey("Orders.Row");
    }

    [Fact]
    public void PaletteHydrated_PriorGeneration_IsIgnoredAfterScopeChange() {
        FrontComposerCommandPaletteState b = CommandPaletteReducers.ReduceScopeChanged(
            new FrontComposerCommandPaletteState(false, string.Empty, [], [], 0, PaletteLoadState.Idle),
            new ScopeChangedAction());
        PaletteHydratedAction stale = new(["/tenant-a/orders"]) { OriginScopeGeneration = b.ScopeGeneration - 1 };

        CommandPaletteReducers.ReducePaletteHydrated(b, stale).ShouldBeSameAs(b);
        CommandPaletteReducers.ReducePaletteHydrated(b, stale with { OriginScopeGeneration = null }).ShouldBeSameAs(b);
        CommandPaletteReducers.ReducePaletteHydrated(b, stale with { OriginScopeGeneration = b.ScopeGeneration })
            .RecentRouteUrls.ShouldBe(["/tenant-a/orders"]);
    }

    [Fact]
    public void BadgeCountsSeeded_PriorGeneration_IsIgnoredAfterScopeChange() {
        FrontComposerCapabilityDiscoveryState b = CapabilityDiscoveryReducers.ReduceScopeChanged(
            FrontComposerCapabilityDiscoveryState.Empty,
            new ScopeChangedAction());
        BadgeCountsSeededAction stale = new(ImmutableDictionary<Type, int>.Empty.Add(typeof(EmptyCatalog), 7)) {
            OriginScopeGeneration = b.ScopeGeneration - 1,
        };

        CapabilityDiscoveryReducers.ReduceBadgeCountsSeeded(b, stale).ShouldBeSameAs(b);
        CapabilityDiscoveryReducers.ReduceBadgeCountsSeeded(b, stale with { OriginScopeGeneration = null }).ShouldBeSameAs(b);
        CapabilityDiscoveryReducers.ReduceBadgeCountsSeeded(b, stale with { OriginScopeGeneration = b.ScopeGeneration })
            .Counts[typeof(EmptyCatalog)].ShouldBe(7);
    }

    [Fact]
    public async Task CapabilityDiscovery_BoundaryAtHydratedDispatch_DropsAAndAcceptsB() {
        string tenant = "a";
        InMemoryStorageService storage = new();
        await storage.SetAsync(StorageKeys.BuildKey("a", "user", "capability-seen"),
            ImmutableHashSet<string>.Empty.Add("a-capability"), Xunit.TestContext.Current.CancellationToken);
        await storage.SetAsync(StorageKeys.BuildKey("b", "user", "capability-seen"),
            ImmutableHashSet<string>.Empty.Add("b-capability"), Xunit.TestContext.Current.CancellationToken);
        FrontComposerCapabilityDiscoveryState current = FrontComposerCapabilityDiscoveryState.Empty;
        IState<FrontComposerCapabilityDiscoveryState> state = Substitute.For<IState<FrontComposerCapabilityDiscoveryState>>();
        state.Value.Returns(_ => current);
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        bool switched = false;
        dispatcher.When(d => d.Dispatch(Arg.Any<SeenCapabilitiesHydratedAction>())).Do(call => {
            SeenCapabilitiesHydratedAction action = call.Arg<SeenCapabilitiesHydratedAction>();
            if (!switched) {
                switched = true;
                tenant = "b";
                current = CapabilityDiscoveryReducers.ReduceScopeChanged(current, new ScopeChangedAction());
                CapabilityDiscoveryReducers.ReduceSeenCapabilitiesHydrated(current, action).ShouldBeSameAs(current);
            }
            current = CapabilityDiscoveryReducers.ReduceSeenCapabilitiesHydrated(current, action);
        });
        using BadgeCountService badges = new(new EmptyCatalog(), new NullActionQueueCountReader(),
            new ServiceCollection().BuildServiceProvider(),
            EnabledLoggerSubstitute.Create<BadgeCountService>(), new FakeTimeProvider());
        using CapabilityDiscoveryEffects effect = new(dispatcher, storage, Accessor(() => tenant), badges,
            state, EnabledLoggerSubstitute.Create<CapabilityDiscoveryEffects>());

        await effect.HandleAppInitialized(new AppInitializedAction("a"), dispatcher);
        current.SeenCapabilities.ShouldBeEmpty();
        await effect.HandleAppInitialized(new AppInitializedAction("b"), dispatcher);
        current.SeenCapabilities.ShouldContain("b-capability");
        current.SeenCapabilities.ShouldNotContain("a-capability");
    }

    [Fact]
    public async Task CommandPalette_BoundaryAtCompletionDispatch_DropsAAndCompletesB() {
        string tenant = "a";
        FrontComposerCommandPaletteState current = new(false, string.Empty, [], [], 0, PaletteLoadState.Idle);
        IState<FrontComposerCommandPaletteState> state = Substitute.For<IState<FrontComposerCommandPaletteState>>();
        state.Value.Returns(_ => current);
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        bool switched = false;
        dispatcher.When(d => d.Dispatch(Arg.Any<PaletteHydratedCompletedAction>())).Do(call => {
            PaletteHydratedCompletedAction action = call.Arg<PaletteHydratedCompletedAction>();
            if (!switched) {
                switched = true;
                tenant = "b";
                current = CommandPaletteReducers.ReduceScopeChanged(current, new ScopeChangedAction());
                CommandPaletteReducers.ReducePaletteHydratedCompleted(current, action).ShouldBeSameAs(current);
            }
            current = CommandPaletteReducers.ReducePaletteHydratedCompleted(current, action);
        });
        ServiceCollection services = [];
        services.AddSingleton<IUserContextAccessor>(Accessor(() => tenant));
        services.AddSingleton<IStorageService>(new InMemoryStorageService());
        IState<FrontComposerNavigationState> navigation = Substitute.For<IState<FrontComposerNavigationState>>();
        navigation.Value.Returns(new FrontComposerNavigationState(false, ImmutableDictionary<string, bool>.Empty, ViewportTier.Desktop));
        using ServiceProvider provider = services.BuildServiceProvider();
        using CommandPaletteEffects effect = new(navigation, state,
            EnabledLoggerSubstitute.Create<CommandPaletteEffects>(), provider);

        await effect.HandleAppInitialized(new AppInitializedAction("a"), dispatcher);
        current.HydrationState.ShouldBe(HydrationState.Idle);
        await effect.HandleAppInitialized(new AppInitializedAction("b"), dispatcher);
        current.HydrationState.ShouldBe(HydrationState.Hydrated);
    }

    [Fact]
    public async Task DataGrid_BoundaryAtCompletionDispatch_DropsAAndCompletesB() {
        string tenant = "a";
        DataGridNavigationState current = new(ImmutableDictionary<string, GridViewSnapshot>.Empty);
        IState<DataGridNavigationState> state = Substitute.For<IState<DataGridNavigationState>>();
        state.Value.Returns(_ => current);
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        bool switched = false;
        dispatcher.When(d => d.Dispatch(Arg.Any<DataGridNavigationHydratedCompletedAction>())).Do(call => {
            DataGridNavigationHydratedCompletedAction action = call.Arg<DataGridNavigationHydratedCompletedAction>();
            if (!switched) {
                switched = true;
                tenant = "b";
                current = DataGridNavigationReducers.ReduceScopeChanged(current, new ScopeChangedAction());
                DataGridNavigationReducers.ReduceDataGridNavigationHydratedCompleted(current, action).ShouldBeSameAs(current);
            }
            current = DataGridNavigationReducers.ReduceDataGridNavigationHydratedCompleted(current, action);
        });
        using DataGridNavigationEffects effect = new(new InMemoryStorageService(), Accessor(() => tenant),
            EnabledLoggerSubstitute.Create<DataGridNavigationEffects>(), state);

        await effect.HandleAppInitialized(new AppInitializedAction("a"), dispatcher);
        current.HydrationState.ShouldBe(HydrationState.Idle);
        await effect.HandleAppInitialized(new AppInitializedAction("b"), dispatcher);
        current.HydrationState.ShouldBe(HydrationState.Hydrated);
    }

    [Fact]
    public async Task Density_BoundaryAtCompletionDispatch_DropsAAndCompletesB() {
        string tenant = "a";
        FrontComposerDensityState current = new(null, DensityLevel.Comfortable);
        IState<FrontComposerDensityState> state = Substitute.For<IState<FrontComposerDensityState>>();
        state.Value.Returns(_ => current);
        IState<FrontComposerNavigationState> navigation = Substitute.For<IState<FrontComposerNavigationState>>();
        navigation.Value.Returns(new FrontComposerNavigationState(false, ImmutableDictionary<string, bool>.Empty, ViewportTier.Desktop));
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        bool switched = false;
        dispatcher.When(d => d.Dispatch(Arg.Any<DensityHydratedCompletedAction>())).Do(call => {
            DensityHydratedCompletedAction action = call.Arg<DensityHydratedCompletedAction>();
            if (!switched) {
                switched = true;
                tenant = "b";
                current = DensityReducers.ReduceScopeChanged(current, new ScopeChangedAction());
                DensityReducers.ReduceDensityHydratedCompleted(current, action).ShouldBeSameAs(current);
            }
            current = DensityReducers.ReduceDensityHydratedCompleted(current, action);
        });
        DensityEffects effect = new(new InMemoryStorageService(), Accessor(() => tenant),
            EnabledLoggerSubstitute.Create<DensityEffects>(), navigation, Microsoft.Extensions.Options.Options.Create(new FcShellOptions()), state);

        await effect.HandleAppInitialized(new AppInitializedAction("a"), dispatcher);
        current.HydrationState.ShouldBe(HydrationState.Idle);
        await effect.HandleAppInitialized(new AppInitializedAction("b"), dispatcher);
        current.HydrationState.ShouldBe(HydrationState.Hydrated);
    }

    [Fact]
    public async Task Navigation_BoundaryAtCompletionDispatch_DropsAAndCompletesB() {
        string tenant = "a";
        FrontComposerNavigationState current = new(false, ImmutableDictionary<string, bool>.Empty, ViewportTier.Desktop);
        IState<FrontComposerNavigationState> state = Substitute.For<IState<FrontComposerNavigationState>>();
        state.Value.Returns(_ => current);
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        bool switched = false;
        dispatcher.When(d => d.Dispatch(Arg.Any<NavigationHydratedCompletedAction>())).Do(call => {
            NavigationHydratedCompletedAction action = call.Arg<NavigationHydratedCompletedAction>();
            if (!switched) {
                switched = true;
                tenant = "b";
                current = NavigationReducers.ReduceScopeChanged(current, new ScopeChangedAction());
                NavigationReducers.ReduceNavigationHydratedCompleted(current, action).ShouldBeSameAs(current);
            }
            current = NavigationReducers.ReduceNavigationHydratedCompleted(current, action);
        });
        NavigationEffects effect = new(new InMemoryStorageService(), Accessor(() => tenant),
            EnabledLoggerSubstitute.Create<NavigationEffects>(), state);

        await effect.HandleAppInitialized(new AppInitializedAction("a"), dispatcher);
        current.HydrationState.ShouldBe(HydrationState.Idle);
        await effect.HandleAppInitialized(new AppInitializedAction("b"), dispatcher);
        current.HydrationState.ShouldBe(HydrationState.Hydrated);
    }
}
