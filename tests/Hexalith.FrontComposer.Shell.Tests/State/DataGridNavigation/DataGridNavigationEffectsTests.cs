using System.Collections.Immutable;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.DataGridNavigation;

/// <summary>
/// Story 3-6 Task 7 — critical-path tests for <c>DataGridNavigationEffects</c> (ADR-050).
/// </summary>
public sealed class DataGridNavigationEffectsTests {
    private const string Tenant = "acme";
    private const string User = "alice";
    private const string ViewKey = "counter:Hexalith.Samples.Counter.Projections.CounterProjection";

    private static GridViewSnapshot Snap(double scroll = 123, string? sort = "name", bool desc = false) => new(
        scrollTop: scroll,
        filters: ImmutableDictionary<string, string>.Empty
            .WithComparers(StringComparer.Ordinal)
            .Add("Status", "Open"),
        sortColumn: sort,
        sortDescending: desc,
        expandedRowId: "row-42",
        selectedRowId: null,
        capturedAt: new DateTimeOffset(2026, 4, 22, 10, 0, 0, TimeSpan.Zero));

    private static IUserContextAccessor MakeAccessor() {
        IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
        accessor.TenantId.Returns(Tenant);
        accessor.UserId.Returns(User);
        return accessor;
    }

    private static IState<DataGridNavigationState> FakeState(GridViewSnapshot? snapshot = null) {
        ImmutableDictionary<string, GridViewSnapshot> views = snapshot is null
            ? ImmutableDictionary<string, GridViewSnapshot>.Empty
            : ImmutableDictionary<string, GridViewSnapshot>.Empty.Add(ViewKey, snapshot);
        IState<DataGridNavigationState> state = Substitute.For<IState<DataGridNavigationState>>();
        state.Value.Returns(new DataGridNavigationState(views, Cap: 50));
        return state;
    }

    private static IFrontComposerRegistry EmptyRegistry() {
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns(Array.Empty<DomainManifest>());
        return registry;
    }

    [Fact]
    public async Task HandleAppInitialized_DispatchesHydratingAndCompleted() {
        ILogger<DataGridNavigationEffects> logger = Substitute.For<ILogger<DataGridNavigationEffects>>();
        var storage = new InMemoryStorageService();
        var sut = new DataGridNavigationEffects(storage, MakeAccessor(), logger, FakeState(), EmptyRegistry());
        IDispatcher dispatcher = Substitute.For<IDispatcher>();

        await sut.HandleAppInitialized(new AppInitializedAction("c1"), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Any<DataGridNavigationHydratingAction>());
        dispatcher.Received(1).Dispatch(Arg.Any<DataGridNavigationHydratedCompletedAction>());
    }

    [Fact]
    public async Task HandleAppInitialized_HydratesStoredView() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        ILogger<DataGridNavigationEffects> logger = Substitute.For<ILogger<DataGridNavigationEffects>>();
        var storage = new InMemoryStorageService();
        string key = StorageKeys.BuildKey(Tenant, User, "datagrid", ViewKey);
        GridViewPersistenceBlob blob = GridViewPersistenceBlob.FromSnapshot(Snap());
        await storage.SetAsync(key, blob, ct);

        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns(new List<DomainManifest> {
            new("Counter", "counter", Array.Empty<string>(), Array.Empty<string>()),
        });

        var sut = new DataGridNavigationEffects(storage, MakeAccessor(), logger, FakeState(), registry);
        IDispatcher dispatcher = Substitute.For<IDispatcher>();

