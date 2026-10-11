using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;

using Bunit;

using Counter.Domain;
using Counter.Web;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.Announcements;
using Hexalith.FrontComposer.Shell.Infrastructure.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

public sealed class GeneratedProjectionAnnouncementTests : GeneratedComponentTestBase {
    [Fact]
    public async Task BrowserFreshnessMarkerRetainsCriteriaInvalidationAndUsesActualMatchingUnchangedRead()
    {
        Services.AddFluxor(options => options.ScanAssemblies(typeof(CounterProjection).Assembly).ScanTypes(typeof(LoadedPageFeature), typeof(LoadedPageReducers)));
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        CounterProjection row = new() { Id = "cached", Count = 1 };
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new CounterProjectionLoadedAction("seed", [row]));
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        IState<LoadedPageState> pageState = Services.GetRequiredService<IState<LoadedPageState>>();
        await cut.InvokeAsync(() => dispatcher.Dispatch(new LoadPageSucceededAction(ViewKey, 0, [row], 1, 0)
            { OriginScopeGeneration = pageState.Value.ScopeGeneration, RequestIdentity = ProjectionRequestIdentity(cut) }));
        string marker = cut.Find("[data-fc-browser-stale]").GetAttribute("data-fc-cached-result").ShouldNotBeNull();
        TaskCompletionSource<object> pending = new();
        LoadedPageState before = pageState.Value;
        LoadPageAction replacement = new(ViewKey, 0, 20, ImmutableDictionary<string, string>.Empty,
            null, false, null, pending, CancellationToken.None)
            { RequestIdentity = "replacement", RequestGeneration = 2 };
        await cut.InvokeAsync(() => pages.RestoreState(LoadedPageReducers.ReduceLoadPage(before, replacement)));
        cut.Find("[data-fc-browser-stale]").GetAttribute("data-fc-cached-result").ShouldBe(marker);
        await cut.InvokeAsync(() => dispatcher.Dispatch(new LoadPageNotModifiedAction(ViewKey, 0, [row], pending)
            { OriginScopeGeneration = pageState.Value.ScopeGeneration, RequestIdentity = "replacement", RequestGeneration = 2 }));
        cut.WaitForAssertion(() => cut.Find("[data-fc-browser-stale]").GetAttribute("data-fc-cached-result").ShouldNotBe(marker));
        string recovered = cut.Find("[data-fc-browser-stale]").GetAttribute("data-fc-cached-result").ShouldNotBeNull();
        await cut.InvokeAsync(() => dispatcher.Dispatch(new LoadPageSucceededAction("other-view", 0, [row], 1, 0)
            { OriginScopeGeneration = pageState.Value.ScopeGeneration }));
        cut.Find("[data-fc-browser-stale]").GetAttribute("data-fc-cached-result").ShouldBe(recovered);
        cut.Markup.ShouldContain("cached");
    }

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
    [InlineData("en", false)]
    [InlineData("fr", false)]
    [InlineData("en", true)]
    [InlineData("fr", true)]
    public async Task CachedFailureSpeechUsesTheCurrentEligibleEventRatherThanEarlierLoadedText(string culture, bool eligibleFailure) {
        FakeTimeProvider time = new();
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(time));
        CultureInfo.CurrentCulture = new(culture);
        CultureInfo.CurrentUICulture = new(culture);
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
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        LoadedPageState initial = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        LoadedPageResult loaded = new(80, request, 1, Failed: false, OperatorInitiated: true);
        await cut.InvokeAsync(() => pages.RestoreState(initial with {
            PagesByKey = initial.PagesByKey.SetItem((ViewKey, 0), [row]),
            LastResultByKey = initial.LastResultByKey.SetItem(ViewKey, loaded),
        }));
        time.Advance(TimeSpan.FromMilliseconds(250));
        cut.WaitForAssertion(() => cut.Find(StatusSelector).TextContent.ShouldNotBeEmpty());
        string earlierLoaded = cut.Find(StatusSelector).TextContent;
        var localizer = Services.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Hexalith.FrontComposer.Shell.Resources.FcShellResources>>();
        string failureCopy = localizer["SectionUpdatingText"].Value;
        LoadedPageResult failed = new(81, request, 0, Failed: true, OperatorInitiated: true, Skip: 500) {
            ErrorMessage = failureCopy,
        };
        LoadedPageState current = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        await cut.InvokeAsync(() => pages.RestoreState(current with {
            ResultsByPage = current.ResultsByPage.SetItem((ViewKey, 500), failed),
            LastResultByKey = current.LastResultByKey.SetItem(ViewKey,
                eligibleFailure ? failed : failed with { RequestIdentity = "superseded-filter" }),
        }));

        cut.Find("[data-fc-datagrid]").ShouldNotBeNull();
        cut.Find("[data-testid='fc-projection-failure']").TextContent.ShouldContain(failureCopy);
        cut.Find("[data-testid='fc-projection-stale-notice']").TextContent.ShouldBe(localizer["Am04Stale"].Value);
        time.Advance(TimeSpan.FromMilliseconds(249));
        cut.Find(StatusSelector).TextContent.ShouldBe(eligibleFailure ? failureCopy : earlierLoaded);
        time.Advance(TimeSpan.FromMilliseconds(1));
        cut.WaitForAssertion(() => cut.Find(StatusSelector).TextContent.ShouldBe(
            eligibleFailure ? failureCopy : localizer["Am04Stale"].Value));
        cut.Render();
        time.Advance(TimeSpan.FromMilliseconds(250));
        cut.Find(StatusSelector).TextContent.ShouldBe(eligibleFailure ? failureCopy : localizer["Am04Stale"].Value);
        cut.FindAll("[role='status'][aria-live='polite']").Count.ShouldBe(1);
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
                .Current("projection:" + ViewKey).ShouldBe("Data could not be loaded."));
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
    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public async Task PrimaryFailureKeepsClassifiedVisibleCopyCachedRowsAndCanonicalSpeech(string culture)
    {
        CultureInfo.CurrentUICulture = new(culture);
        CultureInfo.CurrentCulture = new(culture);
        await InitializeStoreAsync();
        CounterProjection row = new() { Id = "cached-primary-row", Count = 1 };
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new CounterProjectionLoadedAction("seed", [row]));
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        LoadedPageState initial = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        pages.RestoreState(initial with { PagesByKey = initial.PagesByKey.SetItem((ViewKey, 0), [row]) });
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        var localizer = Services.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Hexalith.FrontComposer.Shell.Resources.FcShellResources>>();
        await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadFailedAction("primary-failed", localizer["SectionUpdatingText"].Value)));
        cut.WaitForAssertion(() => {
            cut.FindAll("[data-testid='fc-projection-failure']").Count.ShouldBe(1);
            cut.Find("[data-testid='fc-projection-failure']").TextContent.ShouldContain(localizer["SectionUpdatingText"].Value);
            cut.Find("[data-fc-datagrid]").ShouldNotBeNull();
            cut.Find("[data-testid='fc-projection-stale-notice']").TextContent.ShouldBe(localizer["Am04Stale"].Value);
            cut.Find(StatusSelector).TextContent.ShouldBe(localizer["Am30QueryFailed"].Value);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RetryDispatcherThrowAlwaysDisposesItsTokenAndFatalFailurePropagates(bool fatal)
    {
        await InitializeStoreAsync();
        Services.GetRequiredService<IDispatcher>().Dispatch(new CounterProjectionLoadedAction("seed", [new CounterProjection { Id = "cached", Count = 1 }]));
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        GridViewSnapshot? snapshot = (GridViewSnapshot?)typeof(CounterProjectionView).GetMethod("CurrentGridSnapshot", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(cut.Instance, null);
        string request = (string)typeof(CounterProjectionView).GetMethod("PageRequestIdentity", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [snapshot])!;
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        LoadedPageState current = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        await cut.InvokeAsync(() => pages.RestoreState(current with {
            LastResultByKey = current.LastResultByKey.SetItem(ViewKey, new LoadedPageResult(99, request, 0, Failed: true, OperatorInitiated: true, Skip: 500, Take: 25)),
        }));
        IDispatcher failing = Substitute.For<IDispatcher>();
        LoadPageAction? dispatched = null;
        failing.When(instance => instance.Dispatch(Arg.Any<object>())).Do(call => {
            dispatched = call.ArgAt<object>(0).ShouldBeOfType<LoadPageAction>();
            if (fatal) { throw (OutOfMemoryException)Activator.CreateInstance(typeof(OutOfMemoryException), "fatal dispatcher")!; }
            throw new InvalidOperationException("dispatcher unavailable");
        });
        typeof(CounterProjectionView).GetProperty("Dispatcher", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(cut.Instance, failing);
        Task? retry = null;
        await cut.InvokeAsync(() => { retry = (Task)typeof(CounterProjectionView).GetMethod("RetryProjectionAsync", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(cut.Instance, null)!; });
        if (fatal) { await Should.ThrowAsync<OutOfMemoryException>(() => retry!); }
        else { await retry!.ConfigureAwait(true); }
        typeof(CounterProjectionView).GetField("_failedRetryCancellation", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(cut.Instance).ShouldBeNull();
        dispatched.ShouldNotBeNull();
        Should.Throw<ObjectDisposedException>(() => _ = dispatched.CancellationToken.WaitHandle);
    }
    [Fact]
    public async Task PrimaryRetrySuccessThroughGeneratedReducersClearsFailureAndSpeaksLoadedOnce()
    {
        FakeTimeProvider time = new();
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(time));
        await InitializeStoreAsync();
        CounterProjection row = new() { Id = "cached-retry-row", Count = 1 };
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new CounterProjectionLoadedAction("seed", [row]));
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadFailedAction("failed-primary", "This section is being updated")));
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-projection-failure']").ShouldNotBeNull());
        List<string> spoken = [];
        using IDisposable speech = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>().Subscribe("projection:" + ViewKey, spoken.Add);
        IActionSubscriber actions = Services.GetRequiredService<IActionSubscriber>();
        object owner = new();
        CounterProjectionLoadRequestedAction? retry = null;
        actions.SubscribeToAction<CounterProjectionLoadRequestedAction>(owner, action => retry = action);
        try
        {
            cut.Find("[data-testid='fc-projection-failure'] fluent-button, [data-testid='fc-projection-failure'] button").Click();
            retry.ShouldNotBeNull().OperatorInitiated.ShouldBeTrue();
            Services.GetRequiredService<IState<CounterProjectionState>>().Value.Items!.Single().Id.ShouldBe(row.Id);
            await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadedAction(retry.CorrelationId, [row])));
            cut.WaitForAssertion(() => cut.FindAll("[data-testid='fc-projection-failure']").ShouldBeEmpty());
            time.Advance(TimeSpan.FromMilliseconds(249));
            spoken.Count(message => message == "counters loaded.").ShouldBe(0);
            time.Advance(TimeSpan.FromMilliseconds(1));
            cut.WaitForAssertion(() => cut.Find(StatusSelector).TextContent.ShouldBe("counters loaded."));
            cut.Render();
            time.Advance(TimeSpan.FromMilliseconds(250));
            spoken.Count(message => message == "counters loaded.").ShouldBe(1);
            cut.FindAll("[data-testid='fc-projection-stale-notice']").ShouldBeEmpty();
        }
        finally
        {
            actions.UnsubscribeFromAllActions(owner);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisposedGeneratedGridWithdrawsOnlyItsLoadingOrFailure(bool failure)
    {
        FakeTimeProvider time = new();
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(time));
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadRequestedAction("abandoned", true)));
        if (failure)
        {
            await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadFailedAction("abandoned", "This section is being updated")));
            cut.WaitForAssertion(() => cut.Find(StatusSelector).TextContent.ShouldBe("Data could not be loaded."));
        }
        await cut.Instance.DisposeAsync();
        time.Advance(TimeSpan.FromMilliseconds(250));
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        announcements.Current("projection:" + ViewKey).ShouldBeEmpty();
        typeof(CounterProjectionView).GetMethod("AnnounceProjectionState", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(cut.Instance, null);
        time.Advance(TimeSpan.FromMilliseconds(250));
        announcements.Current("projection:" + ViewKey).ShouldBeEmpty();
        IRenderedComponent<Hexalith.FrontComposer.Shell.Components.Rendering.FcSurfaceStatus> remountedStatus =
            Render<Hexalith.FrontComposer.Shell.Components.Rendering.FcSurfaceStatus>(p => p.Add(c => c.Surface, "projection:" + ViewKey));
        remountedStatus.Find(StatusSelector).TextContent.ShouldBeEmpty();
        announcements.Announce("projection:" + ViewKey, "other-owner", "result", "Other owner remains", immediate: true);
        await cut.Instance.DisposeAsync();
        announcements.Current("projection:" + ViewKey).ShouldBe("Other owner remains");
    }

    [Fact]
    public async Task GridRemountRetainsGenerationCachesAndFailedRetryThenRejectsOldCriteriaIncludingAba()
    {
        await InitializeStoreAsync();
        CounterProjection row = new() { Id = "retained-row", Count = 1 };
        Services.GetRequiredService<IDispatcher>().Dispatch(new CounterProjectionLoadedAction("seed", [row]));
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        DataGridNavigationFeature navigation = Services.GetRequiredService<DataGridNavigationFeature>();
        IRenderedComponent<CounterProjectionView> first = Render<CounterProjectionView>();
        string identity = ProjectionRequestIdentity(first);
        LoadedPageState initial = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        pages.RestoreState(initial with {
            ActiveRequestIdentityByView = initial.ActiveRequestIdentityByView.SetItem(ViewKey, identity),
            ActiveRequestGenerationByView = initial.ActiveRequestGenerationByView.SetItem(ViewKey, 7),
            PagesByKey = initial.PagesByKey.SetItem((ViewKey, 0), [row]),
            ResultsByPage = initial.ResultsByPage.SetItem((ViewKey, 500), new LoadedPageResult(700, identity, 0, Failed: true, OperatorInitiated: true, Skip: 500, Take: 25)),
        });
        await first.Instance.DisposeAsync();
        IRenderedComponent<CounterProjectionView> remounted = Render<CounterProjectionView>();
        List<LoadPageAction> requests = [];
        IActionSubscriber actions = Services.GetRequiredService<IActionSubscriber>();
        object owner = new();
        actions.SubscribeToAction<LoadPageAction>(owner, action => {
            requests.Add(action);
            LoadedPageState state = Services.GetRequiredService<IState<LoadedPageState>>().Value;
            pages.RestoreState(LoadedPageReducers.ReduceLoadPage(state, action));
            action.Completion.TrySetResult(new object[] { row });
        });
        try
        {
            await RequestGeneratedPage(remounted);
            requests.Last().RequestGeneration.ShouldBe(7);
            LoadedPageState preserved = Services.GetRequiredService<IState<LoadedPageState>>().Value;
            preserved.PagesByKey[(ViewKey, 0)].Single().ShouldBe(row);
            preserved.ResultsByPage[(ViewKey, 500)].Failed.ShouldBeTrue();
            remounted.Find("[data-testid='fc-projection-failure']").ShouldNotBeNull();
            DataGridNavigationState originalNavigation = Services.GetRequiredService<IState<DataGridNavigationState>>().Value;
            GridViewSnapshot changed = new(0, ImmutableDictionary<string, string>.Empty.Add("Id", "different"), null, false, null, null, DateTimeOffset.UtcNow);
            await remounted.InvokeAsync(() => navigation.RestoreState(originalNavigation with { ViewStates = originalNavigation.ViewStates.SetItem(ViewKey, changed) }));
            await RequestGeneratedPage(remounted);
            requests.Last().RequestGeneration.ShouldBe(8);
            Services.GetRequiredService<IState<LoadedPageState>>().Value.ResultsByPage.ShouldBeEmpty();
            await remounted.InvokeAsync(() => navigation.RestoreState(originalNavigation));
            await RequestGeneratedPage(remounted);
            requests.Last().RequestGeneration.ShouldBe(9);
            requests.Last().RequestIdentity.ShouldBe(identity);
            LoadedPageState current = Services.GetRequiredService<IState<LoadedPageState>>().Value;
            var monitor = Substitute.For<Microsoft.Extensions.Options.IOptionsMonitor<Hexalith.FrontComposer.Shell.Options.FcShellOptions>>();
            monitor.CurrentValue.Returns(new Hexalith.FrontComposer.Shell.Options.FcShellOptions());
            LoadedPageReducers reducers = new(monitor, Microsoft.Extensions.Logging.Abstractions.NullLogger<LoadedPageReducers>.Instance);
            reducers.ReduceLoadPageSucceeded(current, new LoadPageSucceededAction(ViewKey, 0, ["old"], 1, 0) {
                RequestIdentity = identity, RequestGeneration = 7,
            }).ShouldBeSameAs(current);
        }
        finally
        {
            actions.UnsubscribeFromAllActions(owner);
        }
    }

    [Fact]
    public async Task BackgroundFailedLaneKeepsCachedGridStaleAcrossOtherPageSuccess()
    {
        await InitializeStoreAsync();
        CounterProjection row = new() { Id = "cached-background", Count = 1 };
        Services.GetRequiredService<IDispatcher>().Dispatch(new CounterProjectionLoadedAction("seed", [row]));
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        LoadedPageState initial = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        await cut.InvokeAsync(() => pages.RestoreState(initial with { PagesByKey = initial.PagesByKey.SetItem((ViewKey, 0), [row]),
            StalePages = initial.StalePages.Add((ViewKey, 0)), }));
        cut.Find("[data-testid='fc-projection-stale-notice']").ShouldNotBeNull();
        var monitor = Substitute.For<Microsoft.Extensions.Options.IOptionsMonitor<Hexalith.FrontComposer.Shell.Options.FcShellOptions>>();
        monitor.CurrentValue.Returns(new Hexalith.FrontComposer.Shell.Options.FcShellOptions());
        LoadedPageReducers reducers = new(monitor, Microsoft.Extensions.Logging.Abstractions.NullLogger<LoadedPageReducers>.Instance);
        LoadedPageState stale = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        await cut.InvokeAsync(() => pages.RestoreState(reducers.ReduceLoadPageSucceeded(stale, new LoadPageSucceededAction(ViewKey, 500, [row], 501, 0))));
        cut.Find("[data-testid='fc-projection-stale-notice']").ShouldNotBeNull();
        LoadedPageState unrelated = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        await cut.InvokeAsync(() => pages.RestoreState(reducers.ReduceLoadPageSucceeded(unrelated, new LoadPageSucceededAction(ViewKey, 0, [row], 501, 0))));
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='fc-projection-stale-notice']").ShouldBeEmpty());
    }

    private static string ProjectionRequestIdentity(IRenderedComponent<CounterProjectionView> cut)
    {
        object? snapshot = typeof(CounterProjectionView).GetMethod("CurrentGridSnapshot", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(cut.Instance, null);
        return (string)typeof(CounterProjectionView).GetMethod("PageRequestIdentity", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [snapshot])!;
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task GridDisposalWithdrawsPrimaryAndPageLoadedOrEmptyResults(bool pageResult, bool empty)
    {
        FakeTimeProvider time = new();
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(time));
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        if (pageResult && empty)
        {
            DataGridNavigationFeature navigation = Services.GetRequiredService<DataGridNavigationFeature>();
            DataGridNavigationState current = Services.GetRequiredService<IState<DataGridNavigationState>>().Value;
            navigation.RestoreState(current with { ViewStates = current.ViewStates.SetItem(ViewKey,
                new GridViewSnapshot(0, ImmutableDictionary<string, string>.Empty.Add("Id", "missing"), null, false, null, null, time.GetUtcNow())) });
        }
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        CounterProjection row = new() { Id = "owned-row", Count = 1 };
        await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadRequestedAction("owned-primary", true)));
        await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadedAction("owned-primary", empty && !pageResult ? [] : [row])));
        if (pageResult)
        {
            LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
            LoadedPageState current = Services.GetRequiredService<IState<LoadedPageState>>().Value;
            string request = ProjectionRequestIdentity(cut);
            LoadedPageResult result = new(900, request, empty ? 0 : 1, Failed: false, OperatorInitiated: true);
            await cut.InvokeAsync(() => pages.RestoreState(current with {
                ActiveRequestIdentityByView = current.ActiveRequestIdentityByView.SetItem(ViewKey, request),
                LastResultByKey = current.LastResultByKey.SetItem(ViewKey, result),
                ResultsByPage = current.ResultsByPage.SetItem((ViewKey, 0), result),
            }));
        }
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        if (empty) { announcements.Current("projection:" + ViewKey).ShouldNotBeEmpty(); }
        await cut.Instance.DisposeAsync();
        time.Advance(TimeSpan.FromMilliseconds(250));
        announcements.Current("projection:" + ViewKey).ShouldBeEmpty();
        IRenderedComponent<Hexalith.FrontComposer.Shell.Components.Rendering.FcSurfaceStatus> replacement =
            Render<Hexalith.FrontComposer.Shell.Components.Rendering.FcSurfaceStatus>(p => p.Add(c => c.Surface, "projection:" + ViewKey));
        replacement.Find(StatusSelector).TextContent.ShouldBeEmpty();
        announcements.Announce("projection:" + ViewKey, "another-owner", "current", "Other owner remains", immediate: true);
        await cut.Instance.DisposeAsync();
        announcements.Current("projection:" + ViewKey).ShouldBe("Other owner remains");
    }

    [Fact]
    public async Task NewPrimaryLoadWithdrawsItsPreviouslyQueuedSuccessWithoutCancellingAnotherOwner()
    {
        FakeTimeProvider time = new();
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(time));
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new CounterProjectionLoadRequestedAction("first", true));
        dispatcher.Dispatch(new CounterProjectionLoadedAction("first", [new CounterProjection { Id = "cached", Count = 1 }]));
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable speech = announcements.Subscribe("projection:" + ViewKey, spoken.Add);
        await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadRequestedAction("replacement", true)));
        time.Advance(TimeSpan.FromMilliseconds(250));
        spoken.ShouldNotContain("counters loaded.");
        cut.Find(StatusSelector).TextContent.ShouldBe("Loading counters.");
    }

    [Fact]
    public async Task FailedEligibleBackgroundReadThroughRegisteredStoreLabelsMountedCachedRowsStale()
    {
        Services.AddFluxor(options => options.ScanAssemblies(typeof(CounterProjection).Assembly).ScanTypes(typeof(LoadedPageFeature), typeof(LoadedPageReducers)));
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        CounterProjection row = new() { Id = "real-store-cached", Count = 1 };
        dispatcher.Dispatch(new CounterProjectionLoadedAction("seed", [row]));
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        string request = ProjectionRequestIdentity(cut);
        dispatcher.Dispatch(new LoadPageSucceededAction(ViewKey, 0, [row], 1, 0) { RequestIdentity = request });
        IState<LoadedPageState> pages = Services.GetRequiredService<IState<LoadedPageState>>();
        pages.Value.PagesByKey[(ViewKey, 0)].Single().ShouldBe(row);
        pages.Value.StalePages.ShouldBeEmpty();
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ProjectionPageResult>(new IOException("sensitive backend detail")));
        ProjectionFallbackRefreshScheduler scheduler = new(Services.GetRequiredService<IProjectionConnectionState>(), loader,
            dispatcher, pages, Services.GetRequiredService<IOptionsMonitor<Hexalith.FrontComposer.Shell.Options.FcShellOptions>>(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        using IDisposable lane = scheduler.RegisterLane(new(ViewKey, "Counter.Domain.CounterProjection", "test-tenant", 0, 25,
            ImmutableDictionary<string, string>.Empty, null, false, null));
        ProjectionReconciliationRefreshResult result = await scheduler.TriggerReconciliationOnceAsync(1, Xunit.TestContext.Current.CancellationToken);
        result.RefreshedCount.ShouldBe(0);
        result.FailedViewKeys.ShouldBe([ViewKey]);
        cut.WaitForAssertion(() => {
            pages.Value.StalePages.ShouldContain((ViewKey, 0));
            pages.Value.PagesByKey[(ViewKey, 0)].Single().ShouldBe(row);
            cut.Find("[data-testid='fc-projection-stale-notice']").TextContent.ShouldContain("Data may be out of date");
            cut.Find("[data-fc-datagrid]").ShouldNotBeNull();
            cut.FindAll("[data-testid='fc-projection-failure']").ShouldBeEmpty();
            cut.Markup.ShouldNotContain("sensitive backend detail");
        });
        pages.Value.ResultsByPage[(ViewKey, 0)].Failed.ShouldBeFalse();
    }

    private static async Task RequestGeneratedPage(IRenderedComponent<CounterProjectionView> cut)
    {
        ValueTask<Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderResult<CounterProjection>> pending = default;
        await cut.InvokeAsync(() => pending = (ValueTask<Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderResult<CounterProjection>>)typeof(CounterProjectionView).GetMethod("LoadPageAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(cut.Instance, [default(Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderRequest<CounterProjection>)])!).ConfigureAwait(true);
        _ = await pending.ConfigureAwait(true);
    }


}
