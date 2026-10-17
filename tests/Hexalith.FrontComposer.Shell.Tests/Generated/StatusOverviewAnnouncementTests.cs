using System.Collections.Immutable;
using System.Reflection;

using Bunit;

using Counter.Domain;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.DataGrid;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
using Hexalith.FrontComposer.Shell.State.ExpandedRow;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>Rendered coverage for the generated StatusOverview hidden group.</summary>
public sealed class StatusOverviewAnnouncementTests : GeneratedComponentTestBase
{
    public StatusOverviewAnnouncementTests() : base(typeof(CounterProjection).Assembly)
    {
    }

    [Fact]
    public async Task MeasuredStatusRowsRemountTheVirtualizerWithTheirActualHeight()
    {
        Services.AddSingleton(Substitute.For<IEmptyStateCtaResolver>());
        Services.AddFluxor(options => options.ScanTypes(typeof(StatusOverviewAnnouncementProjectionFeature),
            typeof(StatusOverviewAnnouncementProjectionReducers)));
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new StatusOverviewAnnouncementProjectionLoadedAction("initial", Rows(true))
            { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
        IRenderedComponent<StatusOverviewAnnouncementProjectionView> cut = Render<StatusOverviewAnnouncementProjectionView>();
        var firstGrid = cut.Find("table.fc-projection-grid[data-fc-grid-item-size]");
        float defaultHeight = float.Parse(firstGrid.GetAttribute("data-fc-grid-item-size")!, System.Globalization.CultureInfo.InvariantCulture);
        string instanceId = (string)typeof(StatusOverviewAnnouncementProjectionView)
            .GetField("_gridInstanceId", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(cut.Instance)!;

        await cut.InvokeAsync(() => cut.Instance.HandleRowMetricsAsync(instanceId, 88, defaultHeight));
        cut.WaitForAssertion(() => cut.Find("table.fc-projection-grid").GetAttribute("data-fc-grid-item-size").ShouldBe("88"));
        ReferenceEquals(firstGrid, cut.Find("table.fc-projection-grid")).ShouldBeFalse();

        await cut.InvokeAsync(() => cut.Instance.HandleRowMetricsAsync(instanceId, 4097, defaultHeight));
        cut.WaitForAssertion(() => cut.Find("table.fc-projection-grid").GetAttribute("data-fc-grid-item-size").ShouldBe("4097"));
    }

    [Fact]
    public async Task RemovedStatusGroupWithoutExcludingFilterHasNoClearFilterBanner()
    {
        Services.AddSingleton(Substitute.For<IEmptyStateCtaResolver>());
        Services.AddFluxor(options => options.ScanTypes(typeof(StatusOverviewAnnouncementProjectionFeature),
            typeof(StatusOverviewAnnouncementProjectionReducers)));
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new StatusOverviewAnnouncementProjectionLoadedAction("initial", Rows(true))
            { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
        IRenderedComponent<StatusOverviewAnnouncementProjectionView> cut = Render<StatusOverviewAnnouncementProjectionView>();
        ExpandedRowFeature expansion = Services.GetRequiredService<ExpandedRowFeature>();
        await cut.InvokeAsync(() => expansion.RestoreState(new ExpandedRowState {
            ExpandedByViewKey = ImmutableDictionary<string, ExpandedRowEntry>.Empty
                .Add(EphemeralKey(cut), new ExpandedRowEntry("Open", DateTimeOffset.UtcNow)),
        }));
        await cut.InvokeAsync(() => dispatcher.Dispatch(new StatusOverviewAnnouncementProjectionLoadedAction("deleted", Rows(false))
            { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        cut.Find("[data-testid='fc-expanded-row-hidden-banner']").TextContent.ShouldContain("not in the current results");
        cut.FindAll("[data-testid='fc-expanded-row-hidden-banner-clear']").ShouldBeEmpty();
        cut.FindComponent<FcExpandedRowHiddenBanner>().Instance.IsHiddenByFilter.ShouldBeFalse();
    }

    [Fact]
    public async Task PersistedExpansionMountedUnderStatusFilterUsesCurrentSourceEvidence()
    {
        _ = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js")
            .Setup<bool>("focusFirstButtonWithin", _ => true).SetResult(true);
        Services.AddSingleton(Substitute.For<IEmptyStateCtaResolver>());
        Services.AddFluxor(options => options.ScanTypes(typeof(StatusOverviewAnnouncementProjectionFeature),
            typeof(StatusOverviewAnnouncementProjectionReducers)));
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        GridViewSnapshot filtered = new(0, ImmutableDictionary<string, string>.Empty.Add("Status", "Closed"),
            null, false, null, null, DateTimeOffset.UtcNow);
        string viewKey = "Tickets:" + typeof(StatusOverviewAnnouncementProjection).FullName;
        DataGridNavigationFeature navigation = Services.GetRequiredService<DataGridNavigationFeature>();
        DataGridNavigationState current = Services.GetRequiredService<IState<DataGridNavigationState>>().Value;
        navigation.RestoreState(current with { ViewStates = current.ViewStates.SetItem(viewKey, filtered) });
        dispatcher.Dispatch(new StatusOverviewAnnouncementProjectionLoadedAction("filtered-result", Rows(true))
            { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
        IRenderedComponent<StatusOverviewAnnouncementProjectionView> cut = Render<StatusOverviewAnnouncementProjectionView>();
        ExpandedRowFeature expansion = Services.GetRequiredService<ExpandedRowFeature>();
        await cut.InvokeAsync(() => expansion.RestoreState(new ExpandedRowState {
            ExpandedByViewKey = ImmutableDictionary<string, ExpandedRowEntry>.Empty
                .Add(EphemeralKey(cut), new ExpandedRowEntry("Open", DateTimeOffset.UtcNow)),
        }));
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-expanded-row-hidden-banner']")
            .TextContent.ShouldContain("Clear filter"));
        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1);
        cut.FindAll("[role='status']").Count.ShouldBe(1);
    }

    [Fact]
    public async Task ChangedStatusFilterWithDeletedGroupKeepsNeutralReason()
    {
        Services.AddSingleton(Substitute.For<IEmptyStateCtaResolver>());
        Services.AddFluxor(options => options.ScanTypes(typeof(StatusOverviewAnnouncementProjectionFeature),
            typeof(StatusOverviewAnnouncementProjectionReducers)));
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new StatusOverviewAnnouncementProjectionLoadedAction("initial", Rows(true))
            { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
        IRenderedComponent<StatusOverviewAnnouncementProjectionView> cut = Render<StatusOverviewAnnouncementProjectionView>();
        ExpandedRowFeature expansion = Services.GetRequiredService<ExpandedRowFeature>();
        await cut.InvokeAsync(() => expansion.RestoreState(new ExpandedRowState {
            ExpandedByViewKey = ImmutableDictionary<string, ExpandedRowEntry>.Empty
                .Add(EphemeralKey(cut), new ExpandedRowEntry("Open", DateTimeOffset.UtcNow)),
        }));
        DataGridNavigationFeature navigation = Services.GetRequiredService<DataGridNavigationFeature>();
        DataGridNavigationState current = Services.GetRequiredService<IState<DataGridNavigationState>>().Value;
        string viewKey = "Tickets:" + typeof(StatusOverviewAnnouncementProjection).FullName;
        await cut.InvokeAsync(() => navigation.RestoreState(current with {
            ViewStates = current.ViewStates.SetItem(viewKey, new GridViewSnapshot(0,
                ImmutableDictionary<string, string>.Empty.Add("Status", "Closed"),
                null, false, null, null, DateTimeOffset.UtcNow)),
        }));
        await cut.InvokeAsync(() => dispatcher.Dispatch(new StatusOverviewAnnouncementProjectionLoadedAction("deleted", Rows(false))
            { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-expanded-row-hidden-banner']")
            .TextContent.ShouldContain("not in the current results"));
        cut.FindAll("[data-testid='fc-expanded-row-hidden-banner-clear']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("__search", true)]
    [InlineData("__search", false)]
    [InlineData("Id", true)]
    [InlineData("Id", false)]
    public async Task ChangedSearchOrColumnFilterCannotProveMissingGroupWasFiltered(string filterKey, bool changeFilter)
    {
        Services.AddSingleton(Substitute.For<IEmptyStateCtaResolver>());
        Services.AddFluxor(options => options.ScanTypes(typeof(StatusOverviewAnnouncementProjectionFeature),
            typeof(StatusOverviewAnnouncementProjectionReducers)));
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        GridViewSnapshot initialFilter = new(0, changeFilter
            ? ImmutableDictionary<string, string>.Empty
            : ImmutableDictionary<string, string>.Empty.Add(filterKey, "open"),
            null, false, null, null, DateTimeOffset.UtcNow);
        string viewKey = "Tickets:" + typeof(StatusOverviewAnnouncementProjection).FullName;
        DataGridNavigationFeature navigation = Services.GetRequiredService<DataGridNavigationFeature>();
        DataGridNavigationState current = Services.GetRequiredService<IState<DataGridNavigationState>>().Value;
        navigation.RestoreState(current with { ViewStates = current.ViewStates.SetItem(viewKey, initialFilter) });
        dispatcher.Dispatch(new StatusOverviewAnnouncementProjectionLoadedAction("initial", changeFilter ? Rows(true) : RowsOpenOnly())
            { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
        IRenderedComponent<StatusOverviewAnnouncementProjectionView> cut = Render<StatusOverviewAnnouncementProjectionView>();
        ExpandedRowFeature expansion = Services.GetRequiredService<ExpandedRowFeature>();
        await cut.InvokeAsync(() => expansion.RestoreState(new ExpandedRowState {
            ExpandedByViewKey = ImmutableDictionary<string, ExpandedRowEntry>.Empty
                .Add(EphemeralKey(cut), new ExpandedRowEntry("Open", DateTimeOffset.UtcNow)),
        }));
        cut.FindAll("[data-testid='fc-expanded-row-hidden-banner']").ShouldBeEmpty();
        if (changeFilter)
        {
            current = Services.GetRequiredService<IState<DataGridNavigationState>>().Value;
            GridViewSnapshot search = initialFilter with {
                Filters = initialFilter.Filters.SetItem(filterKey, "closed"),
            };
            await cut.InvokeAsync(() => navigation.RestoreState(current with {
                ViewStates = current.ViewStates.SetItem(viewKey, search),
            }));
        }
        await cut.InvokeAsync(() => dispatcher.Dispatch(new StatusOverviewAnnouncementProjectionLoadedAction("next-result", Rows(false))
            { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-expanded-row-hidden-banner']")
            .TextContent.ShouldContain("not in the current results"));
        cut.FindAll("[data-testid='fc-expanded-row-hidden-banner-clear']").ShouldBeEmpty();
    }

    [Fact]
    public async Task CompletedFiltersHideExpandedStatusAndSecondResultSpeaksThroughOneSharedNode()
    {
        _ = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js")
            .Setup<bool>("focusFirstButtonWithin", _ => true).SetResult(true);
        Services.AddSingleton(Substitute.For<IEmptyStateCtaResolver>());
        Services.AddFluxor(options => options.ScanTypes(typeof(StatusOverviewAnnouncementProjectionFeature),
            typeof(StatusOverviewAnnouncementProjectionReducers)));
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new StatusOverviewAnnouncementProjectionLoadedAction("initial", Rows(true)) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
        IRenderedComponent<StatusOverviewAnnouncementProjectionView> first = Render<StatusOverviewAnnouncementProjectionView>();
        IRenderedComponent<StatusOverviewAnnouncementProjectionView> second = Render<StatusOverviewAnnouncementProjectionView>();
        ExpandedRowFeature expansion = Services.GetRequiredService<ExpandedRowFeature>();
        string firstKey = EphemeralKey(first);
        string secondKey = EphemeralKey(second);
        await first.InvokeAsync(() => expansion.RestoreState(new ExpandedRowState {
            ExpandedByViewKey = ImmutableDictionary<string, ExpandedRowEntry>.Empty
                .Add(firstKey, new ExpandedRowEntry("Open", DateTimeOffset.UtcNow))
                .Add(secondKey, new ExpandedRowEntry("Open", DateTimeOffset.UtcNow)),
        }));
        DataGridNavigationFeature navigation = Services.GetRequiredService<DataGridNavigationFeature>();
        DataGridNavigationState current = Services.GetRequiredService<IState<DataGridNavigationState>>().Value;
        string viewKey = "Tickets:" + typeof(StatusOverviewAnnouncementProjection).FullName;
        GridViewSnapshot filtered = new(0, ImmutableDictionary<string, string>.Empty.Add("Status", "Closed"),
            null, false, null, null, DateTimeOffset.UtcNow);
        await first.InvokeAsync(() => navigation.RestoreState(current with {
            ViewStates = current.ViewStates.SetItem(viewKey, filtered),
        }));
        first.FindAll("[data-testid='fc-expanded-row-hidden-banner']").ShouldBeEmpty();

        await first.InvokeAsync(() => dispatcher.Dispatch(new StatusOverviewAnnouncementProjectionLoadedAction("filter-1", Rows(true)) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        first.WaitForAssertion(() => {
            first.Find("[data-testid='fc-expanded-row-hidden-banner']").TextContent.ShouldContain("Clear filter");
            second.Find("[data-testid='fc-expanded-row-hidden-banner']").TextContent.ShouldContain("Clear filter");
            (first.FindAll("[role='status']").Count + second.FindAll("[role='status']").Count).ShouldBe(1);
        });
        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1);
        string firstResult = first.FindComponent<FcExpandedRowHiddenBanner>().Instance.ResultIdentity.ShouldNotBeNull();

        await first.InvokeAsync(() => dispatcher.Dispatch(new StatusOverviewAnnouncementProjectionLoadedAction("filter-2", Rows(true)) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        first.WaitForAssertion(() => {
            first.FindComponent<FcExpandedRowHiddenBanner>().Instance.ResultIdentity.ShouldNotBe(firstResult);
            (first.FindAll("[role='status']").Count + second.FindAll("[role='status']").Count).ShouldBe(1);
            string message = first.FindAll("[data-testid='fc-surface-status']")
                .Concat(second.FindAll("[data-testid='fc-surface-status']"))
                .Single().TextContent;
            message.ShouldContain("The expanded row is hidden by the current filters");
        });
        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1);
    }

    private static string EphemeralKey(IRenderedComponent<StatusOverviewAnnouncementProjectionView> view)
        => (string)typeof(StatusOverviewAnnouncementProjectionView)
            .GetField("_ephemeralViewKey", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(view.Instance)!;

    private static IReadOnlyList<StatusOverviewAnnouncementProjection> Rows(bool includeOpen)
        => includeOpen
            ? [new() { Id = "open", Status = StatusOverviewAnnouncementState.Open },
               new() { Id = "closed", Status = StatusOverviewAnnouncementState.Closed }]
            : [new() { Id = "closed", Status = StatusOverviewAnnouncementState.Closed }];

    private static IReadOnlyList<StatusOverviewAnnouncementProjection> RowsOpenOnly()
        => [new() { Id = "open", Status = StatusOverviewAnnouncementState.Open }];
}
