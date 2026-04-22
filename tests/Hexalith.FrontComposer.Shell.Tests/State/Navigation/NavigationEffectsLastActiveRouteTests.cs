using System.Collections.Immutable;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.Navigation;

/// <summary>
/// Story 3-6 Task 7 — round-trip tests for <c>LastActiveRoute</c> persist + hydrate + D21 prune.
/// </summary>
public sealed class NavigationEffectsLastActiveRouteTests {
    private const string Tenant = "acme";
    private const string User = "alice";

    private static FrontComposerNavigationState BaseState(string? lastActiveRoute = null) => new(
        SidebarCollapsed: false,
        CollapsedGroups: ImmutableDictionary<string, bool>.Empty.WithComparers(StringComparer.Ordinal),
        CurrentViewport: ViewportTier.Desktop,
        LastActiveRoute: lastActiveRoute);

    private static IState<FrontComposerNavigationState> FakeState(FrontComposerNavigationState value) {
        IState<FrontComposerNavigationState> state = Substitute.For<IState<FrontComposerNavigationState>>();
        state.Value.Returns(value);
        return state;
    }

    private static IUserContextAccessor MakeAccessor() {
        IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
        accessor.TenantId.Returns(Tenant);
        accessor.UserId.Returns(User);
        return accessor;
    }

    private static IServiceProvider BuildServiceProvider(NavigationManager? navigation = null) {
        ServiceCollection services = new();
        if (navigation is not null) {
            services.AddSingleton(navigation);
        }

        return services.BuildServiceProvider();
    }

    private sealed class TestNavigationManager : NavigationManager {
        public TestNavigationManager(string baseUri, string currentUri) {
            Initialize(baseUri, currentUri);
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
            => Initialize(BaseUri, ToAbsoluteUri(uri).ToString());
    }

    [Fact]
    public async Task HandleBoundedContextChanged_NullBc_DoesNotDispatchRouteChange() {
        ILogger<NavigationEffects> logger = Substitute.For<ILogger<NavigationEffects>>();
        var storage = new InMemoryStorageService();
        var sut = new NavigationEffects(storage, MakeAccessor(), logger, FakeState(BaseState()));
        IDispatcher dispatcher = Substitute.For<IDispatcher>();

        await sut.HandleBoundedContextChanged(new BoundedContextChangedAction(null), dispatcher);

        dispatcher.DidNotReceiveWithAnyArgs().Dispatch(Arg.Any<LastActiveRouteChangedAction>());
    }

    [Fact]
    public async Task HandleLastActiveRouteChanged_PersistsUpdatedBlob() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        ILogger<NavigationEffects> logger = Substitute.For<ILogger<NavigationEffects>>();
        var storage = new InMemoryStorageService();
        var sut = new NavigationEffects(
            storage,
            MakeAccessor(),
            logger,
            FakeState(BaseState(lastActiveRoute: "/domain/counter/counter-list")));
        IDispatcher dispatcher = Substitute.For<IDispatcher>();

        await sut.HandleLastActiveRouteChanged(
            new LastActiveRouteChangedAction("c1", "/domain/counter/counter-list"),
            dispatcher);

        string key = StorageKeys.BuildKey(Tenant, User, "nav");
        NavigationPersistenceBlob? stored = await storage.GetAsync<NavigationPersistenceBlob>(key, ct);
        stored.ShouldNotBeNull();
        stored.LastActiveRoute.ShouldBe("/domain/counter/counter-list");
    }

    [Fact]
    public async Task HandleBoundedContextChanged_NormalizesCurrentUriToBaseRelativeRoute() {
        ILogger<NavigationEffects> logger = Substitute.For<ILogger<NavigationEffects>>();
        var storage = new InMemoryStorageService();
        NavigationManager navigation = new TestNavigationManager(
            "https://localhost/app/",
            "https://localhost/app/domain/counter/counter-list?tab=1#recent");
        var sut = new NavigationEffects(
            storage,
            MakeAccessor(),
            logger,
            FakeState(BaseState()),
            BuildServiceProvider(navigation));
        IDispatcher dispatcher = Substitute.For<IDispatcher>();

        await sut.HandleBoundedContextChanged(new BoundedContextChangedAction("counter"), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Is<LastActiveRouteChangedAction>(a =>
            a.Route == "domain/counter/counter-list?tab=1#recent"));
    }

    [Fact]
    public async Task HandleAppInitialized_EmptyBlob_DispatchesHydratedCompleted() {
        ILogger<NavigationEffects> logger = Substitute.For<ILogger<NavigationEffects>>();
        var storage = new InMemoryStorageService();
        var sut = new NavigationEffects(storage, MakeAccessor(), logger, FakeState(BaseState()));
        IDispatcher dispatcher = Substitute.For<IDispatcher>();

        await sut.HandleAppInitialized(new AppInitializedAction("c1"), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Any<NavigationHydratingAction>());
        dispatcher.Received(1).Dispatch(Arg.Any<NavigationHydratedCompletedAction>());
        // Story 3-6 Review F-EH-002 / F-EH-012: empty blob must dispatch the full reset triple so
        // prior in-memory state (from a different user in the same circuit) is cleared. Previously
        // this branch silently left state untouched, enabling cross-user leak.
        dispatcher.Received(1).Dispatch(Arg.Is<LastActiveRouteHydratedAction>(a => a.Route == null));
        dispatcher.Received(1).Dispatch(Arg.Is<NavigationHydratedAction>(a =>
            a.SidebarCollapsed == false && a.CollapsedGroups.Count == 0));
    }

