using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Badges;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Badges;
using Hexalith.FrontComposer.Shell.State.CapabilityDiscovery;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

namespace Hexalith.FrontComposer.Shell.Tests;

/// <summary>
/// Base test class that pre-configures Fluxor, storage, a stub <see cref="IUserContextAccessor"/>,
/// and the override registry for bUnit tests. The Fluxor store is initialized during construction,
/// and <see cref="InitializeStoreAsync"/> remains safe to call when a test wants explicit sequencing.
/// </summary>
public abstract class FrontComposerTestBase : BunitContext {
    /// <summary>Tenant segment used by the test-scoped <see cref="IUserContextAccessor"/>.</summary>
    public const string TestTenantId = "test-tenant";

    /// <summary>User segment used by the test-scoped <see cref="IUserContextAccessor"/>.</summary>
    public const string TestUserId = "test-user";

    private bool _storeInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrontComposerTestBase"/> class.
    /// Registers all services needed for Shell component testing and initializes the Fluxor store.
    /// </summary>
    protected FrontComposerTestBase() {
        _ = Services.AddFluxor(o => o.ScanAssemblies(typeof(FrontComposerThemeState).Assembly));
        _ = Services.AddScoped<IStorageService, InMemoryStorageService>();
        _ = Services.AddScoped<IUserContextAccessor>(_ => {
            IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
            accessor.TenantId.Returns(TestTenantId);
            accessor.UserId.Returns(TestUserId);
            return accessor;
        });
        _ = Services.AddSingleton(Substitute.For<IOverrideRegistry>());
        _ = Services.AddLogging();

        // Story 3-5 — Fluxor scans the Shell assembly for [EffectMethod] handlers, which auto-
        // registers CapabilityDiscoveryEffects. Its constructor needs the badge services even
        // when the test does not exercise them, so wire them with the null defaults.
        _ = Services.AddSingleton<IActionQueueProjectionCatalog>(_ =>
            new EmptyActionQueueProjectionCatalog());
        _ = Services.AddScoped<IActionQueueCountReader, NullActionQueueCountReader>();
        _ = Services.AddScoped<IBadgeCountService, BadgeCountService>();
        _ = Services.AddScoped<CapabilityDiscoveryEffects>();
        _ = Services.AddSingleton(TimeProvider.System);

        // Story 3-6 — Fluxor assembly scan picks up ScopeFlipObserverEffect + DataGridNavigationEffects;
        // their dependencies must be resolvable in the test host.
        _ = Services.AddScoped<IUlidFactory>(_ => new Hexalith.FrontComposer.Shell.Services.Lifecycle.UlidFactory());
        _ = Services.AddScoped<IScopeReadinessGate, ScopeReadinessGate>();
        _ = Services.AddScoped<ScopeFlipObserverEffect>();
        _ = Services.AddScoped<IFrontComposerRegistry>(_ => {
            IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
            registry.GetManifests().Returns(Array.Empty<DomainManifest>());
            return registry;
        });
        _ = Services.AddScoped<DataGridNavigationEffects>();

        // Story 4-4 — Fluxor scan picks up LoadPageEffects + ScrollPersistenceEffect +
        // ColumnVisibilityPersistenceEffect + LoadedPageReducers; their dependencies must resolve
        // in the test host. Default to the no-op loader (adopter tests override via Services.Replace).
        _ = Services.AddScoped<IProjectionPageLoader, NullProjectionPageLoader>();
        _ = Services.AddScoped<LoadedPageReducers>();
        _ = Services.AddScoped<LoadPageEffects>();
        _ = Services.AddScoped<ScrollPersistenceEffect>();
        _ = Services.AddScoped<ColumnVisibilityPersistenceEffect>();

        InitializeStoreAsync().GetAwaiter().GetResult();
    }

    private sealed class EmptyActionQueueProjectionCatalog : IActionQueueProjectionCatalog {
        public IReadOnlyList<Type> ActionQueueTypes { get; } = Array.Empty<Type>();
    }

    /// <summary>
    /// Initializes the Fluxor store if it has not already been initialized.
    /// Leaves feature hydration state untouched; tests that need hydration must dispatch the
    /// corresponding app-lifecycle actions explicitly.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task InitializeStoreAsync() {
        if (_storeInitialized) {
            return;
        }

        IStore store = Services.GetRequiredService<IStore>();
        await store.InitializeAsync().ConfigureAwait(false);
        _storeInitialized = true;
    }
}
