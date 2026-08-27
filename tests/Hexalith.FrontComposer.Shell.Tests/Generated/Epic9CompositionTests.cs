using System.Collections.Immutable;

using Bunit;

using Counter.Domain;
using Counter.Web;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>
/// Proves the Epic 9 generated-command-to-already-rendered-grid composition boundary.
/// </summary>
public sealed class Epic9CompositionTests : GeneratedComponentTestBase
{
    private const string ViewKey = "Counter:Counter.Domain.CounterProjection";
    private const string IndicatorSelector = "[data-testid=\"fc-new-item-indicator\"]";
    private static readonly DateTimeOffset s_now = new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);
    private readonly Epic9UserContextAccessor _userContext = new();

    /// <summary>Initializes the generated command, projection, and sample-effect scan.</summary>
    public Epic9CompositionTests()
        : base(
            typeof(CounterProjection).Assembly,
            typeof(CrossRowProviderTargetCommand).Assembly,
            typeof(CounterProjectionEffects).Assembly)
    {
    }

    [Fact]
    public async Task GeneratedCommands_CallbackAndPolling_ReachAlreadyRenderedGridWithExpectedDisposition()
    {
        FakeTimeProvider time = ConfigureFakeTime();
        Epic9ScriptedCommandService commands = ConfigureCommandService(time);
        ConfigureProviders();
        commands.Enqueue<CreateCounterCommand>("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        commands.Enqueue<SameSourceTargetCommand>("01BRZ3NDEKTSV4RRFFQ69G5FAV", emitTerminal: false);
        commands.Enqueue<CrossRowProviderTargetCommand>("01CRZ3NDEKTSV4RRFFQ69G5FAV");
        commands.Enqueue<StatusMoveProviderTargetCommand>("01DRZ3NDEKTSV4RRFFQ69G5FAV");
        commands.Enqueue<DeleteProviderTargetCommand>("01ERZ3NDEKTSV4RRFFQ69G5FAV");

        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        INewItemIndicatorStateService indicators = Services.GetRequiredService<INewItemIndicatorStateService>();
        dispatcher.Dispatch(new CounterProjectionLoadedAction(
            "epic-9-initial-grid",
            [
                Counter("counter-source", 1),
                Counter("counter-cross-destination", 2),
                Counter("counter-status-destination", 3),
            ]));
        IRenderedComponent<CounterProjectionView> grid = RenderGrid();
        await grid.WaitForAssertionAsync(() => grid.FindAll(IndicatorSelector).ShouldBeEmpty());

        const string CreatedKey = "counter-created-during-test";
        IRenderedComponent<CreateCounterCommandForm> create = Render<CreateCounterCommandForm>(parameters => parameters
            .Add(component => component.InitialValue, new CreateCounterCommand
            {
                CounterId = CreatedKey,
                InitialValue = 7,
            }));
        create.Find("form").Submit();

        await grid.WaitForAssertionAsync(() =>
        {
            PendingCommandEntry entry = pending.GetByMessageId("01ARZ3NDEKTSV4RRFFQ69G5FAV").ShouldNotBeNull();
            entry.Status.ShouldBe(PendingCommandStatus.Confirmed);
            entry.TargetSnapshot.ShouldNotBeNull().EntityKey.ShouldBe(CreatedKey);
            AngleSharp.Dom.IElement indicator = grid.Find(IndicatorSelector);
            indicator.GetAttribute("role").ShouldBe("status");
            indicator.GetAttribute("aria-live").ShouldBe("polite");
            indicator.GetAttribute("aria-label").ShouldBe("New item added outside current filters");
            indicator.TextContent.Trim().ShouldBe("New item. It may not match current filters yet.");
        });
        CreateCounterCommand dispatchedCreate = commands.DispatchedCommands
            .OfType<CreateCounterCommand>()
            .Single();
        dispatchedCreate.CounterId.ShouldBe(CreatedKey);

        await grid.WaitForAssertionAsync(() =>
        {
            Services.GetRequiredService<IState<CounterProjectionState>>()
                .Value.Items.ShouldNotBeNull().ShouldContain(row => row.Id == CreatedKey && row.Count == 7);
            grid.FindAll(IndicatorSelector).ShouldBeEmpty();
            indicators.Snapshot(ViewKey).ShouldBeEmpty();
        }, timeout: TimeSpan.FromSeconds(3));

        PendingCommandRowIdentity source = new(
            typeof(CounterProjection).FullName!,
            ViewKey,
            "counter-source",
            expectedStatusSlot: "Approved",
            priorStatusSlot: "Draft");
        IRenderedComponent<CascadingValue<PendingCommandRowIdentity?>> sameSource =
            Render<CascadingValue<PendingCommandRowIdentity?>>(parameters => parameters
                .Add(component => component.Value, source)
                .Add(component => component.IsFixed, true)
                .AddChildContent<SameSourceTargetCommandForm>());
        sameSource.Find("form").Submit();
        await sameSource.WaitForAssertionAsync(() =>
            pending.GetByMessageId("01BRZ3NDEKTSV4RRFFQ69G5FAV").ShouldNotBeNull().Status
                .ShouldBe(PendingCommandStatus.Pending));

        IPendingCommandStatusQuery statusQuery = new Epic9PendingCommandStatusQuery(new(
                PendingCommandOutcomeSource.FallbackPolling,
                PendingCommandTerminalOutcome.Confirmed,
                MessageId: "01BRZ3NDEKTSV4RRFFQ69G5FAV",
                ObservedAt: time.GetUtcNow())
        {
            Materiality = CommandMateriality.Material,
        });
        PendingCommandPollingCoordinator polling = new(
            pending,
            Services.GetRequiredService<IPendingCommandOutcomeResolver>(),
            statusQuery,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            timeProvider: time);
        _ = await polling.PollOnceAsync(Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        await grid.WaitForAssertionAsync(() =>
        {
            pending.GetByMessageId("01BRZ3NDEKTSV4RRFFQ69G5FAV").ShouldNotBeNull().Status
                .ShouldBe(PendingCommandStatus.Confirmed);
            indicators.Snapshot(ViewKey).Single().EntityKey.ShouldBe("counter-source");
            grid.FindAll(IndicatorSelector).Count.ShouldBe(1);
        });

        ApplyFilterRequery(time, "counter-source");
        await grid.WaitForAssertionAsync(() => grid.FindAll(IndicatorSelector).ShouldBeEmpty());

        Render<CrossRowProviderTargetCommandForm>(parameters => parameters
                .Add(component => component.InitialValue, new CrossRowProviderTargetCommand
                {
                    DestinationId = "counter-cross-destination",
                }))
            .Find("form")
            .Submit();
        await grid.WaitForAssertionAsync(() =>
        {
            PendingCommandEntry entry = pending.GetByMessageId("01CRZ3NDEKTSV4RRFFQ69G5FAV").ShouldNotBeNull();
            entry.TargetSnapshot.ShouldNotBeNull().EntityKey.ShouldBe("counter-cross-destination");
            indicators.Snapshot(ViewKey).Single().EntityKey.ShouldBe("counter-cross-destination");
        });

        indicators.Clear("epic-9-status-move");
        await grid.WaitForAssertionAsync(() => grid.FindAll(IndicatorSelector).ShouldBeEmpty());
        Render<StatusMoveProviderTargetCommandForm>().Find("form").Submit();
        await grid.WaitForAssertionAsync(() =>
        {
            CommandTargetSnapshot target = pending.GetByMessageId("01DRZ3NDEKTSV4RRFFQ69G5FAV")
                .ShouldNotBeNull()
                .TargetSnapshot
                .ShouldNotBeNull();
            target.EntityKey.ShouldBe("counter-status-destination");
            target.PriorStatus.ShouldBe("Draft");
            target.ExpectedStatus.ShouldBe("Approved");
            indicators.Snapshot(ViewKey).Single().EntityKey.ShouldBe("counter-status-destination");
        });

        indicators.Clear("epic-9-delete");
        await grid.WaitForAssertionAsync(() => grid.FindAll(IndicatorSelector).ShouldBeEmpty());
        IRenderedComponent<DeleteProviderTargetCommandForm> delete = Render<DeleteProviderTargetCommandForm>();
        delete.Find("form").Submit();
        await delete.WaitForAssertionAsync(() =>
        {
            PendingCommandEntry entry = pending.GetByMessageId("01ERZ3NDEKTSV4RRFFQ69G5FAV").ShouldNotBeNull();
            entry.Status.ShouldBe(PendingCommandStatus.Confirmed);
            entry.TargetSnapshot.ShouldNotBeNull().ChangeKind.ShouldBe(CommandTargetChangeKind.Delete);
            indicators.Snapshot(ViewKey).ShouldBeEmpty();
            grid.FindAll(IndicatorSelector).ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task AlreadyRenderedGrid_FirstWinsAndEveryInvalidation_RerendersAutomatically()
    {
        FakeTimeProvider time = ConfigureFakeTime();
        Epic9ScriptedCommandService commands = ConfigureCommandService(time);
        IProjectionFallbackRefreshScheduler refreshScheduler = ConfigureRefreshScheduler();
        ConfigureProviders();
        foreach (string messageId in new[]
        {
            "01FRZ3NDEKTSV4RRFFQ69G5FAV",
            "01GRZ3NDEKTSV4RRFFQ69G5FAV",
            "01HRZ3NDEKTSV4RRFFQ69G5FAV",
            "01JRZ3NDEKTSV4RRFFQ69G5FAV",
            "01KRZ3NDEKTSV4RRFFQ69G5FAV",
            "01MRZ3NDEKTSV4RRFFQ69G5FAV",
            "01NRZ3NDEKTSV4RRFFQ69G5FAV",
        })
        {
            commands.Enqueue<CrossRowProviderTargetCommand>(messageId);
        }

        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        INewItemIndicatorStateService indicators = Services.GetRequiredService<INewItemIndicatorStateService>();
        dispatcher.Dispatch(new CounterProjectionLoadedAction("epic-9-grid", [Counter("counter-existing", 1)]));
        IRenderedComponent<CounterProjectionView> grid = RenderGrid();
        await grid.WaitForAssertionAsync(() => grid.FindAll(IndicatorSelector).ShouldBeEmpty());

        SubmitCrossRow("counter-first-wins");
        await grid.WaitForAssertionAsync(() => grid.FindAll(IndicatorSelector).Count.ShouldBe(1));
        NewItemIndicatorEntry first = indicators.Snapshot(ViewKey).Single();
        SubmitCrossRow("counter-first-wins");
        await grid.WaitForAssertionAsync(() =>
        {
            NewItemIndicatorEntry retained = indicators.Snapshot(ViewKey).Single();
            retained.MessageId.ShouldBe(first.MessageId);
            retained.CreatedAt.ShouldBe(first.CreatedAt);
            grid.FindAll(IndicatorSelector).Count.ShouldBe(1);
        });

        time.Advance(TimeSpan.FromSeconds(10));
        await grid.WaitForAssertionAsync(() => grid.FindAll(IndicatorSelector).ShouldBeEmpty());

        SubmitCrossRow("counter-filter");
        await grid.WaitForAssertionAsync(() => grid.FindAll(IndicatorSelector).Count.ShouldBe(1));
        ApplyFilterRequery(time, "counter-filter");
        await grid.WaitForAssertionAsync(() =>
        {
            _ = refreshScheduler.Received().RegisterLane(Arg.Is<ProjectionFallbackLane>(lane =>
                lane.Filters.ContainsKey("Id") && lane.Filters["Id"] == "counter-filter"));
            grid.FindAll(IndicatorSelector).ShouldBeEmpty();
        });

        SubmitCrossRow("counter-clear");
        await grid.WaitForAssertionAsync(() => grid.FindAll(IndicatorSelector).Count.ShouldBe(1));
        indicators.Clear("epic-9-explicit-clear");
        await grid.WaitForAssertionAsync(() => grid.FindAll(IndicatorSelector).ShouldBeEmpty());

        SubmitCrossRow("counter-tenant-scope");
        await grid.WaitForAssertionAsync(() => grid.FindAll(IndicatorSelector).Count.ShouldBe(1));
        _userContext.TenantId = "other-tenant";
        indicators.Snapshot(ViewKey).ShouldBeEmpty();
        await grid.WaitForAssertionAsync(() => grid.FindAll(IndicatorSelector).ShouldBeEmpty());
        _userContext.TenantId = "test-tenant";
        indicators.Snapshot(ViewKey).ShouldBeEmpty();

        SubmitCrossRow("counter-user-scope");
        await grid.WaitForAssertionAsync(() => grid.FindAll(IndicatorSelector).Count.ShouldBe(1));
        _userContext.UserId = "other-user";
        indicators.Snapshot(ViewKey).ShouldBeEmpty();
        await grid.WaitForAssertionAsync(() => grid.FindAll(IndicatorSelector).ShouldBeEmpty());
        _userContext.UserId = "test-user";
        indicators.Snapshot(ViewKey).ShouldBeEmpty();

        SubmitCrossRow("counter-materialized");
        await grid.WaitForAssertionAsync(() => grid.FindAll(IndicatorSelector).Count.ShouldBe(1));
        await grid.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadedAction(
            "epic-9-materialized",
            [Counter("counter-existing", 1), Counter("counter-materialized", 2)])));
        await grid.WaitForAssertionAsync(() => grid.FindAll(IndicatorSelector).ShouldBeEmpty());
    }

    private FakeTimeProvider ConfigureFakeTime()
    {
        FakeTimeProvider time = new(s_now);
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(time));
        Services.Replace(ServiceDescriptor.Singleton<IUserContextAccessor>(_userContext));
        return time;
    }

    private Epic9ScriptedCommandService ConfigureCommandService(TimeProvider time)
    {
        Epic9ScriptedCommandService commands = new(time);
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => commands));
        return commands;
    }

    private IProjectionFallbackRefreshScheduler ConfigureRefreshScheduler()
    {
        IProjectionFallbackRefreshScheduler scheduler = Substitute.For<IProjectionFallbackRefreshScheduler>();
        scheduler.RegisterLane(Arg.Any<ProjectionFallbackLane>()).Returns(_ => Substitute.For<IDisposable>());
        Services.Replace(ServiceDescriptor.Scoped(_ => scheduler));
        return scheduler;
    }

    private void ConfigureProviders()
    {
        Services.AddScoped<ICommandTargetIdentityProvider<CreateCounterCommand>, CreateCounterTargetIdentityProvider>();
        Services.AddScoped<ICommandTargetIdentityProvider<UpdateCounterCommand>, UpdateCounterTargetIdentityProvider>();
        Services.AddScoped<
            ICommandTargetIdentityProvider<CrossRowProviderTargetCommand>,
            Epic9CrossRowTargetIdentityProvider>();
        Services.AddScoped<
            ICommandTargetIdentityProvider<StatusMoveProviderTargetCommand>,
            Epic9StatusMoveTargetIdentityProvider>();
        Services.AddScoped<
            ICommandTargetIdentityProvider<DeleteProviderTargetCommand>,
            Epic9DeleteTargetIdentityProvider>();
    }

    private void SubmitCrossRow(string destinationId)
    {
        Render<CrossRowProviderTargetCommandForm>(parameters => parameters
                .Add(component => component.InitialValue, new CrossRowProviderTargetCommand
                {
                    DestinationId = destinationId,
                }))
            .Find("form")
            .Submit();
    }

    private IRenderedComponent<CounterProjectionView> RenderGrid()
    {
        RenderContext context = new(
            "test-tenant",
            "test-user",
            FcRenderMode.Server,
            DensityLevel.Comfortable,
            IsReadOnly: false);
        IRenderedComponent<CascadingValue<RenderContext>> host = Render<CascadingValue<RenderContext>>(parameters => parameters
            .Add(component => component.Value, context)
            .Add(component => component.IsFixed, true)
            .AddChildContent<CounterProjectionView>());
        return host.FindComponent<CounterProjectionView>();
    }

    private void ApplyFilterRequery(TimeProvider time, string filterValue)
    {
        DataGridNavigationState current = Services.GetRequiredService<IState<DataGridNavigationState>>().Value;
        CaptureGridStateAction action = new(
            ViewKey,
            new GridViewSnapshot(
                scrollTop: 0,
                filters: ImmutableDictionary<string, string>.Empty.Add("Id", filterValue),
                sortColumn: null,
                sortDescending: false,
                expandedRowId: null,
                selectedRowId: null,
                capturedAt: time.GetUtcNow()));
        Services.GetRequiredService<DataGridNavigationFeature>()
            .RestoreState(DataGridNavigationReducers.ReduceCapture(current, action));
    }

    private static CounterProjection Counter(string id, int count) => new()
    {
        Id = id,
        Count = count,
        LastUpdated = s_now,
    };
}