    [Fact]
    public async Task HandleAppInitialized_StoredRoute_DispatchesHydratedActions() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        ILogger<NavigationEffects> logger = Substitute.For<ILogger<NavigationEffects>>();
        var storage = new InMemoryStorageService();
        string key = StorageKeys.BuildKey(Tenant, User, "nav");
        NavigationPersistenceBlob blob = new(
            SidebarCollapsed: false,
            CollapsedGroups: new Dictionary<string, bool>(StringComparer.Ordinal),
            LastActiveRoute: "/domain/counter/counter-list");
        await storage.SetAsync(key, blob, ct);

        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns(new List<DomainManifest> {
            new("Counter", "counter", Array.Empty<string>(), Array.Empty<string>()),
        });

        var sut = new NavigationEffects(
            storage, MakeAccessor(), logger, FakeState(BaseState()),
            serviceProvider: null, registry: registry);
        IDispatcher dispatcher = Substitute.For<IDispatcher>();

        await sut.HandleAppInitialized(new AppInitializedAction("c1"), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Is<LastActiveRouteHydratedAction>(a =>
            a.Route == "domain/counter/counter-list"));
    }

    [Fact]
    public async Task HandleAppInitialized_AbsoluteStoredRoute_NormalizesToBaseRelative() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        ILogger<NavigationEffects> logger = Substitute.For<ILogger<NavigationEffects>>();
        var storage = new InMemoryStorageService();
        string key = StorageKeys.BuildKey(Tenant, User, "nav");
        NavigationPersistenceBlob blob = new(
            SidebarCollapsed: false,
            CollapsedGroups: new Dictionary<string, bool>(StringComparer.Ordinal),
            LastActiveRoute: "https://localhost/app/domain/counter/counter-list?tab=1#recent");
        await storage.SetAsync(key, blob, ct);

        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns(new List<DomainManifest> {
            new("Counter", "counter", Array.Empty<string>(), Array.Empty<string>()),
        });

        NavigationManager navigation = new TestNavigationManager("https://localhost/app/", "https://localhost/app/");
        var sut = new NavigationEffects(
            storage,
            MakeAccessor(),
            logger,
            FakeState(BaseState()),
            BuildServiceProvider(navigation),
            registry);
        IDispatcher dispatcher = Substitute.For<IDispatcher>();

        await sut.HandleAppInitialized(new AppInitializedAction("c1"), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Is<LastActiveRouteHydratedAction>(a =>
            a.Route == "domain/counter/counter-list?tab=1#recent"));
    }

    [Fact]
    public async Task HandleAppInitialized_ExternalAbsoluteStoredRoute_PrunesToNull() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        ILogger<NavigationEffects> logger = Substitute.For<ILogger<NavigationEffects>>();
        var storage = new InMemoryStorageService();
        string key = StorageKeys.BuildKey(Tenant, User, "nav");
        NavigationPersistenceBlob blob = new(
            SidebarCollapsed: false,
            CollapsedGroups: new Dictionary<string, bool>(StringComparer.Ordinal),
            LastActiveRoute: "https://evil.example/domain/counter/counter-list");
        await storage.SetAsync(key, blob, ct);

        NavigationManager navigation = new TestNavigationManager("https://localhost/app/", "https://localhost/app/");
        var sut = new NavigationEffects(
            storage,
            MakeAccessor(),
            logger,
            FakeState(BaseState()),
            BuildServiceProvider(navigation));
        IDispatcher dispatcher = Substitute.For<IDispatcher>();

        await sut.HandleAppInitialized(new AppInitializedAction("c1"), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Is<LastActiveRouteHydratedAction>(a => a.Route == null));
    }

    [Fact]
    public async Task HandleAppInitialized_UnregisteredBc_DispatchesNullHydratedAction() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        ILogger<NavigationEffects> logger = Substitute.For<ILogger<NavigationEffects>>();
        var storage = new InMemoryStorageService();
        string key = StorageKeys.BuildKey(Tenant, User, "nav");
        NavigationPersistenceBlob blob = new(
            SidebarCollapsed: false,
            CollapsedGroups: new Dictionary<string, bool>(StringComparer.Ordinal),
            LastActiveRoute: "/domain/deleted-bc/x");
        await storage.SetAsync(key, blob, ct);

        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns(Array.Empty<DomainManifest>());

        var sut = new NavigationEffects(
            storage, MakeAccessor(), logger,
            FakeState(BaseState(lastActiveRoute: "/domain/deleted-bc/x")),
            serviceProvider: null, registry: registry);
        IDispatcher dispatcher = Substitute.For<IDispatcher>();

        await sut.HandleAppInitialized(new AppInitializedAction("c1"), dispatcher);

        // D21 — prune path emits the null LastActiveRouteChanged + null LastActiveRouteHydrated.
        dispatcher.Received(1).Dispatch(Arg.Is<LastActiveRouteHydratedAction>(a => a.Route == null));
    }
}
