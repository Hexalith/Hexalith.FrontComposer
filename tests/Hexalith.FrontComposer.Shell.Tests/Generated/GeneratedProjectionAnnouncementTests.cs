using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;

using Bunit;

using Counter.Domain;
using Counter.Web;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.Announcements;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

public sealed class GeneratedProjectionAnnouncementTests : GeneratedComponentTestBase {
    private const string ViewKey = "Counter:Counter.Domain.CounterProjection";
    private const string StatusSelector = "[data-testid='fc-surface-status']";

    public GeneratedProjectionAnnouncementTests() : base(typeof(CounterProjection).Assembly) {
    }

    [Fact]
    public async Task CompletedFilteredPages_AnnounceZeroAndFailureThroughOneLiveNode() {
        await InitializeStoreAsync();
        Services.GetRequiredService<IDispatcher>().Dispatch(new CounterProjectionLoadedAction(
            "seed", [new CounterProjection { Id = "present-row", Count = 1 }]));

        GridViewSnapshot filter = new(
            scrollTop: 0,
            filters: ImmutableDictionary<string, string>.Empty.Add("Id", "missing-row"),
            sortColumn: null,
            sortDescending: false,
            expandedRowId: null,
            selectedRowId: null,
            capturedAt: new DateTimeOffset(2026, 10, 5, 0, 0, 0, TimeSpan.Zero));
        DataGridNavigationFeature navigation = Services.GetRequiredService<DataGridNavigationFeature>();
        DataGridNavigationState navigationState = Services.GetRequiredService<IState<DataGridNavigationState>>().Value;
        navigation.RestoreState(navigationState with {
            ViewStates = navigationState.ViewStates.SetItem(ViewKey, filter),
        });

        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        string requestIdentity = (string)(typeof(CounterProjectionView)
            .GetMethod("PageRequestIdentity", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Generated page request identity is missing."))
            .Invoke(null, [filter])!;
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        var delivered = new List<string>();
        using IDisposable subscription = announcements.Subscribe("projection:" + ViewKey, delivered.Add);
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        LoadedPageState initial = Services.GetRequiredService<IState<LoadedPageState>>().Value;

        await cut.InvokeAsync(() => pages.RestoreState(initial with {
            LastResultByKey = initial.LastResultByKey.SetItem(ViewKey,
                new LoadedPageResult(1, requestIdentity, 0, Failed: false, OperatorInitiated: true)),
        }));
        cut.WaitForAssertion(() => {
            cut.FindAll(StatusSelector).Count.ShouldBe(1);
            cut.Find(StatusSelector).TextContent.ShouldBe("No counters match these filters.");
            delivered.Count.ShouldBe(1);
            delivered[0].ShouldBe("No counters match these filters.");
        });
        long firstRevision = Revision(cut);

        LoadedPageState first = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        await cut.InvokeAsync(() => pages.RestoreState(first with {
            LastResultByKey = first.LastResultByKey.SetItem(ViewKey,
                new LoadedPageResult(2, requestIdentity, 0, Failed: false, OperatorInitiated: true)),
        }));
        cut.WaitForAssertion(() => {
            delivered.Count.ShouldBe(2);
            cut.Find(StatusSelector).TextContent.ShouldBe("No counters match these filters.");
            Revision(cut).ShouldBeGreaterThan(firstRevision);
        });

        LoadedPageState second = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        await cut.InvokeAsync(() => pages.RestoreState(second with {
            LastResultByKey = second.LastResultByKey.SetItem(ViewKey,
                new LoadedPageResult(3, requestIdentity, 0, Failed: true, OperatorInitiated: true) {
                    ErrorMessage = "This section is being updated",
                }),
        }));
        cut.WaitForAssertion(() => {
            delivered.Count.ShouldBe(3);
            delivered[0].ShouldBe("No counters match these filters.");
            delivered[1].ShouldBe("No counters match these filters.");
            delivered[2].ShouldBe("This section is being updated");
            cut.Find(StatusSelector).TextContent.ShouldBe("This section is being updated");
            cut.Find("[data-testid='fc-projection-failure']").TextContent.ShouldContain("This section is being updated");
            cut.FindAll("[role='alert'], [aria-live='assertive']").ShouldBeEmpty();
        });
    }

    [Theory]
    [InlineData(ReservedFilterKeys.SearchKey)]
    [InlineData(ReservedFilterKeys.StatusKey)]
    public async Task SearchAndStatusZeroResultsUseFilteredCopyAndHideCap(string filterKey) {
        await InitializeStoreAsync();
        Services.GetRequiredService<IDispatcher>().Dispatch(new CounterProjectionLoadedAction(
            "seed", [new CounterProjection { Id = "cached", Count = 1 }]));
        GridViewSnapshot snapshot = new(0,
            ImmutableDictionary<string, string>.Empty.Add(filterKey, "active"),
            null, false, null, null, DateTimeOffset.UtcNow);
        DataGridNavigationFeature navigation = Services.GetRequiredService<DataGridNavigationFeature>();
        DataGridNavigationState navigationState = Services.GetRequiredService<IState<DataGridNavigationState>>().Value;
        navigation.RestoreState(navigationState with { ViewStates = navigationState.ViewStates.SetItem(ViewKey, snapshot) });
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        string request = (string)typeof(CounterProjectionView)
            .GetMethod("PageRequestIdentity", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [snapshot])!;
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        LoadedPageState state = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        await cut.InvokeAsync(() => pages.RestoreState(state with {
            LastResultByKey = state.LastResultByKey.SetItem(ViewKey,
                new LoadedPageResult(41, request, 0, Failed: false, OperatorInitiated: true)),
            TotalCountByKey = state.TotalCountByKey.SetItem(ViewKey, 10_000),
        }));

        cut.WaitForAssertion(() => cut.Find(StatusSelector).TextContent.ShouldBe("No counters match these filters."));
        cut.FindAll("[data-testid='fc-max-items-cap-notice']").ShouldBeEmpty();
    }

    [Fact]
    public async Task NotModifiedRecoveryWithdrawsPriorPageFailureSpeech() {
        await InitializeStoreAsync();
        CounterProjection row = new() { Id = "cached", Count = 1 };
        Services.GetRequiredService<IDispatcher>().Dispatch(new CounterProjectionLoadedAction("seed", [row]));
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        GridViewSnapshot? snapshot = (GridViewSnapshot?)typeof(CounterProjectionView)
            .GetMethod("CurrentGridSnapshot", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(cut.Instance, null);
        string request = (string)typeof(CounterProjectionView)
            .GetMethod("PageRequestIdentity", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [snapshot])!;
        LoadedPageResult failed = new(42, request, 0, Failed: true, OperatorInitiated: true) {
            ErrorMessage = "Data could not be loaded.",
        };
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        LoadedPageState state = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        await cut.InvokeAsync(() => pages.RestoreState(state with {
            PagesByKey = state.PagesByKey.SetItem((ViewKey, 0), [row]),
            LastResultByKey = state.LastResultByKey.SetItem(ViewKey, failed),
            ResultsByPage = state.ResultsByPage.SetItem((ViewKey, 0), failed),
            FailureByKey = state.FailureByKey.SetItem(ViewKey, "Data could not be loaded."),
        }));
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        cut.WaitForAssertion(() => announcements.Current("projection:" + ViewKey).ShouldBe("Data could not be loaded."));

        TaskCompletionSource<object> completion = new();
        LoadedPageState failedState = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        LoadedPageState pending = failedState with {
            PendingCompletionsByKey = failedState.PendingCompletionsByKey.SetItem((ViewKey, 0), completion),
        };
        LoadedPageState recovered = LoadedPageReducers.ReduceLoadPageNotModified(
            pending,
            new LoadPageNotModifiedAction(ViewKey, 0, [row], completion) { RequestIdentity = request });
        await cut.InvokeAsync(() => pages.RestoreState(recovered));
        cut.WaitForAssertion(() => announcements.Current("projection:" + ViewKey).ShouldBeEmpty());
        cut.FindAll("[data-testid='fc-projection-failure']").ShouldBeEmpty();
    }

    [Fact]
    public void RequestIdentityKeepsRawValuesAndFieldBoundaries() {
        MethodInfo identity = typeof(CounterProjectionView).GetMethod("PageRequestIdentity", BindingFlags.NonPublic | BindingFlags.Static)!;
        GridViewSnapshot Snapshot(string value) => new(
            scrollTop: 0,
            filters: ImmutableDictionary<string, string>.Empty.Add("Id", value),
            sortColumn: null,
            sortDescending: false,
            expandedRowId: null,
            selectedRowId: null,
            capturedAt: DateTimeOffset.UtcNow);

        string raw = (string)identity.Invoke(null, [Snapshot(" a|")])!;
        string trimmed = (string)identity.Invoke(null, [Snapshot("a|")])!;
        string differentField = (string)identity.Invoke(null, [new GridViewSnapshot(
            0, ImmutableDictionary<string, string>.Empty.Add("Other", " a|"), null, false, null, null, DateTimeOffset.UtcNow)])!;

        raw.ShouldNotBe(trimmed);
        raw.ShouldNotBe(differentField);
    }

    [Fact]
    public async Task OffscreenPageFailureKeepsRowsAndRetriesTheSamePageInPlace() {
        FakeTimeProvider time = new();
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(time));
        await InitializeStoreAsync();
        CounterProjection row = new() { Id = "present-row", Count = 1 };
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new CounterProjectionLoadedAction("seed", [row]));
        GridViewSnapshot filtered = new(
            scrollTop: 0,
            filters: ImmutableDictionary<string, string>.Empty
                .Add("Id", "present")
                .Add(ReservedFilterKeys.SearchKey, "present"),
            sortColumn: "Count",
            sortDescending: true,
            expandedRowId: null,
            selectedRowId: null,
            capturedAt: DateTimeOffset.UtcNow);
        DataGridNavigationFeature navigation = Services.GetRequiredService<DataGridNavigationFeature>();
        DataGridNavigationState navigationState = Services.GetRequiredService<IState<DataGridNavigationState>>().Value;
        navigation.RestoreState(navigationState with {
            ViewStates = navigationState.ViewStates.SetItem(ViewKey, filtered),
        });
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        LoadPageAction? retried = null;
        IActionSubscriber actions = Services.GetRequiredService<IActionSubscriber>();
        object owner = new();
        actions.SubscribeToAction<LoadPageAction>(owner, action => {
            retried = action;
            action.Completion.TrySetResult(Array.Empty<object>());
        });
        try {
            IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
            GridViewSnapshot? snapshot = (GridViewSnapshot?)typeof(CounterProjectionView)
                .GetMethod("CurrentGridSnapshot", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(cut.Instance, null);
            string identity = (string)typeof(CounterProjectionView)
                .GetMethod("PageRequestIdentity", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, [snapshot])!;
            LoadedPageState initial = Services.GetRequiredService<IState<LoadedPageState>>().Value;
            await cut.InvokeAsync(() => pages.RestoreState(initial with {
                PagesByKey = initial.PagesByKey.SetItem((ViewKey, 0), [row]),
                LastResultByKey = initial.LastResultByKey.SetItem(ViewKey,
                    new LoadedPageResult(1, identity, 0, Failed: true, OperatorInitiated: false, Skip: 500, Take: 25)),
            }));
            cut.Find("[data-fc-datagrid]").ShouldNotBeNull();
            LoadedPageState observed = Services.GetRequiredService<IState<LoadedPageState>>().Value;
            observed.LastResultByKey[ViewKey].RequestIdentity.ShouldBe(identity);
            observed.LastResultByKey[ViewKey].Failed.ShouldBeTrue();
            observed.PagesByKey.Keys.Any(key => key.ViewKey == ViewKey).ShouldBeTrue();
            GridViewSnapshot? afterSnapshot = (GridViewSnapshot?)typeof(CounterProjectionView)
                .GetMethod("CurrentGridSnapshot", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(cut.Instance, null);
            string afterIdentity = (string)typeof(CounterProjectionView)
                .GetMethod("PageRequestIdentity", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, [afterSnapshot])!;
            afterIdentity.ShouldBe(identity);
            cut.Find("[data-testid='fc-projection-failure']").ShouldNotBeNull();
            cut.Find("[data-testid='fc-projection-stale-notice']").TextContent.ShouldContain("Data may be out of date");
            time.Advance(TimeSpan.FromMilliseconds(250));
            cut.WaitForAssertion(() => Services.GetRequiredService<ISurfaceAnnouncementCoordinator>()
                .Current("projection:" + ViewKey).ShouldContain("Data may be out of date"));
            cut.WaitForElement("[data-testid='fc-projection-failure'] fluent-button, [data-testid='fc-projection-failure'] button").Click();

            retried.ShouldNotBeNull().Skip.ShouldBe(500);
            retried.Take.ShouldBe(25);
            retried.RequestIdentity.ShouldBe(identity);
            retried.Filters["Id"].ShouldBe("present");
            retried.SortColumn.ShouldBe("Count");
            retried.SortDescending.ShouldBeTrue();
            retried.SearchQuery.ShouldBe("present");
            cut.Find("[data-fc-datagrid]").ShouldNotBeNull();
            Services.GetRequiredService<IState<CounterProjectionState>>().Value.Items!.Single().Id.ShouldBe("present-row");
        }
        finally {
            actions.UnsubscribeFromAllActions(owner);
        }
    }

    [Fact]
    public async Task DisposingViewCancelsInPlaceFailedPageRetry() {
        TaskCompletionSource<ProjectionPageResult> blockedLoad = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(_ => blockedLoad.Task);
        Services.Replace(ServiceDescriptor.Singleton(loader));
        await InitializeStoreAsync();
        Services.GetRequiredService<IDispatcher>().Dispatch(new CounterProjectionLoadedAction(
            "seed", [new CounterProjection { Id = "cached", Count = 1 }]));
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        GridViewSnapshot? snapshot = (GridViewSnapshot?)typeof(CounterProjectionView)
            .GetMethod("CurrentGridSnapshot", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(cut.Instance, null);
        string request = (string)typeof(CounterProjectionView)
            .GetMethod("PageRequestIdentity", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [snapshot])!;
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        LoadedPageState state = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        await cut.InvokeAsync(() => pages.RestoreState(state with {
            LastResultByKey = state.LastResultByKey.SetItem(ViewKey,
                new LoadedPageResult(43, request, 0, Failed: true, OperatorInitiated: true, Skip: 500, Take: 25)),
        }));
        LoadPageAction? retry = null;
        IActionSubscriber actions = Services.GetRequiredService<IActionSubscriber>();
        object owner = new();
        actions.SubscribeToAction<LoadPageAction>(owner, action => retry = action);
        try {
            Task? pendingRetry = null;
            await cut.InvokeAsync(() => {
                pendingRetry = (Task)typeof(CounterProjectionView)
                    .GetMethod("RetryProjectionAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .Invoke(cut.Instance, null)!;
            });
            retry.ShouldNotBeNull();
            retry.CancellationToken.IsCancellationRequested.ShouldBeFalse();

            await cut.Instance.DisposeAsync();
            retry.CancellationToken.IsCancellationRequested.ShouldBeTrue();
            await pendingRetry!.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        }
        finally {
            actions.UnsubscribeFromAllActions(owner);
            blockedLoad.TrySetCanceled(Xunit.TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task GeneratedViewLabelsCachedRowsStaleWhenBrowserGoesOffline() {
        await InitializeStoreAsync();
        CounterProjection row = new() { Id = "cached-row", Count = 1 };
        Services.GetRequiredService<IDispatcher>().Dispatch(new CounterProjectionLoadedAction("seed", [row]));
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        LoadedPageState initial = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        pages.RestoreState(initial with {
            PagesByKey = initial.PagesByKey.SetItem((ViewKey, 0), [row]),
        });
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();

        Services.GetRequiredService<IProjectionConnectionState>().SetBrowserOffline(true);
        cut.WaitForAssertion(() => {
            cut.Find("[data-testid='fc-projection-stale-notice']").TextContent.ShouldContain("Data may be out of date");
            announcements.Current("projection:" + ViewKey).ShouldContain("Data may be out of date");
        });
        cut.Find("[data-fc-datagrid]").ShouldNotBeNull();
    }

    [Fact]
    public async Task OffscreenCompletionKeepsPrimaryCapAndFailedPageRetry() {
        await InitializeStoreAsync();
        CounterProjection row = new() { Id = "present-row", Count = 1 };
        Services.GetRequiredService<IDispatcher>().Dispatch(new CounterProjectionLoadedAction(
            "seed", [row]));
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        GridViewSnapshot? snapshot = (GridViewSnapshot?)typeof(CounterProjectionView)
            .GetMethod("CurrentGridSnapshot", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(cut.Instance, null);
        string request = (string)typeof(CounterProjectionView)
            .GetMethod("PageRequestIdentity", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [snapshot])!;
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable subscription = announcements.Subscribe("projection:" + ViewKey, spoken.Add);
        LoadedPageState initial = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        LoadedPageResult primary = new(1, request, 10_000, Failed: false, OperatorInitiated: false);
        await cut.InvokeAsync(() => pages.RestoreState(initial with {
            PagesByKey = initial.PagesByKey.SetItem((ViewKey, 0), [row]),
            ActiveRequestIdentityByView = initial.ActiveRequestIdentityByView.SetItem(ViewKey, request),
            LastResultByKey = initial.LastResultByKey.SetItem(ViewKey, primary),
            ResultsByPage = initial.ResultsByPage.SetItem((ViewKey, 0), primary),
            LastSuccessfulPrimaryByView = initial.LastSuccessfulPrimaryByView.SetItem(ViewKey, primary),
        }));
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-max-items-cap-notice']").ShouldNotBeNull());
        spoken.Count(static message => message.StartsWith("Showing the first", StringComparison.Ordinal)).ShouldBe(1);

        LoadedPageResult failure = new(2, request, 0, Failed: true, OperatorInitiated: false, Skip: 500, Take: 25) {
            ErrorMessage = "This section is being updated",
        };
        LoadedPageState withPrimary = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        await cut.InvokeAsync(() => pages.RestoreState(withPrimary with {
            LastResultByKey = withPrimary.LastResultByKey.SetItem(ViewKey, failure),
            ResultsByPage = withPrimary.ResultsByPage.SetItem((ViewKey, 500), failure),
        }));
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-projection-failure']").ShouldNotBeNull());

        LoadedPageResult otherSuccess = new(3, request, 10_000, Failed: false, OperatorInitiated: false, Skip: 1_000);
        LoadedPageState withFailure = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        await cut.InvokeAsync(() => pages.RestoreState(withFailure with {
            LastResultByKey = withFailure.LastResultByKey.SetItem(ViewKey, otherSuccess),
            ResultsByPage = withFailure.ResultsByPage.SetItem((ViewKey, 1_000), otherSuccess),
        }));
        cut.Find("[data-testid='fc-projection-failure']").TextContent.ShouldContain("This section is being updated");
        cut.FindAll("[data-testid='fc-max-items-cap-notice']").Count.ShouldBe(1);
        spoken.Count(static message => message.StartsWith("Showing the first", StringComparison.Ordinal)).ShouldBe(1);
    }

    private static long Revision(IRenderedComponent<CounterProjectionView> cut)
        => long.Parse(cut.Find(StatusSelector + " span").GetAttribute("data-message-revision")!, CultureInfo.InvariantCulture);
}
