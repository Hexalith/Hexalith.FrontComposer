using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;

using Bunit;

using Counter.Domain;
using Counter.Web;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.Announcements;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.Infrastructure.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
using Hexalith.FrontComposer.Shell.State.Navigation;
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
    public async Task GeneratedGridSlowNoticeUsesDisplayedPageAtThreshold()
    {
        FakeTimeProvider time = new();
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(time));
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        Services.GetRequiredService<IDispatcher>().Dispatch(new CounterProjectionLoadedAction("seed",
            [new CounterProjection { Id = "visible-row", Count = 1 }])
            { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        LoadedPageState state = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        TaskCompletionSource<object> offscreen = new();
        await cut.InvokeAsync(() => pages.RestoreState(state with {
            PendingCompletionsByKey = state.PendingCompletionsByKey.SetItem((ViewKey, 0), offscreen),
            PendingStartedAtByKey = state.PendingStartedAtByKey.SetItem((ViewKey, 0), time.GetUtcNow()),
        }));

        time.Advance(TimeSpan.FromMilliseconds(1000));
        state = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        TaskCompletionSource<object> displayed = new();
        await cut.InvokeAsync(() => pages.RestoreState(state with {
            PendingCompletionsByKey = state.PendingCompletionsByKey.SetItem((ViewKey, 500), displayed),
            PendingStartedAtByKey = state.PendingStartedAtByKey.SetItem((ViewKey, 500), time.GetUtcNow()),
        }));
        await cut.InvokeAsync(() => {
            cut.Instance.HandleScrollAsync(ViewKey, 500d * 44d);
            cut.Render();
        });
        cut.FindComponent<Hexalith.FrontComposer.Shell.Components.DataGrid.FcSlowQueryNotice>()
            .Instance.VisiblePageSkip.ShouldBe(500);

        time.Advance(TimeSpan.FromMilliseconds(1999));
        cut.FindAll("[data-testid='fc-slow-query-notice']").ShouldBeEmpty();
        time.Advance(TimeSpan.FromMilliseconds(1));
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='fc-slow-query-notice']").Count.ShouldBe(1));
        cut.Find(StatusSelector).TextContent.ShouldBe("This is taking longer than expected.");
    }

    [Fact]
    public async Task ThrowingScopeAccessorBlocksGeneratedPageAndRetryUntilCurrentScopeRecovers()
    {
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProjectionPageResult([new CounterProjection { Id = "current-row", Count = 2 }], 1, "etag")));
        Services.Replace(ServiceDescriptor.Singleton(loader));
        Services.AddFluxor(options => options.ScanAssemblies(typeof(CounterProjection).Assembly)
            .ScanTypes(typeof(LoadedPageFeature), typeof(LoadedPageReducers), typeof(LoadPageEffects)));
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadRequestedAction("seed", true)
            { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadedAction("seed", [new CounterProjection { Id = "private-old-row", Count = 1 }])
            { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        Services.GetRequiredService<IState<CounterProjectionState>>().Value.Items!.Single().Id.ShouldBe("private-old-row");
        TestTenantContextAccessor scope = (TestTenantContextAccessor)Services.GetRequiredService<
            Hexalith.FrontComposer.Shell.Infrastructure.Tenancy.IFrontComposerTenantContextAccessor>();
        scope.ThrowForOperationKind = "*";
        cut.Render();
        cut.FindComponent<Hexalith.FrontComposer.Shell.Components.Rendering.FcScopeBlocked>().ShouldNotBeNull();
        cut.Markup.ShouldNotContain("private-old-row");

        IActionSubscriber actions = Services.GetRequiredService<IActionSubscriber>();
        object owner = new();
        List<LoadPageAction> pageActions = [];
        List<CounterProjectionLoadRequestedAction> retries = [];
        actions.SubscribeToAction<LoadPageAction>(owner, pageActions.Add);
        actions.SubscribeToAction<CounterProjectionLoadRequestedAction>(owner, retries.Add);
        try
        {
            ValueTask<Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderResult<CounterProjection>> blocked = default;
            await cut.InvokeAsync(() => blocked = (ValueTask<Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderResult<CounterProjection>>)
                typeof(CounterProjectionView).GetMethod("LoadPageAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .Invoke(cut.Instance, [default(Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderRequest<CounterProjection>)])!);
            (await blocked.ConfigureAwait(true)).Items.ShouldBeEmpty();
            await cut.InvokeAsync(() => (Task)typeof(CounterProjectionView)
                .GetMethod("RetryProjectionAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(cut.Instance, null)!);
            pageActions.ShouldBeEmpty();
            retries.ShouldBeEmpty();
            await loader.DidNotReceiveWithAnyArgs().LoadPageAsync(default!, default, default, default!, default, default,
                default, Xunit.TestContext.Current.CancellationToken);

            scope.ThrowForOperationKind = null;
            cut.Render();
            ValueTask<Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderResult<CounterProjection>> recovered = default;
            await cut.InvokeAsync(() => recovered = (ValueTask<Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderResult<CounterProjection>>)
                typeof(CounterProjectionView).GetMethod("LoadPageAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .Invoke(cut.Instance, [default(Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderRequest<CounterProjection>)])!);
            (await recovered.ConfigureAwait(true)).Items.Single().Id.ShouldBe("current-row");
            pageActions.Count.ShouldBe(1);
            pageActions[0].OriginTenantId.ShouldBe("test-tenant");
            pageActions[0].OriginUserId.ShouldBe("test-user");
        }
        finally
        {
            actions.UnsubscribeFromAllActions(owner);
        }
    }

    [Fact]
    public async Task FailedNewUserAtPriorPageGenerationCannotRevealCachedRows()
    {
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        IRenderedComponent<CounterProjectionView> first = Render<CounterProjectionView>();
        await first.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadRequestedAction("old-user", true)
            { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        await first.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadedAction("old-user", [new CounterProjection { Id = "private-old-entry" }])
            { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        first.Markup.ShouldContain("private-old-entry");
        long priorGeneration = Services.GetRequiredService<IState<LoadedPageState>>().Value.ScopeGeneration;

        TestTenantContextAccessor scope = (TestTenantContextAccessor)Services.GetRequiredService<
            Hexalith.FrontComposer.Shell.Infrastructure.Tenancy.IFrontComposerTenantContextAccessor>();
        scope.UserId = "user-b";
        await first.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadFailedAction("new-user", "safe failure")
            { ScopeTenantId = "test-tenant", ScopeUserId = "user-b" }));
        IRenderedComponent<CounterProjectionView> second = Render<CounterProjectionView>();
        second.FindComponent<Hexalith.FrontComposer.Shell.Components.Rendering.FcScopeBlocked>().ShouldNotBeNull();
        second.Markup.ShouldNotContain("private-old-entry");
        Services.GetRequiredService<IState<LoadedPageState>>().Value.ScopeGeneration.ShouldBe(priorGeneration);

        await second.InvokeAsync(() => dispatcher.Dispatch(new ScopeChangedAction()));
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        pages.RestoreState(LoadedPageReducers.ReduceScopeChanged(Services.GetRequiredService<IState<LoadedPageState>>().Value,
            new ScopeChangedAction()));
        Services.GetRequiredService<IState<LoadedPageState>>().Value.ScopeGeneration.ShouldBeGreaterThan(priorGeneration);
        long currentGeneration = Services.GetRequiredService<IState<CounterProjectionState>>().Value.ScopeGeneration;
        await second.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadRequestedAction("current-user", true)
            { ScopeTenantId = "test-tenant", ScopeUserId = "user-b", OriginScopeGeneration = currentGeneration }));
        await second.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadedAction("current-user", [new CounterProjection { Id = "current-b-entry" }])
            { ScopeTenantId = "test-tenant", ScopeUserId = "user-b", OriginScopeGeneration = currentGeneration }));
        second.WaitForAssertion(() => second.Markup.ShouldContain("current-b-entry"));
        second.Markup.ShouldNotContain("private-old-entry");
    }

    [Fact]
    public async Task UnstampedResultBeforeFirstMountCannotBeAssignedToNewUser()
    {
        FakeTimeProvider time = new();
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(time));
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new CounterProjectionLoadedAction("unprovable-a", [new CounterProjection { Id = "private-a" }]));
        TestTenantContextAccessor scope = (TestTenantContextAccessor)Services.GetRequiredService<
            Hexalith.FrontComposer.Shell.Infrastructure.Tenancy.IFrontComposerTenantContextAccessor>();
        scope.UserId = "user-b";

        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        cut.FindComponent<Hexalith.FrontComposer.Shell.Components.Rendering.FcScopeBlocked>().ShouldNotBeNull();
        cut.Markup.ShouldNotContain("private-a");
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        announcements.Current("projection:" + ViewKey).ShouldBeEmpty();

        await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadRequestedAction("current-b", true)
            { ScopeTenantId = "test-tenant", ScopeUserId = "user-b" }));
        await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadedAction("current-b", [new CounterProjection { Id = "current-b" }])
            { ScopeTenantId = "test-tenant", ScopeUserId = "user-b" }));
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("current-b"));
        cut.Markup.ShouldNotContain("private-a");
        time.Advance(TimeSpan.FromMilliseconds(250));
        cut.WaitForAssertion(() => announcements.Current("projection:" + ViewKey).ShouldNotBeEmpty());
        cut.FindAll("[role='status'][aria-live='polite']").Count.ShouldBe(1);
    }

    [Fact]
    public async Task CachedProjectionWithoutRenderContextKeepsProducerUserUntilCurrentResult()
    {
        FakeTimeProvider time = new();
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(time));
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        IRenderedComponent<CounterProjectionView> first = Render<CounterProjectionView>();
        await first.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadRequestedAction("a", true)
            { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        await first.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadedAction("a", [new CounterProjection { Id = "private-old-entry" }])
            { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        first.Markup.ShouldContain("private-old-entry");

        TestTenantContextAccessor scope = (TestTenantContextAccessor)Services.GetRequiredService<
            Hexalith.FrontComposer.Shell.Infrastructure.Tenancy.IFrontComposerTenantContextAccessor>();
        scope.UserId = "user-b";
        first.Render();
        IRenderedComponent<CounterProjectionView> second = Render<CounterProjectionView>();
        first.FindComponent<Hexalith.FrontComposer.Shell.Components.Rendering.FcScopeBlocked>().ShouldNotBeNull();
        second.FindComponent<Hexalith.FrontComposer.Shell.Components.Rendering.FcScopeBlocked>().ShouldNotBeNull();
        first.Markup.ShouldNotContain("private-old-entry");
        second.Markup.ShouldNotContain("private-old-entry");
        announcements.Current("projection:" + ViewKey).ShouldBeEmpty();

        await second.InvokeAsync(() => dispatcher.Dispatch(new ScopeChangedAction()));
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        pages.RestoreState(LoadedPageReducers.ReduceScopeChanged(Services.GetRequiredService<IState<LoadedPageState>>().Value,
            new ScopeChangedAction()));
        long currentGeneration = Services.GetRequiredService<IState<CounterProjectionState>>().Value.ScopeGeneration;
        await second.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadRequestedAction("b", true)
            { ScopeTenantId = "test-tenant", ScopeUserId = "user-b", OriginScopeGeneration = currentGeneration }));
        await second.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadedAction("b", [new CounterProjection { Id = "current-new-entry" }])
            { ScopeTenantId = "test-tenant", ScopeUserId = "user-b", OriginScopeGeneration = currentGeneration }));
        LoadedPageState pageState = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        pageState.ScopeGeneration.ShouldBeGreaterThan(0);
        Hexalith.FrontComposer.Shell.Services.ProjectionStateScopeOwnership.For(dispatcher)
            .IsCurrent("projection:" + ViewKey, "test-tenant", "user-b", scope.TryGetContext().Context.ShouldNotBeNull(), pageState.ScopeGeneration)
            .ShouldBeTrue();
        second.WaitForAssertion(() => second.Markup.ShouldContain("current-new-entry"));
        second.Markup.ShouldNotContain("private-old-entry");
        time.Advance(TimeSpan.FromMilliseconds(250));
        (first.FindAll("[role='status'][aria-live='polite']").Count
            + second.FindAll("[role='status'][aria-live='polite']").Count).ShouldBe(1);
    }

    [Fact]
    public async Task SameTenantUserChangeWithdrawsGeneratedProjectionSpeechBeforeReducerReset()
    {
        FakeTimeProvider time = new();
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(time));
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        RenderContext userA = new("test-tenant", "test-user", FcRenderMode.Server, DensityLevel.Comfortable, false);
        IRenderedComponent<Microsoft.AspNetCore.Components.CascadingValue<RenderContext>> hostA =
            Render<Microsoft.AspNetCore.Components.CascadingValue<RenderContext>>(parameters => parameters
                .Add(component => component.Value, userA)
                .AddChildContent<CounterProjectionView>());
        IRenderedComponent<CounterProjectionView> cutA = hostA.FindComponent<CounterProjectionView>();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable subscription = announcements.Subscribe("projection:" + ViewKey, message => {
            if (message.Length > 0) { spoken.Add(message); }
        });
        await cutA.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadRequestedAction("user-a", true) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        TestTenantContextAccessor scope = (TestTenantContextAccessor)Services.GetRequiredService<
            Hexalith.FrontComposer.Shell.Infrastructure.Tenancy.IFrontComposerTenantContextAccessor>();
        scope.UserId = "user-b";
        await cutA.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadFailedAction("user-a", "backend secret") { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        cutA.Render();
        cutA.FindComponent<Hexalith.FrontComposer.Shell.Components.Rendering.FcScopeBlocked>().ShouldNotBeNull();
        announcements.Current("projection:" + ViewKey).ShouldBeEmpty();
        time.Advance(TimeSpan.FromMilliseconds(250));
        spoken.ShouldBeEmpty();

        RenderContext userB = userA with { UserId = "user-b" };
        IRenderedComponent<Microsoft.AspNetCore.Components.CascadingValue<RenderContext>> hostB =
            Render<Microsoft.AspNetCore.Components.CascadingValue<RenderContext>>(parameters => parameters
                .Add(component => component.Value, userB)
                .AddChildContent<CounterProjectionView>());
        IRenderedComponent<CounterProjectionView> cutB = hostB.FindComponent<CounterProjectionView>();
        await cutB.InvokeAsync(() => dispatcher.Dispatch(new ScopeChangedAction()));
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        pages.RestoreState(LoadedPageReducers.ReduceScopeChanged(Services.GetRequiredService<IState<LoadedPageState>>().Value,
            new ScopeChangedAction()));
        long currentGeneration = Services.GetRequiredService<IState<CounterProjectionState>>().Value.ScopeGeneration;
        await cutB.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadRequestedAction("user-b", true) { ScopeTenantId = "test-tenant", ScopeUserId = "user-b", OriginScopeGeneration = currentGeneration }));
        await cutB.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadedAction("user-b", []) { ScopeTenantId = "test-tenant", ScopeUserId = "user-b", OriginScopeGeneration = currentGeneration }));
        Services.GetRequiredService<IState<LoadedPageState>>().Value.ScopeGeneration.ShouldBeGreaterThan(0);
        cutB.WaitForAssertion(() => cutB.Find(StatusSelector).TextContent.ShouldBe("No counters available."));
        spoken.Count(message => message == "No counters available.").ShouldBe(1);
        cutA.FindComponent<Hexalith.FrontComposer.Shell.Components.Rendering.FcScopeBlocked>().ShouldNotBeNull();
    }

    [Fact]
    public async Task GeneratedLoadingUsesOneStatusWhileSkeletonRemainsAccessibleThroughFailure()
    {
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        IRenderedComponent<CounterProjectionView> first = Render<CounterProjectionView>();
        IRenderedComponent<CounterProjectionView> second = Render<CounterProjectionView>();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

        await first.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadRequestedAction("loading-status", true) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        first.WaitForAssertion(() => {
            (first.FindAll("[role='status']").Count + second.FindAll("[role='status']").Count).ShouldBe(1);
            first.Find("[role='group'][aria-busy='true'][aria-label='Loading counters']").ShouldNotBeNull();
            second.Find("[role='group'][aria-busy='true'][aria-label='Loading counters']").ShouldNotBeNull();
        });

        await first.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadFailedAction("loading-status", "This section is being updated") { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        first.WaitForAssertion(() => {
            (first.FindAll("[role='status']").Count + second.FindAll("[role='status']").Count).ShouldBe(1);
            first.Find("[data-testid='fc-projection-failure']").ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task GeneratedGridReplacesDelimiterBearingFilterLaneWithActualCurrentMap()
    {
        IProjectionFallbackRefreshScheduler scheduler = Substitute.For<IProjectionFallbackRefreshScheduler>();
        List<ProjectionFallbackLane> registered = [];
        scheduler.RegisterReplacingLane(Arg.Any<ProjectionFallbackLane>()).Returns(call =>
        {
            registered.Add((ProjectionFallbackLane)call[0]!);
            return Substitute.For<IDisposable>();
        });
        Services.Replace(ServiceDescriptor.Scoped<IProjectionFallbackRefreshScheduler>(_ => scheduler));
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        IImmutableDictionary<string, string> firstFilters = ImmutableDictionary<string, string>.Empty.Add("A", "B\u001fC\u001eD");
        IImmutableDictionary<string, string> secondFilters = ImmutableDictionary<string, string>.Empty.Add("A", "B").Add("C", "D");
        GridViewSnapshot first = new(0, firstFilters, null, false, null, null, DateTimeOffset.UtcNow);
        DataGridNavigationFeature navigation = Services.GetRequiredService<DataGridNavigationFeature>();
        DataGridNavigationState current = Services.GetRequiredService<IState<DataGridNavigationState>>().Value;
        navigation.RestoreState(current with { ViewStates = current.ViewStates.SetItem(ViewKey, first) });
        RenderContext context = new("test-tenant", "test-user", FcRenderMode.Server, DensityLevel.Comfortable, false);
        IRenderedComponent<Microsoft.AspNetCore.Components.CascadingValue<RenderContext>> host =
            Render<Microsoft.AspNetCore.Components.CascadingValue<RenderContext>>(parameters => parameters
                .Add(component => component.Value, context)
                .Add(component => component.IsFixed, true)
                .AddChildContent<CounterProjectionView>());
        IRenderedComponent<CounterProjectionView> cut = host.FindComponent<CounterProjectionView>();
        cut.WaitForAssertion(() => registered.Last().Filters.ShouldBe(firstFilters));
        GridViewSnapshot second = new(0, secondFilters, null, false, null, null, DateTimeOffset.UtcNow);
        await cut.InvokeAsync(() =>
        {
            DataGridNavigationState state = Services.GetRequiredService<IState<DataGridNavigationState>>().Value;
            navigation.RestoreState(state with { ViewStates = state.ViewStates.SetItem(ViewKey, second) });
        });
        cut.WaitForAssertion(() =>
        {
            registered.Count.ShouldBeGreaterThanOrEqualTo(2);
            registered.Last().Filters.ShouldBe(secondFilters);
        });
    }

    [Fact]
    public async Task GeneratedGridRebindsFallbackLaneToCurrentUserAndPageScope()
    {
        IProjectionFallbackRefreshScheduler scheduler = Substitute.For<IProjectionFallbackRefreshScheduler>();
        List<ProjectionFallbackLane> lanes = [];
        List<IDisposable> registrations = [];
        scheduler.RegisterReplacingLane(Arg.Any<ProjectionFallbackLane>()).Returns(call => {
            lanes.Add((ProjectionFallbackLane)call[0]!);
            IDisposable registration = Substitute.For<IDisposable>();
            registrations.Add(registration);
            return registration;
        });
        Services.Replace(ServiceDescriptor.Scoped<IProjectionFallbackRefreshScheduler>(_ => scheduler));
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        cut.WaitForAssertion(() => lanes.Last().UserId.ShouldBe("test-user"));
        lanes.Last().ScopeGeneration.ShouldBe(0);

        TestTenantContextAccessor scope = (TestTenantContextAccessor)Services.GetRequiredService<
            Hexalith.FrontComposer.Shell.Infrastructure.Tenancy.IFrontComposerTenantContextAccessor>();
        scope.UserId = "user-b";
        cut.Render();
        registrations[0].Received(1).Dispose();
        cut.FindComponent<Hexalith.FrontComposer.Shell.Components.Rendering.FcScopeBlocked>().ShouldNotBeNull();

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        await cut.InvokeAsync(() => dispatcher.Dispatch(new ScopeChangedAction()));
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        await cut.InvokeAsync(() => pages.RestoreState(LoadedPageReducers.ReduceScopeChanged(
            Services.GetRequiredService<IState<LoadedPageState>>().Value, new ScopeChangedAction())));
        cut.Render();
        cut.WaitForAssertion(() => {
            lanes.Last().TenantId.ShouldBe("test-tenant");
            lanes.Last().UserId.ShouldBe("user-b");
            lanes.Last().ScopeGeneration.ShouldBe(1);
        });
    }

    [Fact]
    public async Task GeneratedFreshnessMarkerSerializesOnlyBoundedCachedPageEvidence()
    {
        Services.Configure<Hexalith.FrontComposer.Shell.Options.FcShellOptions>(options => options.MaxCachedPages = 10);
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        CounterProjection row = new() { Id = "cached", Count = 1 };
        dispatcher.Dispatch(new CounterProjectionLoadedAction("seed", [row]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        IState<LoadedPageState> pageState = Services.GetRequiredService<IState<LoadedPageState>>();
        LoadedPageReducers reducers = new(Services.GetRequiredService<IOptionsMonitor<Hexalith.FrontComposer.Shell.Options.FcShellOptions>>(),
            NullLogger<LoadedPageReducers>.Instance);
        for (int offset = 0; offset < 600; offset += 20)
        {
            LoadedPageState next = reducers.ReduceLoadPageSucceeded(pageState.Value,
                new LoadPageSucceededAction(ViewKey, offset, [row], 600, 1)
                { OriginScopeGeneration = pageState.Value.ScopeGeneration });
            await cut.InvokeAsync(() => pages.RestoreState(next));
            pageState.Value.SuccessfulReadSequenceByPage.Count.ShouldBeLessThanOrEqualTo(10);
            string marker = cut.Find("[data-fc-browser-stale]").GetAttribute("data-fc-cached-result").ShouldNotBeNull();
            marker.Split('|').Length.ShouldBeLessThanOrEqualTo(11);
        }
    }

    [Fact]
    public async Task GeneratedFreshnessMarkerTracksDisplayedCachedPageDuringFailure()
    {
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new CounterProjectionLoadedAction("seed", [new CounterProjection { Id = "cached", Count = 1 }]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        IState<LoadedPageState> state = Services.GetRequiredService<IState<LoadedPageState>>();
        await cut.InvokeAsync(() => pages.RestoreState(state.Value with {
            SuccessfulReadSequenceByPage = state.Value.SuccessfulReadSequenceByPage
                .SetItem((ViewKey, 0), 1).SetItem((ViewKey, 500), 1),
        }));
        cut.Find("[data-fc-browser-stale]").GetAttribute("data-fc-cached-result").ShouldBe("pages:0|0:1|500:1");

        await cut.InvokeAsync(() => {
            cut.Instance.HandleScrollAsync(ViewKey, 500d * 44d);
            cut.Render();
        });
        cut.Find("[data-fc-browser-stale]").GetAttribute("data-fc-cached-result").ShouldBe("pages:500|0:1|500:1");
        await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadFailedAction("failed", "This section is being updated") { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        await cut.InvokeAsync(() => {
            cut.Instance.HandleScrollAsync(ViewKey, 0d);
            cut.Render();
        });
        cut.Find("[data-fc-browser-stale]").GetAttribute("data-fc-cached-result").ShouldBe("pages:0|0:1|500:1");
    }

    [Fact]
    public async Task CoMountedGeneratedPageProvidersBothSettleFromSameCurrentRequest()
    {
        TaskCompletionSource<ProjectionPageResult> held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(_ => held.Task);
        Services.Replace(ServiceDescriptor.Singleton(loader));
        Services.AddFluxor(options => options.ScanTypes(typeof(LoadedPageFeature), typeof(LoadedPageReducers), typeof(LoadPageEffects)));
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        IRenderedComponent<CounterProjectionView> first = Render<CounterProjectionView>();
        IRenderedComponent<CounterProjectionView> second = Render<CounterProjectionView>();
        static ValueTask<Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderResult<CounterProjection>> Start(IRenderedComponent<CounterProjectionView> cut)
            => (ValueTask<Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderResult<CounterProjection>>)typeof(CounterProjectionView)
                .GetMethod("LoadPageAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(cut.Instance, [default(Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderRequest<CounterProjection>)])!;

        ValueTask<Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderResult<CounterProjection>> firstPage = default;
        ValueTask<Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderResult<CounterProjection>> secondPage = default;
        await first.InvokeAsync(() => firstPage = Start(first));
        await second.InvokeAsync(() => secondPage = Start(second));
        firstPage.IsCompleted.ShouldBeFalse();
        secondPage.IsCompleted.ShouldBeFalse();
        CounterProjection row = new() { Id = "shared", Count = 1 };
        held.SetResult(new ProjectionPageResult([row], 1, "etag"));
        (await firstPage.ConfigureAwait(true)).Items.Single().Id.ShouldBe("shared");
        (await secondPage.ConfigureAwait(true)).Items.Single().Id.ShouldBe("shared");
    }

    [Fact]
    public async Task CoMountedGeneratedPageProviderSurvivesOtherOwnersFailedRead()
    {
        TaskCompletionSource<ProjectionPageResult> failedRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<ProjectionPageResult> successfulRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(_ =>
                Interlocked.Increment(ref calls) == 1 ? failedRead.Task : successfulRead.Task);
        Services.Replace(ServiceDescriptor.Singleton(loader));
        Services.AddFluxor(options => options.ScanTypes(typeof(LoadedPageFeature), typeof(LoadedPageReducers), typeof(LoadPageEffects)));
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        IRenderedComponent<CounterProjectionView> first = Render<CounterProjectionView>();
        IRenderedComponent<CounterProjectionView> second = Render<CounterProjectionView>();
        static ValueTask<Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderResult<CounterProjection>> Start(IRenderedComponent<CounterProjectionView> cut)
            => (ValueTask<Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderResult<CounterProjection>>)typeof(CounterProjectionView)
                .GetMethod("LoadPageAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(cut.Instance, [default(Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderRequest<CounterProjection>)])!;

        ValueTask<Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderResult<CounterProjection>> firstPage = default;
        ValueTask<Microsoft.FluentUI.AspNetCore.Components.GridItemsProviderResult<CounterProjection>> secondPage = default;
        await first.InvokeAsync(() => firstPage = Start(first));
        await second.InvokeAsync(() => secondPage = Start(second));
        calls.ShouldBe(2);
        failedRead.SetException(new InvalidOperationException("sensitive transport detail"));
        (await Should.ThrowAsync<InvalidOperationException>(async () => await firstPage.ConfigureAwait(true)))
            .Message.ShouldBe("Data could not be loaded.");
        secondPage.IsCompleted.ShouldBeFalse();

        successfulRead.SetResult(new ProjectionPageResult([new CounterProjection { Id = "survivor", Count = 1 }], 1, "etag"));
        (await secondPage.ConfigureAwait(true)).Items.Single().Id.ShouldBe("survivor");
    }

    [Fact]
    public async Task DirectProjectionFailureClassifiesSensitiveStateErrorBeforeRendering()
    {
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new CounterProjectionLoadedAction("seed", [new CounterProjection { Id = "cached-row", Count = 1 }]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadFailedAction(
            "direct-failure", "tenant-secret backend failure") { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        cut.WaitForAssertion(() => {
            cut.Find("[data-testid='fc-projection-failure']").TextContent.ShouldContain("Data could not be loaded.");
            cut.Markup.ShouldNotContain("tenant-secret");
            cut.Find(StatusSelector).TextContent.ShouldBe("Data could not be loaded.");
        });
    }

    [Fact]
    public async Task SamePageResultNumberInNewScopeSpeaksThroughCurrentSingleStatus()
    {
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        IRenderedComponent<CounterProjectionView> coMounted = Render<CounterProjectionView>();
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        IState<LoadedPageState> state = Services.GetRequiredService<IState<LoadedPageState>>();
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable subscription = announcements.Subscribe("projection:" + ViewKey, message => {
            if (message.Length > 0) { spoken.Add(message); }
        });
        string request = ProjectionRequestIdentity(cut);

        async Task PublishPageOneAsync()
        {
            LoadedPageState current = state.Value;
            await cut.InvokeAsync(() => pages.RestoreState(current with {
                ActiveRequestIdentityByView = current.ActiveRequestIdentityByView.SetItem(ViewKey, request),
                LastResultByKey = current.LastResultByKey.SetItem(ViewKey,
                    new LoadedPageResult(1, request, 0, Failed: false, OperatorInitiated: true)),
            })).ConfigureAwait(true);
        }

        await PublishPageOneAsync();
        cut.WaitForAssertion(() => spoken.Count.ShouldBe(1));
        LoadedPageState prior = state.Value;
        await cut.InvokeAsync(() => pages.RestoreState(new LoadedPageState { ScopeGeneration = prior.ScopeGeneration + 1 }));
        state.Value.PagesByKey.ShouldBeEmpty();
        await PublishPageOneAsync();
        cut.WaitForAssertion(() => {
            spoken.ShouldBe(["No counters available.", "No counters available."]);
            cut.FindAll(StatusSelector).Count.ShouldBe(1);
            coMounted.FindAll(StatusSelector).ShouldBeEmpty();
        });
    }

    [Theory]
    [InlineData("unclassified backend payload", "Data could not be loaded.")]
    [InlineData("This section is being updated", "This section is being updated")]
    public async Task DirectPageFailureUsesOnlySafeClassifiedVisibleAndSpokenCopy(string raw, string expected)
    {
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new CounterProjectionLoadedAction("seed", [new CounterProjection { Id = "cached-row", Count = 1 }]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        LoadedPageState state = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        string request = ProjectionRequestIdentity(cut);
        await cut.InvokeAsync(() => pages.RestoreState(state with {
            ActiveRequestIdentityByView = state.ActiveRequestIdentityByView.SetItem(ViewKey, request),
            LastResultByKey = state.LastResultByKey.SetItem(ViewKey,
                new LoadedPageResult(1, request, 1, Failed: true, OperatorInitiated: true) { ErrorMessage = raw }),
        }));

        cut.WaitForAssertion(() => {
            cut.Find(StatusSelector).TextContent.ShouldBe(expected);
            cut.Find("[data-testid='fc-projection-failure']").TextContent.ShouldContain(expected);
            cut.Markup.ShouldNotContain("unclassified backend payload");
            cut.Markup.ShouldContain("cached-row");
        });
    }

    [Fact]
    public async Task CoMountedGridKeepsPendingPageAndScrollStateUntilFinalOwnerLeaves()
    {
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        BunitJSModuleInterop scroll = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-datagrid.js");
        await InitializeStoreAsync();
        IRenderedComponent<CounterProjectionView> first = Render<CounterProjectionView>();
        IRenderedComponent<CounterProjectionView> second = Render<CounterProjectionView>();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        await first.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadedAction("grid-seed",
            [new CounterProjection { Id = "grid-seed", Count = 1 }])
            { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        string firstInstance = first.Find("[data-fc-datagrid]").GetAttribute("data-fc-datagrid-instance").ShouldNotBeNull();
        string secondInstance = second.Find("[data-fc-datagrid]").GetAttribute("data-fc-datagrid-instance").ShouldNotBeNull();
        firstInstance.ShouldNotBe(secondInstance);
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        IState<LoadedPageState> state = Services.GetRequiredService<IState<LoadedPageState>>();
        TaskCompletionSource<object> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        LoadPageAction request = new(ViewKey, 0, 20, ImmutableDictionary<string, string>.Empty,
            null, false, null, completion, CancellationToken.None);
        await first.InvokeAsync(() => pages.RestoreState(LoadedPageReducers.ReduceLoadPage(state.Value, request)));
        int clears = 0;
        dispatcher.ActionDispatched += (_, args) => {
            if (args.Action is ClearPendingPagesAction clear) {
                clears++;
                pages.RestoreState(LoadedPageReducers.ReduceClearPendingPages(state.Value, clear));
            }
        };

        await first.Instance.DisposeAsync();
        first.Dispose();
        clears.ShouldBe(0);
        completion.Task.IsCompleted.ShouldBeFalse();
        state.Value.PendingCompletionsByKey.ShouldContainKey((ViewKey, 0));
        scroll.Invocations.Count(invocation => invocation.Identifier == "disposeViewKey").ShouldBe(1);

        await second.Instance.DisposeAsync();
        second.Dispose();
        clears.ShouldBe(1);
        completion.Task.IsCanceled.ShouldBeTrue();
        scroll.Invocations.Count(invocation => invocation.Identifier == "disposeViewKey").ShouldBe(2);
    }

    [Fact]
    public async Task HeldGeneratedScrollCleanupCannotTargetReplacementGeneration()
    {
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        BunitJSModuleInterop scroll = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-datagrid.js");
        var heldCleanup = scroll.SetupVoid("disposeViewKey", _ => true);
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new CounterProjectionLoadedAction("seed", [new CounterProjection { Id = "cached", Count = 1 }])
            { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
        IRenderedComponent<CounterProjectionView> oldView = Render<CounterProjectionView>();
        Task oldDisposal = oldView.Instance.DisposeAsync().AsTask();
        oldDisposal.IsCompleted.ShouldBeFalse();
        var oldCleanup = scroll.Invocations.Single(invocation => invocation.Identifier == "disposeViewKey");
        long oldGeneration = oldCleanup.Arguments[1].ShouldBeOfType<long>();

        IRenderedComponent<CounterProjectionView> replacement = Render<CounterProjectionView>();
        replacement.Find("[data-fc-datagrid]").TriggerEvent("onscroll", EventArgs.Empty);
        var pendingScroll = scroll.Invocations.Last(invocation => invocation.Identifier == "captureScrollThrottled");
        pendingScroll.Arguments[0].ShouldBe(ViewKey);
        long replacementGeneration = pendingScroll.Arguments[3].ShouldBeOfType<long>();
        replacementGeneration.ShouldBeGreaterThan(oldGeneration);

        heldCleanup.SetVoidResult();
        await oldDisposal.ConfigureAwait(true);
        scroll.Invocations.Count(invocation => invocation.Identifier == "disposeViewKey").ShouldBe(1);
        scroll.Invocations.Last(invocation => invocation.Identifier == "captureScrollThrottled")
            .Arguments[3].ShouldBe(replacementGeneration);
    }

    [Fact]
    public async Task TwoMountedInstancesOfOneProjectionShareStatusAndTransferItsCurrentSpeech()
    {
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        IRenderedComponent<CounterProjectionView> first = Render<CounterProjectionView>();
        IRenderedComponent<CounterProjectionView> second = Render<CounterProjectionView>();
        first.FindAll(StatusSelector).Count.ShouldBe(1);
        second.FindAll(StatusSelector).ShouldBeEmpty();

        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        announcements.Announce("projection:" + ViewKey, "external:1", "current", "Current projection state.", terminal: true);
        first.WaitForAssertion(() => first.Find(StatusSelector).TextContent.ShouldBe("Current projection state."));

        first.FindComponent<Hexalith.FrontComposer.Shell.Components.Rendering.FcSurfaceStatus>().Instance.Dispose();
        first.Dispose();
        second.WaitForAssertion(() => second.Find(StatusSelector).TextContent.ShouldBe("Current projection state."));
    }

    [Fact]
    public async Task TwoMountedViewsRetainOneCompletedResultUntilItsFinalOwnerLeaves()
    {
        FakeTimeProvider time = new();
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(time));
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        IRenderedComponent<CounterProjectionView> first = Render<CounterProjectionView>();
        IRenderedComponent<CounterProjectionView> second = Render<CounterProjectionView>();
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable speech = announcements.Subscribe("projection:" + ViewKey, message => {
            if (message.Length > 0) { spoken.Add(message); }
        });
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        await first.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadRequestedAction("shared-result", true) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        await first.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadedAction("shared-result",
            [new CounterProjection { Id = "shared-row", Count = 1 }]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        time.Advance(TimeSpan.FromMilliseconds(250));
        first.WaitForAssertion(() => first.Find(StatusSelector).TextContent.ShouldContain("loaded"));
        spoken.Count.ShouldBe(1);

        first.FindComponent<Hexalith.FrontComposer.Shell.Components.Rendering.FcSurfaceStatus>().Instance.Dispose();
        await first.Instance.DisposeAsync();
        first.Dispose();
        second.WaitForAssertion(() => second.Find(StatusSelector).TextContent.ShouldContain("loaded"));
        announcements.Current("projection:" + ViewKey).ShouldContain("loaded");
        await second.Instance.DisposeAsync();
        second.Dispose();
        announcements.Current("projection:" + ViewKey).ShouldBeEmpty();
        spoken.Count.ShouldBe(1);
    }

    [Fact]
    public async Task TwoMountedViewsRetainLoadingAndFailureUntilFinalOwnerLeaves()
    {
        FakeTimeProvider time = new();
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(time));
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        IRenderedComponent<CounterProjectionView> first = Render<CounterProjectionView>();
        IRenderedComponent<CounterProjectionView> second = Render<CounterProjectionView>();
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable speech = announcements.Subscribe("projection:" + ViewKey, message => {
            if (message.Length > 0) { spoken.Add(message); }
        });
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        await first.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadRequestedAction("shared-failure", true) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        time.Advance(TimeSpan.FromMilliseconds(250));
        first.WaitForAssertion(() => first.Find(StatusSelector).TextContent.ShouldContain("Loading"));
        spoken.Count.ShouldBe(1);

        await first.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadFailedAction("shared-failure", "This section is being updated") { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        first.WaitForAssertion(() => first.Find(StatusSelector).TextContent.ShouldBe("This section is being updated"));
        spoken.ShouldBe(["Loading counters.", "This section is being updated"]);
        first.FindComponent<Hexalith.FrontComposer.Shell.Components.Rendering.FcSurfaceStatus>().Instance.Dispose();
        await first.Instance.DisposeAsync();
        first.Dispose();
        second.WaitForAssertion(() => second.Find(StatusSelector).TextContent.ShouldBe("This section is being updated"));
        await second.Instance.DisposeAsync();
        second.Dispose();
        announcements.Current("projection:" + ViewKey).ShouldBeEmpty();
    }

    [Fact]
    public async Task BrowserFreshnessMarkerRetainsCriteriaInvalidationAndUsesActualMatchingUnchangedRead()
    {
        Services.AddFluxor(options => options.ScanAssemblies(typeof(CounterProjection).Assembly).ScanTypes(typeof(LoadedPageFeature), typeof(LoadedPageReducers)));
        Services.AddSingleton(Substitute.For<Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver>());
        await InitializeStoreAsync();
        CounterProjection row = new() { Id = "cached", Count = 1 };
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new CounterProjectionLoadedAction("seed", [row]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
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
            { OriginScopeGeneration = pageState.Value.ScopeGeneration, RequestIdentity = "replacement", RequestGeneration = 2, TotalCount = 1 }));
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
    public async Task BackgroundEmptyPageRefreshStaysSilentAndRetainsBoundedResultClaim()
    {
        await InitializeStoreAsync();
        Services.GetRequiredService<IDispatcher>().Dispatch(new CounterProjectionLoadedAction("seed",
            [new CounterProjection { Id = "seed", Count = 1 }])
            { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        GridViewSnapshot? snapshot = (GridViewSnapshot?)typeof(CounterProjectionView)
            .GetMethod("CurrentGridSnapshot", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(cut.Instance, null);
        string request = (string)typeof(CounterProjectionView)
            .GetMethod("PageRequestIdentity", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [snapshot])!;
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> delivered = [];
        using IDisposable speech = announcements.Subscribe("projection:" + ViewKey, message => {
            if (message.Length > 0) { delivered.Add(message); }
        });
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        for (long identity = 1; identity <= 4; identity++)
        {
            LoadedPageState current = Services.GetRequiredService<IState<LoadedPageState>>().Value;
            await cut.InvokeAsync(() => pages.RestoreState(current with {
                LastResultByKey = current.LastResultByKey.SetItem(ViewKey,
                    new LoadedPageResult(identity, request, 0, Failed: false, OperatorInitiated: identity == 4)),
            }));
            delivered.Count.ShouldBe(identity == 4 ? 2 : 1);
            object claims = typeof(CounterProjectionView).GetField("_resultAnnouncementGroups",
                BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(cut.Instance)!;
            claims.GetType().GetProperty("Count")!.GetValue(claims).ShouldBe(1);
        }
        delivered.ShouldAllBe(message => message == "No counters available.");
        cut.FindAll("[role='status'][aria-live='polite']").Count.ShouldBe(1);
    }

    [Fact]
    public async Task CompletedFilteredPages_AnnounceZeroAndFailureThroughOneLiveNode() {
        await InitializeStoreAsync();
        Services.GetRequiredService<IDispatcher>().Dispatch(new CounterProjectionLoadedAction(
            "seed", [new CounterProjection { Id = "present-row", Count = 1 }]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });

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
        Services.GetRequiredService<IDispatcher>().Dispatch(new CounterProjectionLoadedAction("seed", [row]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
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
        cut.Find(StatusSelector).TextContent.ShouldBe(failureCopy);
        time.Advance(TimeSpan.FromMilliseconds(1));
        cut.WaitForAssertion(() => cut.Find(StatusSelector).TextContent.ShouldBe(failureCopy));
        cut.Render();
        time.Advance(TimeSpan.FromMilliseconds(250));
        cut.Find(StatusSelector).TextContent.ShouldBe(failureCopy);
        cut.FindAll("[role='status'][aria-live='polite']").Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(ReservedFilterKeys.SearchKey)]
    [InlineData(ReservedFilterKeys.StatusKey)]
    public async Task SearchAndStatusZeroResultsUseFilteredCopyAndHideCap(string filterKey) {
        await InitializeStoreAsync();
        Services.GetRequiredService<IDispatcher>().Dispatch(new CounterProjectionLoadedAction(
            "seed", [new CounterProjection { Id = "cached", Count = 1 }]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
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
        Services.GetRequiredService<IDispatcher>().Dispatch(new CounterProjectionLoadedAction("seed", [row]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
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
        dispatcher.Dispatch(new CounterProjectionLoadedAction("seed", [row]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
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
            "seed", [new CounterProjection { Id = "cached", Count = 1 }]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
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
        Services.GetRequiredService<IDispatcher>().Dispatch(new CounterProjectionLoadedAction("seed", [row]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
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
            "seed", [row]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
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
        cut.Find(StatusSelector).TextContent.ShouldBe("This section is being updated");
        cut.FindAll("[data-testid='fc-max-items-cap-notice']").Count.ShouldBe(1);
        spoken.Count(static message => message.StartsWith("Showing the first", StringComparison.Ordinal)).ShouldBe(1);
    }

    [Fact]
    public async Task MultipleFailedOffsetsUseCurrentViewportForVisibleFailureAndRetry()
    {
        await InitializeStoreAsync();
        CounterProjection row = new() { Id = "cached", Count = 1 };
        Services.GetRequiredService<IDispatcher>().Dispatch(new CounterProjectionLoadedAction("seed", [row]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        GridViewSnapshot? snapshot = (GridViewSnapshot?)typeof(CounterProjectionView)
            .GetMethod("CurrentGridSnapshot", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(cut.Instance, null);
        string request = (string)typeof(CounterProjectionView)
            .GetMethod("PageRequestIdentity", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [snapshot])!;
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        LoadedPageState initial = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        LoadedPageResult failedA = new(2, request, 0, true, false, 500, 25) { ErrorMessage = "Data could not be loaded." };
        LoadedPageResult failedB = new(3, request, 0, true, false, 1_000, 30) { ErrorMessage = "This section is being updated" };
        await cut.InvokeAsync(() => pages.RestoreState(initial with {
            PagesByKey = initial.PagesByKey.SetItem((ViewKey, 0), [row]),
            ActiveRequestIdentityByView = initial.ActiveRequestIdentityByView.SetItem(ViewKey, request),
            LastResultByKey = initial.LastResultByKey.SetItem(ViewKey, failedB),
            ResultsByPage = initial.ResultsByPage.SetItem((ViewKey, 500), failedA).SetItem((ViewKey, 1_000), failedB),
        }));
        await cut.InvokeAsync(() => cut.Instance.HandleScrollAsync(ViewKey, 1_000d * 44d));
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-projection-failure']").TextContent
            .ShouldContain("This section is being updated"));
        LoadedPageState withFailures = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        LoadedPageResult otherSuccess = new(4, request, 1, false, false, 1_500);
        await cut.InvokeAsync(() => pages.RestoreState(withFailures with {
            LastResultByKey = withFailures.LastResultByKey.SetItem(ViewKey, otherSuccess),
            ResultsByPage = withFailures.ResultsByPage.SetItem((ViewKey, 1_500), otherSuccess),
        }));
        cut.Find("[data-testid='fc-projection-failure']").TextContent.ShouldContain("This section is being updated");
        cut.Find(StatusSelector).TextContent.ShouldBe("This section is being updated");

        LoadPageAction? retry = null;
        IActionSubscriber actions = Services.GetRequiredService<IActionSubscriber>();
        object owner = new();
        actions.SubscribeToAction<LoadPageAction>(owner, action => retry = action);
        try {
            cut.Find("[data-testid='fc-projection-failure'] fluent-button, [data-testid='fc-projection-failure'] button").Click();
            retry.ShouldNotBeNull().Skip.ShouldBe(1_000);
            retry.Take.ShouldBe(30);
            Services.GetRequiredService<IState<LoadedPageState>>().Value.ResultsByPage[(ViewKey, 500)].Failed.ShouldBeTrue();
        }
        finally {
            actions.UnsubscribeFromAllActions(owner);
        }
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
        dispatcher.Dispatch(new CounterProjectionLoadedAction("seed", [row]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        LoadedPageState initial = Services.GetRequiredService<IState<LoadedPageState>>().Value;
        pages.RestoreState(initial with { PagesByKey = initial.PagesByKey.SetItem((ViewKey, 0), [row]) });
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        var localizer = Services.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Hexalith.FrontComposer.Shell.Resources.FcShellResources>>();
        await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadFailedAction("primary-failed", localizer["SectionUpdatingText"].Value) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        cut.WaitForAssertion(() => {
            cut.FindAll("[data-testid='fc-projection-failure']").Count.ShouldBe(1);
            cut.Find("[data-testid='fc-projection-failure']").TextContent.ShouldContain(localizer["SectionUpdatingText"].Value);
            cut.Find("[data-fc-datagrid]").ShouldNotBeNull();
            cut.Find("[data-testid='fc-projection-stale-notice']").TextContent.ShouldBe(localizer["Am04Stale"].Value);
            cut.Find(StatusSelector).TextContent.ShouldBe(localizer["SectionUpdatingText"].Value);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RetryDispatcherThrowAlwaysDisposesItsTokenAndFatalFailurePropagates(bool fatal)
    {
        await InitializeStoreAsync();
        Services.GetRequiredService<IDispatcher>().Dispatch(new CounterProjectionLoadedAction("seed", [new CounterProjection { Id = "cached", Count = 1 }]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
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
        dispatcher.Dispatch(new CounterProjectionLoadedAction("seed", [row]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadFailedAction("failed-primary", "This section is being updated") { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
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
            await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadedAction(retry.CorrelationId, [row]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
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
        await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadRequestedAction("abandoned", true) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        if (failure)
        {
            await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadFailedAction("abandoned", "This section is being updated") { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
            cut.WaitForAssertion(() => cut.Find(StatusSelector).TextContent.ShouldBe("This section is being updated"));
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
        Services.GetRequiredService<IDispatcher>().Dispatch(new CounterProjectionLoadedAction("seed", [row]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
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
    public async Task CoMountedAwayAndBackResynchronizesSurvivingPageProvider()
    {
        await InitializeStoreAsync();
        CounterProjection row = new() { Id = "current-row", Count = 1 };
        Services.GetRequiredService<IDispatcher>().Dispatch(new CounterProjectionLoadedAction("seed", [row]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
        LoadedPageFeature pages = Services.GetRequiredService<LoadedPageFeature>();
        DataGridNavigationFeature navigation = Services.GetRequiredService<DataGridNavigationFeature>();
        IRenderedComponent<CounterProjectionView> survivor = Render<CounterProjectionView>();
        IRenderedComponent<CounterProjectionView> changing = Render<CounterProjectionView>();
        string originalIdentity = ProjectionRequestIdentity(survivor);
        List<LoadPageAction> requests = [];
        IActionSubscriber actions = Services.GetRequiredService<IActionSubscriber>();
        object owner = new();
        actions.SubscribeToAction<LoadPageAction>(owner, action => {
            requests.Add(action);
            LoadedPageState current = Services.GetRequiredService<IState<LoadedPageState>>().Value;
            pages.RestoreState(LoadedPageReducers.ReduceLoadPage(current, action));
            action.Completion.TrySetResult(new object[] { row });
        });
        try
        {
            await RequestGeneratedPage(survivor);
            long firstGeneration = requests.Last().RequestGeneration!.Value;
            DataGridNavigationState originalNavigation = Services.GetRequiredService<IState<DataGridNavigationState>>().Value;
            GridViewSnapshot changed = new(0, ImmutableDictionary<string, string>.Empty.Add("Id", "different"),
                null, false, null, null, DateTimeOffset.UtcNow);
            await changing.InvokeAsync(() => navigation.RestoreState(originalNavigation with {
                ViewStates = originalNavigation.ViewStates.SetItem(ViewKey, changed),
            }));
            await RequestGeneratedPage(changing);
            requests.Last().RequestGeneration.ShouldBe(firstGeneration + 1);
            await changing.InvokeAsync(() => navigation.RestoreState(originalNavigation));
            await RequestGeneratedPage(changing);
            requests.Last().RequestGeneration.ShouldBe(firstGeneration + 2);

            await RequestGeneratedPage(survivor);
            requests.Last().RequestIdentity.ShouldBe(originalIdentity);
            requests.Last().RequestGeneration.ShouldBe(firstGeneration + 2);
            LoadedPageState current = Services.GetRequiredService<IState<LoadedPageState>>().Value;
            current.ActiveRequestGenerationByView[ViewKey].ShouldBe(firstGeneration + 2);
            requests.Last().Completion.Task.IsCompletedSuccessfully.ShouldBeTrue();
            LoadedPageReducers.ReduceLoadPage(current, requests[0] with {
                Completion = new TaskCompletionSource<object>(),
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
        Services.GetRequiredService<IDispatcher>().Dispatch(new CounterProjectionLoadedAction("seed", [row]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
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
        await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadRequestedAction("owned-primary", true) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
        await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadedAction("owned-primary", empty && !pageResult ? [] : [row]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
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
        dispatcher.Dispatch(new CounterProjectionLoadRequestedAction("first", true) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
        dispatcher.Dispatch(new CounterProjectionLoadedAction("first", [new CounterProjection { Id = "cached", Count = 1 }]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable speech = announcements.Subscribe("projection:" + ViewKey, spoken.Add);
        await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadRequestedAction("replacement", true) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" }));
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
        dispatcher.Dispatch(new CounterProjectionLoadedAction("seed", [row]) { ScopeTenantId = "test-tenant", ScopeUserId = "test-user" });
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
