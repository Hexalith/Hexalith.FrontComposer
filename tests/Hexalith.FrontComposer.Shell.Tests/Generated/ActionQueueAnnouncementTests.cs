using System.Collections.Immutable;

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

/// <summary>Rendered ActionQueue evidence for state-rule and user-filter ownership.</summary>
public sealed class ActionQueueAnnouncementTests : GeneratedComponentTestBase
{
    public ActionQueueAnnouncementTests() : base(typeof(CounterProjection).Assembly)
    {
    }

    [Fact]
    public async Task WhenStateExclusionIsNeutralButBadgeFilterHidingEligibleRowUsesAm22()
    {
        _ = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js")
            .Setup<bool>("focusFirstButtonWithin", _ => true).SetResult(true);
        Services.AddSingleton(Substitute.For<IEmptyStateCtaResolver>());
        Services.AddFluxor(options => options.ScanTypes(typeof(ActionQueueAnnouncementProjectionFeature),
            typeof(ActionQueueAnnouncementProjectionReducers)));
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        string viewKey = "Tickets:" + typeof(ActionQueueAnnouncementProjection).FullName;
        DataGridNavigationFeature navigation = Services.GetRequiredService<DataGridNavigationFeature>();
        DataGridNavigationState current = Services.GetRequiredService<IState<DataGridNavigationState>>().Value;
        GridViewSnapshot badgeFilter = new(0,
            ImmutableDictionary<string, string>.Empty.Add(ReservedFilterKeys.StatusKey, "Neutral"),
            null, false, null, null, DateTimeOffset.UtcNow);
        navigation.RestoreState(current with { ViewStates = current.ViewStates.SetItem(viewKey, badgeFilter) });
        IReadOnlyList<ActionQueueAnnouncementProjection> rows = [
            new() { Id = "open", Status = StatusOverviewAnnouncementState.Open },
            new() { Id = "closed", Status = StatusOverviewAnnouncementState.Closed },
        ];
        dispatcher.Dispatch(new ActionQueueAnnouncementProjectionLoadedAction("initial", rows)
            { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
        IRenderedComponent<ActionQueueAnnouncementProjectionView> cut = Render<ActionQueueAnnouncementProjectionView>();
        ExpandedRowFeature expansion = Services.GetRequiredService<ExpandedRowFeature>();
        string ephemeralKey = (string)typeof(ActionQueueAnnouncementProjectionView)
            .GetField("_ephemeralViewKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(cut.Instance)!;

        await cut.InvokeAsync(() => expansion.RestoreState(new ExpandedRowState {
            ExpandedByViewKey = ImmutableDictionary<string, ExpandedRowEntry>.Empty
                .Add(ephemeralKey, new ExpandedRowEntry("closed", DateTimeOffset.UtcNow)),
        }));
        cut.WaitForAssertion(() => {
            cut.Find("[data-testid='fc-expanded-row-hidden-banner']").TextContent.ShouldContain("not in the current results");
            cut.FindAll("[data-testid='fc-expanded-row-hidden-banner-clear']").ShouldBeEmpty();
            cut.FindComponent<FcExpandedRowHiddenBanner>().Instance.IsHiddenByFilter.ShouldBeFalse();
        });

        await cut.InvokeAsync(() => expansion.RestoreState(new ExpandedRowState {
            ExpandedByViewKey = ImmutableDictionary<string, ExpandedRowEntry>.Empty
                .Add(ephemeralKey, new ExpandedRowEntry("open", DateTimeOffset.UtcNow)),
        }));
        cut.WaitForAssertion(() => {
            cut.Find("[data-testid='fc-expanded-row-hidden-banner']").TextContent.ShouldContain("Clear filter");
            cut.FindComponent<FcExpandedRowHiddenBanner>().Instance.IsHiddenByFilter.ShouldBeTrue();
        });
        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1);

        await cut.InvokeAsync(() => dispatcher.Dispatch(new ActionQueueAnnouncementProjectionLoadedAction("filtered-again", rows)
            { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        cut.WaitForAssertion(() => {
            cut.FindAll("[role='status']").Count.ShouldBe(1);
            cut.Find("[data-testid='fc-surface-status']").TextContent
                .ShouldContain("The expanded row is hidden by the current filters");
        });
        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1);
    }
}
