using System.Globalization;
using System.Reflection;
using System.Collections.Immutable;

using Bunit;

using Counter.Domain;
using Counter.Web;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.Announcements;
using Hexalith.FrontComposer.Shell.Components.Rendering;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Microsoft.AspNetCore.Components;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;
using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

public sealed class GeneratedFreshRowAnnouncementTests : GeneratedComponentTestBase
{
    private const string ViewKey = "Counter:Counter.Domain.CounterProjection";
    private const string Cue = "[data-testid=fc-new-item-indicator]";
    private readonly FakeTimeProvider _time = new();
    private readonly Epic9UserContextAccessor _scope = new();

    public GeneratedFreshRowAnnouncementTests() : base(typeof(CounterProjection).Assembly)
    {
        CultureInfo.CurrentUICulture = new("en");
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(_time));
        Services.Replace(ServiceDescriptor.Singleton<IUserContextAccessor>(_scope));
        Services.AddFluxor(options => options.ScanAssemblies(typeof(CounterProjection).Assembly)
            .ScanTypes(typeof(DataGridNavigationFeature), typeof(DataGridNavigationReducers), typeof(FilterEffects),
                typeof(LoadedPageFeature), typeof(LoadedPageReducers), typeof(LoadPageEffects)));
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public async Task DistinctOverlappingAndSameRenderBurstEntriesEachMutateTheSharedNodeOnce(bool beforeRender, bool olderFirst)
    {
        await InitializeStoreAsync();
        Seed("existing-row");
        var indicators = Services.GetRequiredService<INewItemIndicatorStateService>();
        IRenderedComponent<CascadingValue<PagePanelVisibility>>? panel = null;
        IRenderedComponent<CounterProjectionView> grid;
        if (beforeRender)
        {
            // Keep the production status component mounted but unexposed while both cues
            // arrive, then deliver both through its first exposed parent render.
            panel = Render<CascadingValue<PagePanelVisibility>>(p => p.Add(c => c.Value, new(false))
                .AddChildContent<CounterProjectionView>());
            grid = panel.FindComponent<CounterProjectionView>();
        }
        else grid = Render<CounterProjectionView>();
        var status = grid.FindComponent<FcSurfaceStatus>();
        List<string> mutations = [];
        string? lastRevision = null;
        status.OnMarkupUpdated += (_, _) => {
            var node = status.FindAll("[data-testid=fc-surface-status]").SingleOrDefault();
            string? revision = node?.QuerySelector("[data-message-revision]")?.GetAttribute("data-message-revision");
            bool changed = revision is not null && revision != lastRevision;
            lastRevision = revision;
            if (changed && node?.GetAttribute("aria-live") == "polite"
                && node.TextContent.StartsWith("Updated row:", StringComparison.Ordinal)) mutations.Add(node.TextContent);
        };
        await grid.InvokeAsync(() => {
            indicators.Add(Entry("row-A", "message-A"));
            indicators.Add(Entry("row-B", "message-B"));
        });
        if (panel is not null)
        {
            mutations.ShouldBeEmpty();
            await panel.InvokeAsync(() => panel.Render(p => p.Add(c => c.Value, new(true)).AddChildContent<CounterProjectionView>()));
        }
        status.WaitForAssertion(() => mutations.ShouldBe(["Updated row: row-A.", "Updated row: row-B."]));
        grid.FindAll(Cue).Count.ShouldBe(2);
        grid.FindAll("[data-testid=fc-surface-status][aria-live=polite]").Count.ShouldBe(1);
        string firstRemoval = olderFirst ? "row-A" : "row-B";
        await grid.InvokeAsync(() => indicators.DismissMaterialized(ViewKey, firstRemoval));
        grid.WaitForAssertion(() => grid.FindAll(Cue).Count.ShouldBe(1));
        if (olderFirst)
            status.Find("[data-testid=fc-surface-status][aria-live=polite]").TextContent.ShouldBe("Updated row: row-B.");
        else
            status.WaitForAssertion(() => {
                status.Find("[data-testid=fc-surface-status]").TextContent.ShouldBe("Updated row: row-A.");
                status.Find("[data-testid=fc-surface-status]").GetAttribute("aria-live").ShouldBe("off");
            });
        mutations.ShouldBe(["Updated row: row-A.", "Updated row: row-B."]);
        await grid.InvokeAsync(() => indicators.DismissMaterialized(ViewKey, olderFirst ? "row-B" : "row-A"));
        grid.WaitForAssertion(() => grid.FindAll(Cue).ShouldBeEmpty());
        mutations.ShouldBe(["Updated row: row-A.", "Updated row: row-B."]);
        grid.FindAll("[data-testid=fc-surface-status]").Count.ShouldBe(1);
    }