        await sut.HandleAppInitialized(new AppInitializedAction("c1"), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Is<GridViewHydratedAction>(a =>
            a.ViewKey == ViewKey && Math.Abs(a.Snapshot.ScrollTop - 123.0) < 0.001));
    }

    [Fact]
    public async Task HandleAppInitialized_OutOfScopeKey_PrunesAndDoesNotDispatch() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        ILogger<DataGridNavigationEffects> logger = Substitute.For<ILogger<DataGridNavigationEffects>>();
        var storage = new InMemoryStorageService();
        string key = StorageKeys.BuildKey(Tenant, User, "datagrid", "deleted-bc:Hexalith.Samples.X");
        await storage.SetAsync(key, GridViewPersistenceBlob.FromSnapshot(Snap()), ct);

        var sut = new DataGridNavigationEffects(storage, MakeAccessor(), logger, FakeState(), EmptyRegistry());
        IDispatcher dispatcher = Substitute.For<IDispatcher>();

        await sut.HandleAppInitialized(new AppInitializedAction("c1"), dispatcher);

        dispatcher.DidNotReceiveWithAnyArgs().Dispatch(Arg.Any<GridViewHydratedAction>());
        GridViewPersistenceBlob? after = await storage.GetAsync<GridViewPersistenceBlob>(key, ct);
        after.ShouldBeNull();
    }

    [Fact]
    public async Task HandleClearGridState_RemovesKey() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        ILogger<DataGridNavigationEffects> logger = Substitute.For<ILogger<DataGridNavigationEffects>>();
        var storage = new InMemoryStorageService();
        string key = StorageKeys.BuildKey(Tenant, User, "datagrid", ViewKey);
        await storage.SetAsync(key, GridViewPersistenceBlob.FromSnapshot(Snap()), ct);

        var sut = new DataGridNavigationEffects(storage, MakeAccessor(), logger, FakeState(), EmptyRegistry());
        await sut.HandleClearGridState(new ClearGridStateAction(ViewKey), Substitute.For<IDispatcher>());

        (await storage.GetAsync<GridViewPersistenceBlob>(key, ct)).ShouldBeNull();
    }

    [Fact]
    public async Task HandleRestoreGridState_BlobExists_DispatchesHydrated() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        ILogger<DataGridNavigationEffects> logger = Substitute.For<ILogger<DataGridNavigationEffects>>();
        var storage = new InMemoryStorageService();
        string key = StorageKeys.BuildKey(Tenant, User, "datagrid", ViewKey);
        await storage.SetAsync(key, GridViewPersistenceBlob.FromSnapshot(Snap()), ct);

        var sut = new DataGridNavigationEffects(storage, MakeAccessor(), logger, FakeState(), EmptyRegistry());
        IDispatcher dispatcher = Substitute.For<IDispatcher>();

        await sut.HandleRestoreGridState(new RestoreGridStateAction(ViewKey), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Is<GridViewHydratedAction>(a => a.ViewKey == ViewKey));
    }

    [Fact]
    public async Task HandleRestoreGridState_NoBlob_DoesNotDispatch() {
        ILogger<DataGridNavigationEffects> logger = Substitute.For<ILogger<DataGridNavigationEffects>>();
        var storage = new InMemoryStorageService();
        var sut = new DataGridNavigationEffects(storage, MakeAccessor(), logger, FakeState(), EmptyRegistry());
        IDispatcher dispatcher = Substitute.For<IDispatcher>();

        await sut.HandleRestoreGridState(new RestoreGridStateAction(ViewKey), dispatcher);

        dispatcher.DidNotReceiveWithAnyArgs().Dispatch(Arg.Any<GridViewHydratedAction>());
    }

    [Fact]
    public async Task HandleCaptureGridState_PersistsReducerSnapshotAfterDebounce() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        ILogger<DataGridNavigationEffects> logger = Substitute.For<ILogger<DataGridNavigationEffects>>();
        var storage = new InMemoryStorageService();
        GridViewSnapshot seeded = Snap(scroll: 456);
        var sut = new DataGridNavigationEffects(
            storage, MakeAccessor(), logger, FakeState(seeded), EmptyRegistry());

        await sut.HandleCaptureGridState(
            new CaptureGridStateAction(ViewKey, seeded),
            Substitute.For<IDispatcher>());

        string key = StorageKeys.BuildKey(Tenant, User, "datagrid", ViewKey);
        GridViewPersistenceBlob? stored = await storage.GetAsync<GridViewPersistenceBlob>(key, ct);
        stored.ShouldNotBeNull();
        stored.ScrollTop.ShouldBe(456);
    }

    [Fact]
    public async Task HandleCaptureGridState_RechecksScopeAfterDebounceBeforeWriting() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        string? tenant = Tenant;
        string? user = User;
        IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
        accessor.TenantId.Returns(_ => tenant);
        accessor.UserId.Returns(_ => user);
        ILogger<DataGridNavigationEffects> logger = Substitute.For<ILogger<DataGridNavigationEffects>>();
        var storage = new InMemoryStorageService();
        FakeTimeProvider time = new();
        GridViewSnapshot seeded = Snap(scroll: 456);
        var sut = new DataGridNavigationEffects(
            storage,
            accessor,
            logger,
            FakeState(seeded),
            EmptyRegistry(),
            time);

        Task pending = sut.HandleCaptureGridState(
            new CaptureGridStateAction(ViewKey, seeded),
            Substitute.For<IDispatcher>());

        tenant = null;
        time.Advance(TimeSpan.FromMilliseconds(250));
        await pending.ConfigureAwait(true);

        string key = StorageKeys.BuildKey(Tenant, User, "datagrid", ViewKey);
        (await storage.GetAsync<GridViewPersistenceBlob>(key, ct)).ShouldBeNull();
    }

    [Fact]
    public async Task HandleAppInitialized_MalformedViewKey_PrunesAndDoesNotDispatch() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        ILogger<DataGridNavigationEffects> logger = Substitute.For<ILogger<DataGridNavigationEffects>>();
        var storage = new InMemoryStorageService();
        string key = StorageKeys.BuildKey(Tenant, User, "datagrid", "broken-key");
        await storage.SetAsync(key, GridViewPersistenceBlob.FromSnapshot(Snap()), ct);

        var sut = new DataGridNavigationEffects(storage, MakeAccessor(), logger, FakeState(), EmptyRegistry());
        IDispatcher dispatcher = Substitute.For<IDispatcher>();

        await sut.HandleAppInitialized(new AppInitializedAction("c1"), dispatcher);

        dispatcher.DidNotReceiveWithAnyArgs().Dispatch(Arg.Any<GridViewHydratedAction>());
        (await storage.GetAsync<GridViewPersistenceBlob>(key, ct)).ShouldBeNull();
    }

    [Fact]
    public async Task Dispose_IsIdempotent() {
        ILogger<DataGridNavigationEffects> logger = Substitute.For<ILogger<DataGridNavigationEffects>>();
        var storage = new InMemoryStorageService();
        var sut = new DataGridNavigationEffects(storage, MakeAccessor(), logger, FakeState(), EmptyRegistry());

        sut.Dispose();
        sut.Dispose(); // Must not throw.

        // Post-dispose handler calls are silent no-ops.
        await sut.HandleClearGridState(new ClearGridStateAction(ViewKey), Substitute.For<IDispatcher>());
    }
}
