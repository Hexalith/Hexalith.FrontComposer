// ATDD RED PHASE — Story 3-3 Task 10.5 (D7, D8; AC3, AC4)
// Fails at compile until:
//   Task 2.3 — UserPreferenceChangedAction / DensityHydratedAction / EffectiveDensityRecomputedAction
//   Task 3.1 — DensityEffects constructor expanded with IState<FrontComposerNavigationState> + IOptions<FcShellOptions>
//   Task 3.2/3.3 — HandleViewportTierChanged + rewritten HandleAppInitialized

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State;
using Hexalith.FrontComposer.Shell.State.Density;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace Hexalith.FrontComposer.Shell.Tests.State.Density;

/// <summary>
/// Unit tests for the rewritten <see cref="DensityEffects"/> (Story 3-3 Task 3).
/// Covers user-preference persistence, viewport-driven recompute, and hydrate dispatch.
/// </summary>
public class DensityEffectsTests
{
    private const string TestTenant = "tenant-a";
    private const string TestUser = "user-1";

    [Fact]
    public async Task HandleAppInitialized_StorageContainsValue_DispatchesDensityHydrated()
    {
        // Arrange
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        InMemoryStorageService storage = new();
        string key = StorageKeys.BuildKey(TestTenant, TestUser, "density");
        await storage.SetAsync<DensityLevel?>(key, DensityLevel.Compact, ct);
        ILogger<DensityEffects> logger = Substitute.For<ILogger<DensityEffects>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        IState<FrontComposerNavigationState> navState = FakeNavState(ViewportTier.Desktop);
        IOptions<FcShellOptions> options = MsOptions.Create(new FcShellOptions());

        DensityEffects sut = new(storage, StubAccessor(TestTenant, TestUser), logger, navState, options);

        // Act
        await sut.HandleAppInitialized(new AppInitializedAction("corr-init"), dispatcher);

        // Assert — hydrate path dispatches DensityHydratedAction (NOT the legacy DensityChangedAction).
        dispatcher.Received(1).Dispatch(
            Arg.Is<DensityHydratedAction>(a =>
                a.UserPreference == DensityLevel.Compact &&
                a.NewEffective == DensityLevel.Compact));
    }

    [Fact]
    public async Task HandleViewportTierChanged_DispatchesEffectiveDensityRecomputed()
    {
        // D7 — cross-feature handler re-resolves and emits an intra-feature recompute action.
        InMemoryStorageService storage = new();
        ILogger<DensityEffects> logger = Substitute.For<ILogger<DensityEffects>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        // Navigation state exposes Desktop initially; the action carries the new tier (Tablet).
        IState<FrontComposerNavigationState> navState = FakeNavState(ViewportTier.Desktop);
        IOptions<FcShellOptions> options = MsOptions.Create(new FcShellOptions());

        DensityEffects sut = new(storage, StubAccessor(TestTenant, TestUser), logger, navState, options);

        await sut.HandleViewportTierChanged(new ViewportTierChangedAction(ViewportTier.Tablet), dispatcher);

        dispatcher.Received(1).Dispatch(
            Arg.Is<EffectiveDensityRecomputedAction>(a => a.NewEffective == DensityLevel.Comfortable));
    }

    [Fact]
    public async Task HandleUserPreferenceChanged_PersistsToStorage()
    {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        InMemoryStorageService storage = new();
        ILogger<DensityEffects> logger = Substitute.For<ILogger<DensityEffects>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        IState<FrontComposerNavigationState> navState = FakeNavState(ViewportTier.Desktop);
        IOptions<FcShellOptions> options = MsOptions.Create(new FcShellOptions());

        DensityEffects sut = new(storage, StubAccessor(TestTenant, TestUser), logger, navState, options);

        await sut.HandleUserPreferenceChanged(
            new UserPreferenceChangedAction("c1", DensityLevel.Roomy, DensityLevel.Roomy),
            dispatcher);

        string key = StorageKeys.BuildKey(TestTenant, TestUser, "density");
        DensityLevel? stored = await storage.GetAsync<DensityLevel?>(key, ct);
        stored.ShouldBe(DensityLevel.Roomy);
    }

    private static IUserContextAccessor StubAccessor(string? tenantId, string? userId)
    {
        IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
        accessor.TenantId.Returns(tenantId);
        accessor.UserId.Returns(userId);
        return accessor;
    }

    private static IState<FrontComposerNavigationState> FakeNavState(ViewportTier tier)
    {
        IState<FrontComposerNavigationState> state = Substitute.For<IState<FrontComposerNavigationState>>();
        state.Value.Returns(new FrontComposerNavigationState(
            SidebarCollapsed: false,
            CollapsedGroups: System.Collections.Immutable.ImmutableDictionary<string, bool>.Empty.WithComparers(StringComparer.Ordinal),
            CurrentViewport: tier));
        return state;
    }
}