    [Fact]
    public async Task AlreadyRenderedGridAppearsWithoutUnrelatedRenderAndRetainsFirstTransitionUntilExactExpiry()
    {
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProjectionPageResult([new CounterProjection { Id = "paged-row", Count = 1 }], 600, "etag")));
        Services.Replace(ServiceDescriptor.Singleton(loader));
        await InitializeStoreAsync();
        Seed("existing-row");
        var grid = Render<CounterProjectionView>();
        var indicators = Services.GetRequiredService<INewItemIndicatorStateService>();
        var speech = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using var subscription = ObserveFreshSpeech(speech, spoken);
        NewItemIndicatorEntry first = Entry("fresh-row", "message-one");
        indicators.Add(first);
        grid.WaitForAssertion(() => {
            grid.Find(Cue).TextContent.Trim().ShouldBe("Updated row: fresh-row.");
            grid.FindAll("[aria-live=polite]").Count.ShouldBe(1);
            grid.Find("[data-testid=fc-surface-status]").TextContent.ShouldBe("Updated row: fresh-row.");
            spoken.Count.ShouldBe(1);
        });
        _time.Advance(TimeSpan.FromSeconds(5));
        indicators.Add(Entry("fresh-row", "later-message"));
        indicators.Snapshot(ViewKey).ShouldHaveSingleItem().ShouldBe(first);
        await grid.InvokeAsync(() => grid.Instance.HandleScrollAsync(ViewKey, 44d * 500d));
        ValueTask<Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderResult<CounterProjection>> page = default;
        await grid.InvokeAsync(() => page = (ValueTask<Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderResult<CounterProjection>>)
            typeof(CounterProjectionView).GetMethod("LoadPageAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(grid.Instance, [default(Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderRequest<CounterProjection>)])!);
        (await page.ConfigureAwait(true)).Items.Single().Id.ShouldBe("paged-row");
        Services.GetRequiredService<IState<LoadedPageState>>().Value.PagesByKey[(ViewKey, 0)]
            .OfType<CounterProjection>().Single().Id.ShouldBe("paged-row");
        indicators.Snapshot(ViewKey).ShouldHaveSingleItem().ShouldBe(first);
        spoken.Count.ShouldBe(1);
        await DisposeComponentsAsync();
        var remount = Render<CounterProjectionView>();
        remount.Find(Cue).ShouldNotBeNull();
        spoken.Count.ShouldBe(1);
        _time.Advance(TimeSpan.FromMilliseconds(4999));
        remount.FindAll(Cue).Count.ShouldBe(1);
        _time.Advance(TimeSpan.FromMilliseconds(1));
        remount.WaitForAssertion(() => remount.FindAll(Cue).ShouldBeEmpty());
        spoken.Count.ShouldBe(1);
    }

    [Fact]
    public async Task MaterializationFilterRequeryAndDismissalRemoveSilently()
    {
        await InitializeStoreAsync();
        Seed("existing-row");
        var grid = Render<CounterProjectionView>();
        var indicators = Services.GetRequiredService<INewItemIndicatorStateService>();
        List<string> spoken = [];
        List<string> audible = [];
        using var subscription = ObserveFreshSpeech(Services.GetRequiredService<ISurfaceAnnouncementCoordinator>(), spoken, audible);
        foreach (string removal in new[] { "materialize", "filter-requery", "dismiss" })
        {
            indicators.Add(Entry(removal, removal));
            grid.WaitForAssertion(() => {
                grid.FindAll(Cue).Count.ShouldBe(1);
                spoken.ShouldContain($"Updated row: {removal}.");
            });
            int before = audible.Count;
            if (removal == "materialize") await grid.InvokeAsync(() => Seed(removal));
            else if (removal == "filter-requery")
                await grid.InvokeAsync(() => Services.GetRequiredService<IDispatcher>()
                    .Dispatch(new ColumnFilterChangedAction(ViewKey, "Id", "filtered-row")));
            else indicators.DismissMaterialized(ViewKey, removal);
            grid.WaitForAssertion(() => grid.FindAll(Cue).ShouldBeEmpty());
            audible.Count.ShouldBe(before);
        }
        spoken.ShouldBe(["Updated row: materialize.", "Updated row: filter-requery.", "Updated row: dismiss."]);
    }

    [Fact]
    public async Task GeneratedFilterActionRequeriesRealPageAndSilentlyRemovesFirstTransition()
    {
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        IImmutableDictionary<string, string>? queriedFilters = null;
        loader.LoadPageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(call => {
                queriedFilters = call.ArgAt<IImmutableDictionary<string, string>>(3);
                return Task.FromResult(new ProjectionPageResult([new CounterProjection { Id = "filtered-row", Count = 2 }], 1, "etag"));
            });
        Services.Replace(ServiceDescriptor.Singleton(loader));
        await InitializeStoreAsync();
        Seed("existing-row");
        var grid = Render<CounterProjectionView>();
        var indicators = Services.GetRequiredService<INewItemIndicatorStateService>();
        List<string> spoken = [];
        List<string> audible = [];
        using var subscription = ObserveFreshSpeech(Services.GetRequiredService<ISurfaceAnnouncementCoordinator>(), spoken, audible);
        NewItemIndicatorEntry first = Entry("fresh-row", "message-one");
        indicators.Add(first);
        grid.WaitForAssertion(() => spoken.Count.ShouldBe(1));
        _time.Advance(TimeSpan.FromSeconds(5));
        indicators.Add(Entry("fresh-row", "later-message"));
        indicators.Snapshot(ViewKey).ShouldHaveSingleItem().ShouldBe(first);
        int before = audible.Count;
        await grid.InvokeAsync(() => Services.GetRequiredService<IDispatcher>()
            .Dispatch(new ColumnFilterChangedAction(ViewKey, "Id", "filtered-row")));
        grid.WaitForAssertion(() => grid.FindAll(Cue).ShouldBeEmpty());
        audible.Count.ShouldBe(before);
        ValueTask<Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderResult<CounterProjection>> page = default;
        await grid.InvokeAsync(() => page = (ValueTask<Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderResult<CounterProjection>>)
            typeof(CounterProjectionView).GetMethod("LoadPageAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(grid.Instance, [default(Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderRequest<CounterProjection>)])!);
        (await page.ConfigureAwait(true)).Items.Single().Id.ShouldBe("filtered-row");
        queriedFilters.ShouldNotBeNull()["Id"].ShouldBe("filtered-row");
        Services.GetRequiredService<IState<LoadedPageState>>().Value.PagesByKey[(ViewKey, 0)]
            .OfType<CounterProjection>().Single().Id.ShouldBe("filtered-row");
        indicators.Snapshot(ViewKey).ShouldBeEmpty();
        spoken.ShouldBe(["Updated row: fresh-row."]);
    }

    [Theory]
    [InlineData("tenant")]
    [InlineData("user")]
    [InlineData("missing-tenant")]
    [InlineData("missing-user")]
    public async Task ScopeChangeClearsCueAndClaimsBeforeNewScopeRendersWithoutSpeech(string change)
    {
        await InitializeStoreAsync();
        Seed("existing-row");
        var grid = Render<CounterProjectionView>();
        var indicators = Services.GetRequiredService<INewItemIndicatorStateService>();
        List<string> spoken = [];
        List<string> audible = [];
        using var subscription = ObserveFreshSpeech(Services.GetRequiredService<ISurfaceAnnouncementCoordinator>(), spoken, audible);
        var entry = Entry("fresh-row", "message");
        indicators.Add(entry);
        grid.WaitForAssertion(() => spoken.Count.ShouldBe(1));
        int before = audible.Count;
        if (change == "tenant") _scope.TenantId = "other-tenant";
        if (change == "user") _scope.UserId = "other-user";
        if (change == "missing-tenant") _scope.TenantId = null;
        if (change == "missing-user") _scope.UserId = " ";
        indicators.Snapshot(ViewKey).ShouldBeEmpty();
        grid.WaitForAssertion(() => grid.FindAll(Cue).ShouldBeEmpty());
        audible.Count.ShouldBe(before);
        _scope.TenantId = "test-tenant";
        _scope.UserId = "test-user";
        FreshRowAnnouncementLedger ledger = Services.GetRequiredService<FreshRowAnnouncementLedger>();
        ledger.TryClaim(entry).ShouldBeFalse();
        _time.Advance(TimeSpan.FromMilliseconds(1));
        ledger.TryClaim(Entry("fresh-row", "message")).ShouldBeTrue();
        spoken.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("unknown-materiality")]
    [InlineData("unknown-identity")]
    [InlineData("no-op")]
    [InlineData("delete")]
    [InlineData("rejected")]
    [InlineData("needs-review")]
    [InlineData("server-allocated")]
    public async Task SuppressedOutcomeProducesNeitherCueNorAm21(string scenario)
    {
        await InitializeStoreAsync();
        Seed("existing-row");
        var grid = Render<CounterProjectionView>();
        const string message = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        const string correlation = "01CPZ3NDEKTSV4RRFFQ69G5FAV";
        var pending = Services.GetRequiredService<IPendingCommandStateService>();
        var resolver = Services.GetRequiredService<IPendingCommandOutcomeResolver>();
        var target = scenario is "unknown-identity" or "server-allocated" ? null : new CommandTargetSnapshot(
            "Counter.Domain.CounterProjection", ViewKey, "fresh-row",
            scenario == "delete" ? CommandTargetChangeKind.Delete : CommandTargetChangeKind.Update,
            null, null, "test-tenant", "test-user", _time.GetUtcNow());
        pending.Register(new(correlation, message, "TestCommand", "Counter.Domain.CounterProjection", ViewKey,
            scenario == "server-allocated" ? null : "fresh-row", SubmittedAt: _time.GetUtcNow()) { TargetSnapshot = target });
        List<string> spoken = [];
        using var subscription = ObserveFreshSpeech(Services.GetRequiredService<ISurfaceAnnouncementCoordinator>(), spoken);
        resolver.Resolve(new(PendingCommandOutcomeSource.LiveNudgeRefresh,
            scenario == "rejected" ? PendingCommandTerminalOutcome.Rejected
                : scenario == "needs-review" ? PendingCommandTerminalOutcome.NeedsReview : PendingCommandTerminalOutcome.Confirmed,
            MessageId: message, ObservedAt: _time.GetUtcNow()) {
            Materiality = scenario == "unknown-materiality" ? CommandMateriality.Unknown
                : scenario == "no-op" ? CommandMateriality.NoOp : CommandMateriality.Material,
        });
        await grid.InvokeAsync(() => grid.Render());
        grid.FindAll(Cue).ShouldBeEmpty();
        spoken.ShouldBeEmpty();
    }

    private static IDisposable ObserveFreshSpeech(ISurfaceAnnouncementCoordinator speech, List<string> spoken, List<string>? audible = null) =>
        speech.SubscribeWithReplay("projection:" + ViewKey, (message, revision) => {
            if (message.Length == 0
                || speech is SurfaceAnnouncementCoordinator coordinator && coordinator.IsSilentRevision("projection:" + ViewKey, revision))
                return;
            audible?.Add(message);
            if (message.StartsWith("Updated row:", StringComparison.Ordinal))
                spoken.Add(message);
        });

    private NewItemIndicatorEntry Entry(string entity, string message) => new(ViewKey, entity, message, _time.GetUtcNow());

    private void Seed(string key) => Services.GetRequiredService<IDispatcher>().Dispatch(
        new CounterProjectionLoadedAction("seed", [new CounterProjection { Id = key, Count = 1 }]) {
            ScopeTenantId = _scope.TenantId, ScopeUserId = _scope.UserId,
        });
}
