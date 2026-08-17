using System.Diagnostics;
using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Lifecycle;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

public sealed class CommandTargetGeneratedFormTests : CommandRendererTestBase {
    private const string AcceptedMessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string WrongMessageId = "01BRZ3NDEKTSV4RRFFQ69G5FAV";
    private const string SecondWrongMessageId = "01CRZ3NDEKTSV4RRFFQ69G5FAV";
    private const string ThirdWrongMessageId = "01DRZ3NDEKTSV4RRFFQ69G5FAV";
    private IgnoringCancellationProvider? _ignoringCancellationProvider;

    [Fact]
    public void LifecycleBridge_LaterConstructorSubscriptionFailureUnsubscribesEarlierActions() {
        IActionSubscriber subscriber = Substitute.For<IActionSubscriber>();
        subscriber.When(instance => instance.SubscribeToAction<SameSourceTargetCommandActions.AcknowledgedAction>(
                Arg.Any<object>(),
                Arg.Any<Action<SameSourceTargetCommandActions.AcknowledgedAction>>()))
            .Do(_ => throw new InvalidOperationException("later subscription failed"));

        _ = Should.Throw<InvalidOperationException>(() => new SameSourceTargetCommandLifecycleBridge(
            subscriber,
            Substitute.For<IDispatcher>(),
            Substitute.For<ILifecycleStateService>()));

        subscriber.Received(1).UnsubscribeFromAllActions(Arg.Any<SameSourceTargetCommandLifecycleBridge>());
    }

    [Fact]
    public void LifecycleBridge_SynchronousTerminalReplayDispatchesAndDisposesReturnedSubscription() {
        const string correlationId = "01ERZ3NDEKTSV4RRFFQ69G5FAV";
        IActionSubscriber subscriber = Substitute.For<IActionSubscriber>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        SynchronousTerminalReplayLifecycleService lifecycle = new(correlationId);
        using SameSourceTargetCommandLifecycleBridge bridge = new(subscriber, dispatcher, lifecycle);

        typeof(SameSourceTargetCommandLifecycleBridge)
            .GetMethod("EnsureLifecycleSubscription", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .ShouldNotBeNull()
            .Invoke(bridge, [correlationId]);

        dispatcher.Received(1).Dispatch(Arg.Is<SameSourceTargetCommandActions.ConfirmedAction>(
            action => action != null && action.CorrelationId == correlationId));
        lifecycle.SubscriptionDisposed.ShouldBeTrue();
    }

    [Fact]
    public async Task LifecycleBridge_ConcurrentDifferentTerminalIsNotSuppressedByForwardedAcknowledgement() {
        const string correlationId = "01ERZ3NDEKTSV4RRFFQ69G5FAV";
        LifecycleStateService lifecycle = new(Microsoft.Extensions.Options.Options.Create(new LifecycleOptions()));
        IActionSubscriber subscriber = Substitute.For<IActionSubscriber>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        using ManualResetEventSlim acknowledgementEntered = new(initialState: false);
        using ManualResetEventSlim releaseAcknowledgement = new(initialState: false);
        using IDisposable blocker = lifecycle.Subscribe(correlationId, transition => {
            if (transition.NewState == CommandLifecycleState.Acknowledged) {
                acknowledgementEntered.Set();
                releaseAcknowledgement.Wait(Xunit.TestContext.Current.CancellationToken);
            }
        });
        using SameSourceTargetCommandLifecycleBridge bridge = new(subscriber, dispatcher, lifecycle);
        typeof(SameSourceTargetCommandLifecycleBridge)
            .GetMethod("EnsureLifecycleSubscription", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .ShouldNotBeNull()
            .Invoke(bridge, [correlationId]);
        lifecycle.Transition(correlationId, CommandLifecycleState.Submitting);

        Task forwardedAcknowledgement = Task.Run(() =>
            typeof(SameSourceTargetCommandLifecycleBridge)
                .GetMethod("ForwardAction", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .ShouldNotBeNull()
                .Invoke(bridge, [correlationId, CommandLifecycleState.Acknowledged, AcceptedMessageId]));
        acknowledgementEntered.Wait(Xunit.TestContext.Current.CancellationToken);
        try {
            lifecycle.Transition(correlationId, CommandLifecycleState.Confirmed, AcceptedMessageId);

            dispatcher.Received(1).Dispatch(Arg.Is<SameSourceTargetCommandActions.ConfirmedAction>(
                action => action != null && action.CorrelationId == correlationId));
        }
        finally {
            releaseAcknowledgement.Set();
            await forwardedAcknowledgement.ConfigureAwait(true);
            await lifecycle.DisposeAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public void LifecycleBridge_ThrowingStaleSubscriptionContinuesSweepAndNewSubmission() {
        const string firstStale = "01ERZ3NDEKTSV4RRFFQ69G5FAV";
        const string secondStale = "01FRZ3NDEKTSV4RRFFQ69G5FAV";
        const string current = "01GRZ3NDEKTSV4RRFFQ69G5FAV";
        IActionSubscriber subscriber = Substitute.For<IActionSubscriber>();
        Action<SameSourceTargetCommandActions.SubmittedAction>? onSubmitted = null;
        subscriber.When(instance => instance.SubscribeToAction<SameSourceTargetCommandActions.SubmittedAction>(
                Arg.Any<object>(),
                Arg.Any<Action<SameSourceTargetCommandActions.SubmittedAction>>()))
            .Do(call => onSubmitted = call.ArgAt<Action<SameSourceTargetCommandActions.SubmittedAction>>(1));
        ThrowingStaleSubscriptionLifecycleService lifecycle = new(firstStale);
        using SameSourceTargetCommandLifecycleBridge bridge = new(
            subscriber,
            Substitute.For<IDispatcher>(),
            lifecycle);
        System.Reflection.MethodInfo ensure = typeof(SameSourceTargetCommandLifecycleBridge)
            .GetMethod("EnsureLifecycleSubscription", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .ShouldNotBeNull();
        _ = ensure.Invoke(bridge, [firstStale]);
        _ = ensure.Invoke(bridge, [secondStale]);

        onSubmitted.ShouldNotBeNull().Invoke(new(
            current,
            new SameSourceTargetCommand()));

        lifecycle.Disposed.ShouldContain(firstStale);
        lifecycle.Disposed.ShouldContain(secondStale);
        lifecycle.Subscribed.ShouldContain(current);
        lifecycle.Transitions.ShouldContain((current, CommandLifecycleState.Submitting));
    }

    [Fact]
    public void LifecycleBridge_FatalStaleSubscriptionFailurePropagates() {
        const string stale = "01ERZ3NDEKTSV4RRFFQ69G5FAV";
        IActionSubscriber subscriber = Substitute.For<IActionSubscriber>();
        Action<SameSourceTargetCommandActions.SubmittedAction>? onSubmitted = null;
        subscriber.When(instance => instance.SubscribeToAction<SameSourceTargetCommandActions.SubmittedAction>(
                Arg.Any<object>(),
                Arg.Any<Action<SameSourceTargetCommandActions.SubmittedAction>>()))
            .Do(call => onSubmitted = call.ArgAt<Action<SameSourceTargetCommandActions.SubmittedAction>>(1));
        ThrowingStaleSubscriptionLifecycleService lifecycle = new(stale, fatal: true);
        using SameSourceTargetCommandLifecycleBridge bridge = new(
            subscriber,
            Substitute.For<IDispatcher>(),
            lifecycle);
        _ = typeof(SameSourceTargetCommandLifecycleBridge)
            .GetMethod("EnsureLifecycleSubscription", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .ShouldNotBeNull()
            .Invoke(bridge, [stale]);

        _ = Should.Throw<OutOfMemoryException>(() => onSubmitted.ShouldNotBeNull().Invoke(new(
            "01GRZ3NDEKTSV4RRFFQ69G5FAV",
            new SameSourceTargetCommand())));
    }

    [Fact]
    public async Task SameAsSourceTarget_DispatchesAssociatesSnapshotAndPublishesEligibleTerminal() {
        EarlyTerminalCommandService service = new(AcceptedMessageId);
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        INewItemIndicatorStateService indicators = Services.GetRequiredService<INewItemIndicatorStateService>();
        PendingCommandRowIdentity row = new(
            typeof(Counter.Domain.CounterProjection).FullName!,
            "Counter:Counter.Domain.CounterProjection",
            "counter-42",
            expectedStatusSlot: "Approved",
            priorStatusSlot: "Draft");

        IRenderedComponent<CascadingValue<PendingCommandRowIdentity?>> host =
            Render<CascadingValue<PendingCommandRowIdentity?>>(parameters => parameters
                .Add(component => component.Value, row)
                .Add(component => component.IsFixed, true)
                .AddChildContent<SameSourceTargetCommandForm>());
        host.Find("form").Submit();

        host.WaitForAssertion(() => {
            service.DispatchCount.ShouldBe(1);
            PendingCommandEntry entry = pending.GetByMessageId(AcceptedMessageId).ShouldNotBeNull();
            entry.Status.ShouldBe(PendingCommandStatus.Confirmed);
            CommandTargetSnapshot target = entry.TargetSnapshot.ShouldNotBeNull();
            target.ProjectionTypeName.ShouldBe(typeof(Counter.Domain.CounterProjection).FullName);
            target.ViewKey.ShouldBe("Counter:Counter.Domain.CounterProjection");
            target.EntityKey.ShouldBe("counter-42");
            target.PriorStatus.ShouldBe("Draft");
            target.ExpectedStatus.ShouldBe("Approved");
            target.TenantId.ShouldBe("counter-demo");
            target.UserId.ShouldBe("demo-user");
            entry.ProjectionTypeName.ShouldBe(target.ProjectionTypeName);
            entry.LaneKey.ShouldBe(target.ViewKey);
            entry.EntityKey.ShouldBe(target.EntityKey);
            entry.ExpectedStatusSlot.ShouldBe(target.ExpectedStatus);
            entry.PriorStatusSlot.ShouldBe(target.PriorStatus);
            indicators.Snapshot("Counter:Counter.Domain.CounterProjection").Single().EntityKey.ShouldBe("counter-42");
        });
    }

    [Fact]
    public async Task PollingTerminal_ConvergesResolverLifecycleIntoGeneratedFluxorState() {
        EarlyTerminalCommandService service = new(AcceptedMessageId, emitTerminal: false);
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        IPendingCommandOutcomeResolver resolver = Services.GetRequiredService<IPendingCommandOutcomeResolver>();
        IState<SameSourceTargetCommandLifecycleState> state =
            Services.GetRequiredService<IState<SameSourceTargetCommandLifecycleState>>();
        IActionSubscriber actionSubscriber = Services.GetRequiredService<IActionSubscriber>();
        object actionOwner = new();
        int confirmedActions = 0;
        actionSubscriber.SubscribeToAction<SameSourceTargetCommandActions.ConfirmedAction>(
            actionOwner,
            _ => Interlocked.Increment(ref confirmedActions));
        PendingCommandPollingCoordinator polling = new(
            pending,
            resolver,
            new FixedPendingCommandStatusQuery(new PendingCommandOutcomeObservation(
                PendingCommandOutcomeSource.FallbackPolling,
                PendingCommandTerminalOutcome.Confirmed,
                MessageId: AcceptedMessageId,
                ObservedAt: TimeProvider.System.GetUtcNow()) {
                Materiality = CommandMateriality.Material,
            }),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            timeProvider: Services.GetRequiredService<TimeProvider>());
        PendingCommandRowIdentity row = new(
            typeof(Counter.Domain.CounterProjection).FullName!,
            "Counter:Counter.Domain.CounterProjection",
            "counter-42");
        IRenderedComponent<CascadingValue<PendingCommandRowIdentity?>> host =
            Render<CascadingValue<PendingCommandRowIdentity?>>(parameters => parameters
                .Add(component => component.Value, row)
                .Add(component => component.IsFixed, true)
                .AddChildContent<SameSourceTargetCommandForm>());

        try {
            host.Find("form").Submit();
            host.WaitForAssertion(() => {
                pending.GetByMessageId(AcceptedMessageId).ShouldNotBeNull().Status
                    .ShouldBe(PendingCommandStatus.Pending);
                state.Value.State.ShouldBe(CommandLifecycleState.Acknowledged);
            });

            (await polling.PollOnceAsync(Xunit.TestContext.Current.CancellationToken)).ShouldBe(1);

            host.WaitForAssertion(() => {
                pending.GetByMessageId(AcceptedMessageId).ShouldNotBeNull().Status
                    .ShouldBe(PendingCommandStatus.Confirmed);
                state.Value.State.ShouldBe(CommandLifecycleState.Confirmed);
                Volatile.Read(ref confirmedActions).ShouldBe(1);
            });
        }
        finally {
            actionSubscriber.UnsubscribeFromAllActions(actionOwner);
        }
    }

    [Fact]
    public async Task SameAsSourceTarget_WithoutSource_DispatchesWithNullTargetAndNoIndicator() {
        EarlyTerminalCommandService service = new(AcceptedMessageId);
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        INewItemIndicatorStateService indicators = Services.GetRequiredService<INewItemIndicatorStateService>();
        IRenderedComponent<SameSourceTargetCommandForm> cut = Render<SameSourceTargetCommandForm>();

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => {
            service.DispatchCount.ShouldBe(1);
            PendingCommandEntry entry = pending.GetByMessageId(AcceptedMessageId).ShouldNotBeNull();
            entry.Status.ShouldBe(PendingCommandStatus.Confirmed);
            entry.TargetSnapshot.ShouldBeNull();
            entry.ProjectionTypeName.ShouldBeNull();
            entry.LaneKey.ShouldBeNull();
            entry.EntityKey.ShouldBeNull();
            entry.ExpectedStatusSlot.ShouldBeNull();
            entry.PriorStatusSlot.ShouldBeNull();
            indicators.Snapshot("counter-counts").ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task SameAsSourceTarget_WithMissingFixedExpectedStatusFailsClosed() {
        EarlyTerminalCommandService service = new(AcceptedMessageId);
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        PendingCommandRowIdentity row = new(
            typeof(Counter.Domain.CounterProjection).FullName!,
            "Counter:Counter.Domain.CounterProjection",
            "counter-42",
            expectedStatusSlot: null,
            priorStatusSlot: "Draft");
        IRenderedComponent<CascadingValue<PendingCommandRowIdentity?>> host =
            Render<CascadingValue<PendingCommandRowIdentity?>>(parameters => parameters
                .Add(component => component.Value, row)
                .Add(component => component.IsFixed, true)
                .AddChildContent<SameSourceTargetCommandForm>());

        host.Find("form").Submit();

        host.WaitForAssertion(() =>
            pending.GetByMessageId(AcceptedMessageId).ShouldNotBeNull().TargetSnapshot.ShouldBeNull());
    }

    [Fact]
    public async Task SynchronousSyncingBeforeAcceptedAcknowledgement_RemainsSyncing() {
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => new SynchronousSyncingCommandService()));
        await InitializeStoreAsync();
        IState<TwoFieldCompactCommandLifecycleState> state =
            Services.GetRequiredService<IState<TwoFieldCompactCommandLifecycleState>>();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>();

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => state.Value.State.ShouldBe(CommandLifecycleState.Syncing));
    }

    [Fact]
    public async Task SameAsSourceTarget_WithWrongProjection_DispatchesWithNullTargetAndNoIndicator() {
        EarlyTerminalCommandService service = new(AcceptedMessageId);
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        INewItemIndicatorStateService indicators = Services.GetRequiredService<INewItemIndicatorStateService>();
        PendingCommandRowIdentity wrongProjection = new(
            "Other.Projection",
            "counter-counts",
            "counter-42",
            expectedStatusSlot: "Approved",
            priorStatusSlot: "Draft");
        IRenderedComponent<CascadingValue<PendingCommandRowIdentity?>> host =
            Render<CascadingValue<PendingCommandRowIdentity?>>(parameters => parameters
                .Add(component => component.Value, wrongProjection)
                .Add(component => component.IsFixed, true)
                .AddChildContent<SameSourceTargetCommandForm>());

        host.Find("form").Submit();

        host.WaitForAssertion(() => {
            service.DispatchCount.ShouldBe(1);
            PendingCommandEntry entry = pending.GetByMessageId(AcceptedMessageId).ShouldNotBeNull();
            entry.Status.ShouldBe(PendingCommandStatus.Confirmed);
            entry.TargetSnapshot.ShouldBeNull();
            indicators.Snapshot("counter-counts").ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task OneProviderTarget_DispatchesAssociatesSnapshotAndPublishesEligibleTerminal() {
        EarlyTerminalCommandService service = new(AcceptedMessageId);
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        Services.AddScoped<ICommandTargetIdentityProvider<ProviderTargetCommand>>(
            _ => new SuccessfulProvider());
        await InitializeStoreAsync();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        INewItemIndicatorStateService indicators = Services.GetRequiredService<INewItemIndicatorStateService>();
        IRenderedComponent<ProviderTargetCommandForm> cut = Render<ProviderTargetCommandForm>();

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => {
            service.DispatchCount.ShouldBe(1);
            PendingCommandEntry entry = pending.GetByMessageId(AcceptedMessageId).ShouldNotBeNull();
            entry.Status.ShouldBe(PendingCommandStatus.Confirmed);
            CommandTargetSnapshot target = entry.TargetSnapshot.ShouldNotBeNull();
            target.ViewKey.ShouldBe("Counter:Counter.Domain.CounterProjection");
            target.EntityKey.ShouldBe("counter-provider");
            indicators.Snapshot("Counter:Counter.Domain.CounterProjection").Single().EntityKey.ShouldBe("counter-provider");
        });
    }

    [Fact]
    public async Task DeleteProviderTarget_AssociatesDeleteSnapshotAndNeverPublishesIndicator() {
        EarlyTerminalCommandService service = new(AcceptedMessageId);
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        Services.AddScoped<ICommandTargetIdentityProvider<DeleteProviderTargetCommand>>(
            _ => new SuccessfulDeleteProvider());
        await InitializeStoreAsync();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        INewItemIndicatorStateService indicators = Services.GetRequiredService<INewItemIndicatorStateService>();
        IRenderedComponent<DeleteProviderTargetCommandForm> cut = Render<DeleteProviderTargetCommandForm>();

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => {
            service.DispatchCount.ShouldBe(1);
            PendingCommandEntry entry = pending.GetByMessageId(AcceptedMessageId).ShouldNotBeNull();
            entry.Status.ShouldBe(PendingCommandStatus.Confirmed);
            CommandTargetSnapshot target = entry.TargetSnapshot.ShouldNotBeNull();
            target.ChangeKind.ShouldBe(CommandTargetChangeKind.Delete);
            target.ViewKey.ShouldBe("Counter:Counter.Domain.CounterProjection");
            target.EntityKey.ShouldBe("counter-provider-delete");
            indicators.Snapshot("Counter:Counter.Domain.CounterProjection").ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task SynchronouslyBlockingProvider_DeadlineStillAllowsDispatch() {
        EarlyTerminalCommandService service = new(AcceptedMessageId, emitTerminal: false);
        SynchronouslyBlockingProvider provider = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        Services.AddScoped<ICommandTargetIdentityProvider<ProviderTargetCommand>>(_ => provider);
        Services.Configure<FcShellOptions>(options => options.CommandTargetResolutionTimeoutMs = 25);
        await InitializeStoreAsync();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        IRenderedComponent<ProviderTargetCommandForm> cut = Render<ProviderTargetCommandForm>();

        cut.Find("form").Submit();
        await provider.Started.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            Xunit.TestContext.Current.CancellationToken);

        try {
            cut.WaitForAssertion(() => {
                service.DispatchCount.ShouldBe(1);
                PendingCommandEntry entry = pending.GetByMessageId(AcceptedMessageId).ShouldNotBeNull();
                entry.TargetSnapshot.ShouldBeNull();
            }, timeout: TimeSpan.FromSeconds(2));
        }
        finally {
            provider.Release();
        }
    }

    [Fact]
    public async Task ProviderRegistration_IsConstructedLazilyInsideSubmitBoundary() {
        int constructions = 0;
        EarlyTerminalCommandService service = new(AcceptedMessageId, emitTerminal: false);
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        Services.AddScoped<ICommandTargetIdentityProvider<ProviderTargetCommand>>(_ => {
            Interlocked.Increment(ref constructions);
            return new SuccessfulProvider();
        });
        await InitializeStoreAsync();

        IRenderedComponent<ProviderTargetCommandForm> cut = Render<ProviderTargetCommandForm>();
        Volatile.Read(ref constructions).ShouldBe(0);

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => service.DispatchCount.ShouldBe(1));
        Volatile.Read(ref constructions).ShouldBe(1);
    }

    [Fact]
    public async Task BlockingProviderFactory_RepeatedTimeoutsRemainBounded() {
        RejectedRecordingCommandService service = new();
        int constructions = 0;
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        Services.AddTransient<ICommandTargetIdentityProvider<ProviderTargetCommand>>(_ => {
            Interlocked.Increment(ref constructions);
            Thread.Sleep(100);
            return new SuccessfulProvider();
        });
        Services.Configure<FcShellOptions>(options => options.CommandTargetResolutionTimeoutMs = 25);
        await InitializeStoreAsync();
        IRenderedComponent<ProviderTargetCommandForm>[] forms = [
            Render<ProviderTargetCommandForm>(),
            Render<ProviderTargetCommandForm>(),
        ];

        forms[0].Find("form").Submit();
        forms[0].WaitForAssertion(() => service.DispatchCount.ShouldBe(1), timeout: TimeSpan.FromSeconds(2));
        SpinWait.SpinUntil(
            () => Volatile.Read(ref constructions) == 1,
            TimeSpan.FromSeconds(2)).ShouldBeTrue();
        await Task.Delay(150, Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        forms[1].Find("form").Submit();
        forms[1].WaitForAssertion(() => service.DispatchCount.ShouldBe(2), timeout: TimeSpan.FromSeconds(2));

        Volatile.Read(ref constructions).ShouldBe(2);
    }

    [Fact]
    public async Task ThrowingProviderFactory_FailsTargetOnlyAndDispatchesFrozenCommand() {
        RecordingCommandService service = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        Services.AddScoped<ICommandTargetIdentityProvider<ProviderTargetCommand>>(_ =>
            throw new InvalidOperationException("factory failed"));
        await InitializeStoreAsync();
        IRenderedComponent<ProviderTargetCommandForm> cut = Render<ProviderTargetCommandForm>(parameters =>
            parameters.Add(component => component.InitialValue, new ProviderTargetCommand { Name = "original" }));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => {
            service.DispatchCount.ShouldBe(1);
            service.DispatchedName.ShouldBe("original");
        });
    }

    [Fact]
    public async Task EditAfterProviderClone_DoesNotChangeFrozenTransportCommand() {
        RecordingCommandService service = new();
        BlockingProvider provider = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        Services.AddScoped<ICommandTargetIdentityProvider<ProviderTargetCommand>>(_ => provider);
        await InitializeStoreAsync();
        IRenderedComponent<ProviderTargetCommandForm> cut = Render<ProviderTargetCommandForm>(parameters =>
            parameters.Add(component => component.InitialValue, new ProviderTargetCommand { Name = "original" }));

        cut.Find("form").Submit();
        await provider.Started.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);
        cut.Find("input").Input("edited-after-clone");
        provider.Release();

        cut.WaitForAssertion(() => {
            service.DispatchCount.ShouldBe(1);
            service.DispatchedName.ShouldBe("original");
        });
    }

    [Fact]
    public async Task MutatingProvider_ReceivesCloneAndCannotChangeDispatchedCommand() {
        RecordingCommandService service = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        Services.AddScoped<ICommandTargetIdentityProvider<ProviderTargetCommand>>(
            _ => new MutatingProvider());
        await InitializeStoreAsync();
        IRenderedComponent<ProviderTargetCommandForm> cut = Render<ProviderTargetCommandForm>(parameters =>
            parameters.Add(component => component.InitialValue, new ProviderTargetCommand { Name = "original" }));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => {
            service.DispatchCount.ShouldBe(1);
            service.DispatchedName.ShouldBe("original");
            cut.Instance.InitialValue.ShouldNotBeNull().Name.ShouldBe("original");
        });
    }

    [Fact]
    public async Task BlockingCloneGetter_UsesSingleBoundedWorkerAndLaterSubmissionsFailTargetFast() {
        RejectedRecordingCommandService service = new();
        CountingBlockingCloneProvider provider = new();
        using ManualResetEventSlim release = new(initialState: false);
        int getterReads = 0;
        BlockingCloneProviderTargetCommand.MessageIdRead = () => {
            Interlocked.Increment(ref getterReads);
            release.Wait(CancellationToken.None);
        };
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        Services.AddScoped<ICommandTargetIdentityProvider<BlockingCloneProviderTargetCommand>>(_ => provider);
        Services.Configure<FcShellOptions>(options => options.CommandTargetResolutionTimeoutMs = 25);
        await InitializeStoreAsync();
        IRenderedComponent<BlockingCloneProviderTargetCommandForm> first =
            Render<BlockingCloneProviderTargetCommandForm>();
        IRenderedComponent<BlockingCloneProviderTargetCommandForm> second =
            Render<BlockingCloneProviderTargetCommandForm>();

        try {
            first.Find("form").Submit();
            SpinWait.SpinUntil(() => Volatile.Read(ref getterReads) == 1, TimeSpan.FromSeconds(2)).ShouldBeTrue();
            first.WaitForAssertion(() => service.DispatchCount.ShouldBe(1), timeout: TimeSpan.FromSeconds(2));

            Stopwatch secondElapsed = Stopwatch.StartNew();
            second.Find("form").Submit();
            second.WaitForAssertion(() => service.DispatchCount.ShouldBe(2), timeout: TimeSpan.FromSeconds(2));

            secondElapsed.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
            Volatile.Read(ref getterReads).ShouldBe(1);
            provider.DispatchCount.ShouldBe(0);
        }
        finally {
            BlockingCloneProviderTargetCommand.MessageIdRead = null;
            release.Set();
        }
    }

    [Fact]
    public async Task ValidStatusMoveProvider_PreservesTargetMetadataAndPublishesExactlyOnce() {
        EarlyTerminalCommandService service = new(AcceptedMessageId);
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        Services.AddScoped<ICommandTargetIdentityProvider<StatusMoveProviderTargetCommand>>(
            _ => new SuccessfulStatusMoveProvider());
        await InitializeStoreAsync();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        INewItemIndicatorStateService indicators = Services.GetRequiredService<INewItemIndicatorStateService>();
        IRenderedComponent<StatusMoveProviderTargetCommandForm> cut =
            Render<StatusMoveProviderTargetCommandForm>();

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => {
            PendingCommandEntry entry = pending.GetByMessageId(AcceptedMessageId).ShouldNotBeNull();
            CommandTargetSnapshot target = entry.TargetSnapshot.ShouldNotBeNull();
            target.ViewKey.ShouldBe("Counter:Counter.Domain.CounterProjection");
            target.EntityKey.ShouldBe("counter-provider");
            target.PriorStatus.ShouldBe("Draft");
            target.ExpectedStatus.ShouldBe("Approved");
            entry.ProjectionTypeName.ShouldBe(target.ProjectionTypeName);
            entry.LaneKey.ShouldBe(target.ViewKey);
            entry.EntityKey.ShouldBe(target.EntityKey);
            entry.ExpectedStatusSlot.ShouldBe(target.ExpectedStatus);
            entry.PriorStatusSlot.ShouldBe(target.PriorStatus);
            indicators.Snapshot("Counter:Counter.Domain.CounterProjection").Count.ShouldBe(1);
        });
    }

    [Theory]
    [InlineData(ProviderFailureMode.Missing)]
    [InlineData(ProviderFailureMode.Duplicate)]
    [InlineData(ProviderFailureMode.Timeout)]
    [InlineData(ProviderFailureMode.Failure)]
    public async Task ProviderTargetFailure_IsLifecycleNeutralAndNeverBlocksDispatch(ProviderFailureMode mode) {
        EarlyTerminalCommandService service = new(AcceptedMessageId);
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        Services.Configure<FcShellOptions>(options => options.CommandTargetResolutionTimeoutMs = 25);
        RegisterProviderMode(mode);
        await InitializeStoreAsync();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        INewItemIndicatorStateService indicators = Services.GetRequiredService<INewItemIndicatorStateService>();
        IRenderedComponent<ProviderTargetCommandForm> cut = Render<ProviderTargetCommandForm>();
        Stopwatch elapsed = Stopwatch.StartNew();

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => {
            service.DispatchCount.ShouldBe(1);
            PendingCommandEntry entry = pending.GetByMessageId(AcceptedMessageId).ShouldNotBeNull();
            entry.Status.ShouldBe(PendingCommandStatus.Confirmed);
            entry.TargetSnapshot.ShouldBeNull();
            indicators.Snapshot("Counter:Counter.Domain.CounterProjection").ShouldBeEmpty();
        }, timeout: TimeSpan.FromSeconds(2));
        elapsed.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(2));
        if (mode == ProviderFailureMode.Timeout) {
            _ignoringCancellationProvider.ShouldNotBeNull().Release();
            await _ignoringCancellationProvider.Completed.Task.WaitAsync(
                TimeSpan.FromSeconds(2),
                Xunit.TestContext.Current.CancellationToken);
        }
    }

    [Theory]
    [InlineData(ProviderContractFailureMode.FixedViewMismatch)]
    [InlineData(ProviderContractFailureMode.ExpectedStatusMismatch)]
    [InlineData(ProviderContractFailureMode.ExpectedStatusMissing)]
    [InlineData(ProviderContractFailureMode.InvalidIdentity)]
    [InlineData(ProviderContractFailureMode.IncompleteStatusMove)]
    public async Task ProviderContractFailure_DispatchesWithNullTargetAndNoIndicator(
        ProviderContractFailureMode mode) {
        EarlyTerminalCommandService service = new(AcceptedMessageId);
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        RegisterProviderContractFailure(mode);
        await InitializeStoreAsync();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        INewItemIndicatorStateService indicators = Services.GetRequiredService<INewItemIndicatorStateService>();

        SubmitProviderContractFailure(mode);

        SpinWait.SpinUntil(
            () => pending.GetByMessageId(AcceptedMessageId)?.Status == PendingCommandStatus.Confirmed,
            TimeSpan.FromSeconds(2)).ShouldBeTrue();
        service.DispatchCount.ShouldBe(1);
        pending.GetByMessageId(AcceptedMessageId).ShouldNotBeNull().TargetSnapshot.ShouldBeNull();
        indicators.Snapshot("Counter:Counter.Domain.CounterProjection").ShouldBeEmpty();
    }

    [Fact]
    public async Task ThrowingUserContextTargetResolution_PreservesAcceptedLifecycleWithoutTarget() {
        EarlyTerminalCommandService service = new(AcceptedMessageId);
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        Services.Replace(ServiceDescriptor.Scoped<IUserContextAccessor>(_ => new ThrowingUserContextAccessor()));
        Services.AddScoped<ICommandTargetIdentityProvider<ProviderTargetCommand>>(_ => new SuccessfulProvider());
        await InitializeStoreAsync();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        INewItemIndicatorStateService indicators = Services.GetRequiredService<INewItemIndicatorStateService>();
        IRenderedComponent<ProviderTargetCommandForm> cut = Render<ProviderTargetCommandForm>();

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => {
            service.DispatchCount.ShouldBe(1);
            PendingCommandEntry entry = pending.GetByMessageId(AcceptedMessageId).ShouldNotBeNull();
            entry.Status.ShouldBe(PendingCommandStatus.Confirmed);
            entry.TargetSnapshot.ShouldBeNull();
        });
        indicators.Snapshot("Counter:Counter.Domain.CounterProjection").ShouldBeEmpty();
    }

    [Fact]
    public async Task ThrowingTimeProviderDuringTargetCapture_StillDispatchesWithoutTarget() {
        EarlyTerminalCommandService service = new(AcceptedMessageId, emitTerminal: false);
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(new TargetCaptureThrowingTimeProvider()));
        Services.AddScoped<ICommandTargetIdentityProvider<ProviderTargetCommand>>(_ => new SuccessfulProvider());
        await InitializeStoreAsync();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        IRenderedComponent<ProviderTargetCommandForm> cut = Render<ProviderTargetCommandForm>();

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => {
            service.DispatchCount.ShouldBe(1);
            pending.GetByMessageId(AcceptedMessageId).ShouldNotBeNull().TargetSnapshot.ShouldBeNull();
        });
    }

    [Fact]
    public async Task CallerCancellationDuringProviderResolution_NeverInvokesCancellationIgnoringService() {
        EarlyTerminalCommandService service = new(AcceptedMessageId, emitTerminal: false);
        BlockingProvider provider = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        Services.AddScoped<ICommandTargetIdentityProvider<ProviderTargetCommand>>(_ => provider);
        Services.Configure<FcShellOptions>(options => options.CommandTargetResolutionTimeoutMs = 10_000);
        await InitializeStoreAsync();
        IRenderedComponent<ProviderTargetCommandForm> cut = Render<ProviderTargetCommandForm>();

        cut.Find("form").Submit();
        await provider.Started.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            Xunit.TestContext.Current.CancellationToken);

        try {
            cut.Instance.Dispose();

            SpinWait.SpinUntil(() => service.DispatchCount != 0, TimeSpan.FromMilliseconds(200)).ShouldBeFalse();
            service.DispatchCount.ShouldBe(0);
        }
        finally {
            provider.Release();
        }
    }

    [Fact]
    public async Task PreAcceptMixedTerminalIds_OnlyAcceptedReplayDispatchesOneTerminalAction() {
        MultiEarlyTerminalCommandService service = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        IPendingCommandOutcomeCoordinator resolver = Services.GetRequiredService<IPendingCommandOutcomeCoordinator>();
        IActionSubscriber actionSubscriber = Services.GetRequiredService<IActionSubscriber>();
        object actionOwner = new();
        int confirmedActions = 0;
        int rejectedActions = 0;
        actionSubscriber.SubscribeToAction<TwoFieldCompactCommandActions.ConfirmedAction>(
            actionOwner,
            _ => Interlocked.Increment(ref confirmedActions));
        actionSubscriber.SubscribeToAction<TwoFieldCompactCommandActions.RejectedAction>(
            actionOwner,
            _ => Interlocked.Increment(ref rejectedActions));
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>();

        try {
            cut.Find("form").Submit();

            cut.WaitForAssertion(() => {
                pending.GetByMessageId(AcceptedMessageId).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Confirmed);
                pending.GetByMessageId(WrongMessageId).ShouldBeNull();
                pending.GetByMessageId(SecondWrongMessageId).ShouldBeNull();
                Volatile.Read(ref confirmedActions).ShouldBe(1);
                Volatile.Read(ref rejectedActions).ShouldBe(0);
            });
        }
        finally {
            actionSubscriber.UnsubscribeFromAllActions(actionOwner);
        }

        resolver.AssociateAccepted(Registration("01DPZ3NDEKTSV4RRFFQ69G5FAV", WrongMessageId))
            .Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        resolver.AssociateAccepted(Registration("01EPZ3NDEKTSV4RRFFQ69G5FAV", SecondWrongMessageId))
            .Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
    }

    [Fact]
    public async Task PreAcceptCallbackForAnotherPendingMessageId_CannotResolveThatCommand() {
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(provider =>
            new PreAcceptCollisionCommandService(
                provider.GetRequiredService<IPendingCommandOutcomeCoordinator>())));
        await InitializeStoreAsync();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>();

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => {
            pending.GetByMessageId(WrongMessageId).ShouldNotBeNull().Status
                .ShouldBe(PendingCommandStatus.Pending);
            pending.GetByMessageId(AcceptedMessageId).ShouldNotBeNull().Status
                .ShouldBe(PendingCommandStatus.Confirmed);
        });
    }

    [Fact]
    public async Task PreAcceptTracking_CanonicalizesVariantsBoundsDistinctIdsAndCleansFifoBuffer() {
        HeldManyTerminalCommandService service = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        Services.Configure<FcShellOptions>(options => options.MaxPendingCommandEntries = 2);
        await InitializeStoreAsync();
        PendingCommandOutcomeResolver resolver =
            (PendingCommandOutcomeResolver)Services.GetRequiredService<IPendingCommandOutcomeCoordinator>();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>();

        cut.Find("form").Submit();
        await service.ObservationsSent.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            Xunit.TestContext.Current.CancellationToken);

        resolver.BufferedObservationCount.ShouldBe(2);
        resolver.BufferedOrderCount.ShouldBe(2);

        service.ReturnResult.TrySetResult();

        cut.WaitForAssertion(() => {
            resolver.BufferedObservationCount.ShouldBe(0);
            resolver.BufferedOrderCount.ShouldBe(0);
        });
        resolver.AssociateAccepted(Registration("01EPZ3NDEKTSV4RRFFQ69G5FAV", SecondWrongMessageId))
            .Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        resolver.AssociateAccepted(Registration("01FPZ3NDEKTSV4RRFFQ69G5FAV", ThirdWrongMessageId))
            .Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
    }

    [Fact]
    public async Task PreAcceptException_DiscardsEveryBufferedTerminalId() {
        ThrowingAfterTerminalCommandService service = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        PendingCommandOutcomeResolver resolver = (PendingCommandOutcomeResolver)Services.GetRequiredService<IPendingCommandOutcomeResolver>();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>();

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => {
            service.DispatchCount.ShouldBe(1);
            resolver.BufferedObservationCount.ShouldBe(0);
            resolver.BufferedOrderCount.ShouldBe(0);
        });
        resolver.AssociateAccepted(Registration("01DPZ3NDEKTSV4RRFFQ69G5FAV", WrongMessageId))
            .Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
    }

    [Fact]
    public async Task DispatchException_ClosesRetainedCallbackAndIgnoresLateTerminal() {
        ThrowingAfterTerminalCommandService service = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        PendingCommandOutcomeResolver resolver =
            (PendingCommandOutcomeResolver)Services.GetRequiredService<IPendingCommandOutcomeResolver>();
        IState<TwoFieldCompactCommandLifecycleState> state =
            Services.GetRequiredService<IState<TwoFieldCompactCommandLifecycleState>>();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>();
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => state.Value.State.ShouldBe(CommandLifecycleState.Rejected));

        service.EmitRetainedTerminal(AcceptedMessageId);

        resolver.BufferedObservationCount.ShouldBe(0);
        resolver.BufferedOrderCount.ShouldBe(0);
        state.Value.State.ShouldBe(CommandLifecycleState.Rejected);
    }

    [Fact]
    public async Task PreAcceptDisposalDiscardsBufferedIds() {
        CancelAfterTerminalCommandService canceledService = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => canceledService));
        await InitializeStoreAsync();
        PendingCommandOutcomeResolver resolver = (PendingCommandOutcomeResolver)Services.GetRequiredService<IPendingCommandOutcomeResolver>();
        IRenderedComponent<TwoFieldCompactCommandForm> canceled = Render<TwoFieldCompactCommandForm>();
        canceled.Find("form").Submit();
        await canceledService.CallbackSent.Task.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);

        canceled.Instance.Dispose();

        SpinWait.SpinUntil(
            () => resolver.BufferedObservationCount == 0 && resolver.BufferedOrderCount == 0,
            TimeSpan.FromSeconds(2)).ShouldBeTrue();
        resolver.BufferedObservationCount.ShouldBe(0);
        resolver.BufferedOrderCount.ShouldBe(0);
        resolver.AssociateAccepted(Registration("01DPZ3NDEKTSV4RRFFQ69G5FAV", WrongMessageId))
            .Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
    }

    [Fact]
    public async Task DisposalAfterServerAcceptance_DefersEarlyTerminalCleanupUntilAcceptedAssociation() {
        HeldAcceptedWithEarlyTerminalCommandService service = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        PendingCommandOutcomeResolver resolver =
            (PendingCommandOutcomeResolver)Services.GetRequiredService<IPendingCommandOutcomeResolver>();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>();
        cut.Find("form").Submit();
        await service.ServerAccepted.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            Xunit.TestContext.Current.CancellationToken);
        resolver.BufferedObservationCount.ShouldBe(1);

        cut.Instance.Dispose();

        resolver.BufferedObservationCount.ShouldBe(1);
        service.ReturnAccepted.TrySetResult();
        SpinWait.SpinUntil(
            () => pending.GetByMessageId(AcceptedMessageId)?.Status == PendingCommandStatus.Confirmed,
            TimeSpan.FromSeconds(2)).ShouldBeTrue();
        resolver.BufferedObservationCount.ShouldBe(0);
        pending.GetByMessageId(AcceptedMessageId).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Confirmed);
    }

    [Fact]
    public async Task AcceptedDisposalPreservesResolverOwnedLifecycleForLateTerminalCallback() {
        LateTerminalCommandService acceptedService = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => acceptedService));
        await InitializeStoreAsync();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        ILifecycleStateService lifecycle = Services.GetRequiredService<ILifecycleStateService>();
        IRenderedComponent<FourFieldCompactCommandForm> accepted = Render<FourFieldCompactCommandForm>();
        accepted.Find("form").Submit();
        accepted.WaitForAssertion(() => acceptedService.Callback.ShouldNotBeNull());
        accepted.WaitForAssertion(() => pending.GetByMessageId(AcceptedMessageId).ShouldNotBeNull());

        accepted.Instance.Dispose();
        acceptedService.EmitConfirmed();

        PendingCommandEntry entry = pending.GetByMessageId(AcceptedMessageId).ShouldNotBeNull();
        entry.Status.ShouldBe(PendingCommandStatus.Confirmed);
        lifecycle.GetState(entry.CorrelationId).ShouldBe(CommandLifecycleState.Confirmed);
    }

    [Fact]
    public async Task AcceptedTerminal_ClosesCallbackAndLateSyncingCannotRegressUiOrDispatchAgain() {
        LateTerminalCommandService service = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IState<FourFieldCompactCommandLifecycleState> state =
            Services.GetRequiredService<IState<FourFieldCompactCommandLifecycleState>>();
        IActionSubscriber actionSubscriber = Services.GetRequiredService<IActionSubscriber>();
        object actionOwner = new();
        int confirmedActions = 0;
        int syncingActions = 0;
        actionSubscriber.SubscribeToAction<FourFieldCompactCommandActions.ConfirmedAction>(
            actionOwner,
            _ => Interlocked.Increment(ref confirmedActions));
        actionSubscriber.SubscribeToAction<FourFieldCompactCommandActions.SyncingAction>(
            actionOwner,
            _ => Interlocked.Increment(ref syncingActions));
        IRenderedComponent<FourFieldCompactCommandForm> cut = Render<FourFieldCompactCommandForm>();

        try {
            cut.Find("form").Submit();
            cut.WaitForAssertion(() => service.Callback.ShouldNotBeNull());
            service.EmitConfirmed();
            cut.WaitForAssertion(() => {
                state.Value.State.ShouldBe(CommandLifecycleState.Confirmed);
                Volatile.Read(ref confirmedActions).ShouldBe(1);
            });

            service.EmitSyncing();

            state.Value.State.ShouldBe(CommandLifecycleState.Confirmed);
            Volatile.Read(ref confirmedActions).ShouldBe(1);
            Volatile.Read(ref syncingActions).ShouldBe(0);
        }
        finally {
            actionSubscriber.UnsubscribeFromAllActions(actionOwner);
        }
    }

    [Fact]
    public async Task AcceptedResultReturningAfterDisposal_IsAssociatedBeforeSubmitExits() {
        HeldAcceptedCommandService service = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>();
        cut.Find("form").Submit();
        await service.DispatchStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            Xunit.TestContext.Current.CancellationToken);

        cut.Instance.Dispose();
        service.ReturnAccepted.TrySetResult();

        SpinWait.SpinUntil(
            () => pending.GetByMessageId(AcceptedMessageId) is not null,
            TimeSpan.FromSeconds(2)).ShouldBeTrue();
        pending.GetByMessageId(AcceptedMessageId).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Pending);
    }

    [Fact]
    public async Task RejectedDispatchResult_DoesNotAcknowledgeAndLeavesFormRejected() {
        HeldResultCommandService service = new(new CommandResult(AcceptedMessageId, "Rejected"));
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IState<TwoFieldCompactCommandLifecycleState> state =
            Services.GetRequiredService<IState<TwoFieldCompactCommandLifecycleState>>();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>();
        cut.Find("form").Submit();
        await service.DispatchStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            Xunit.TestContext.Current.CancellationToken);
        state.Value.State.ShouldBe(CommandLifecycleState.Submitting);

        service.ReturnResult.TrySetResult();

        cut.WaitForAssertion(() => state.Value.State.ShouldBe(CommandLifecycleState.Rejected));
        pending.GetByMessageId(AcceptedMessageId).ShouldBeNull();
    }

    [Fact]
    public async Task NonAcceptedResult_ClosesRetainedCallbackAndIgnoresLateTerminal() {
        HeldResultCommandService service = new(new CommandResult(AcceptedMessageId, "Rejected"));
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        PendingCommandOutcomeResolver resolver =
            (PendingCommandOutcomeResolver)Services.GetRequiredService<IPendingCommandOutcomeResolver>();
        IState<TwoFieldCompactCommandLifecycleState> state =
            Services.GetRequiredService<IState<TwoFieldCompactCommandLifecycleState>>();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>();
        cut.Find("form").Submit();
        await service.DispatchStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            Xunit.TestContext.Current.CancellationToken);
        service.ReturnResult.TrySetResult();
        cut.WaitForAssertion(() => state.Value.State.ShouldBe(CommandLifecycleState.Rejected));

        service.EmitRetainedTerminal(AcceptedMessageId);

        resolver.BufferedObservationCount.ShouldBe(0);
        resolver.BufferedOrderCount.ShouldBe(0);
        state.Value.State.ShouldBe(CommandLifecycleState.Rejected);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("not-a-ulid")]
    public async Task InvalidAcceptedMessageId_DoesNotAcknowledgeAndKeepsSubmitting(string messageId) {
        HeldResultCommandService service = new(new CommandResult(messageId, "Accepted"));
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IState<TwoFieldCompactCommandLifecycleState> state =
            Services.GetRequiredService<IState<TwoFieldCompactCommandLifecycleState>>();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>();
        cut.Find("form").Submit();
        await service.DispatchStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            Xunit.TestContext.Current.CancellationToken);
        state.Value.State.ShouldBe(CommandLifecycleState.Submitting);

        service.ReturnResult.TrySetResult();

        cut.WaitForAssertion(() => state.Value.State.ShouldBe(CommandLifecycleState.Submitting));
        state.Value.State.ShouldNotBe(CommandLifecycleState.Idle);
        state.Value.State.ShouldNotBe(CommandLifecycleState.Confirmed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnavailableAcceptedAssociation_DoesNotAcknowledgeAndKeepsSubmitting(bool throwOnAssociation) {
        HeldResultCommandService service = new(new CommandResult(AcceptedMessageId, "Accepted"));
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        if (throwOnAssociation) {
            Services.Replace(ServiceDescriptor.Scoped<IPendingCommandOutcomeCoordinator>(
                _ => new ThrowingOutcomeCoordinator()));
        }
        await InitializeStoreAsync();
        IPendingCommandOutcomeCoordinator coordinator =
            Services.GetRequiredService<IPendingCommandOutcomeCoordinator>();
        if (!throwOnAssociation) {
            ((IDisposable)coordinator).Dispose();
        }
        IState<TwoFieldCompactCommandLifecycleState> state =
            Services.GetRequiredService<IState<TwoFieldCompactCommandLifecycleState>>();
        IRenderedComponent<TwoFieldCompactCommandForm> cut = Render<TwoFieldCompactCommandForm>();
        cut.Find("form").Submit();
        await service.DispatchStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            Xunit.TestContext.Current.CancellationToken);

        service.ReturnResult.TrySetResult();

        cut.WaitForAssertion(() => state.Value.State.ShouldBe(CommandLifecycleState.Submitting));
        state.Value.State.ShouldNotBe(CommandLifecycleState.Idle);
        state.Value.State.ShouldNotBe(CommandLifecycleState.Confirmed);
    }

    [Fact]
    public async Task TypedRejectedCallback_ResolvesPendingRejectedWithoutIndicatorAndLeavesUiRejected() {
        EarlyRejectedCommandService service = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => service));
        await InitializeStoreAsync();
        IState<SameSourceTargetCommandLifecycleState> state =
            Services.GetRequiredService<IState<SameSourceTargetCommandLifecycleState>>();
        IPendingCommandStateService pending = Services.GetRequiredService<IPendingCommandStateService>();
        INewItemIndicatorStateService indicators = Services.GetRequiredService<INewItemIndicatorStateService>();
        PendingCommandRowIdentity row = new(
            typeof(Counter.Domain.CounterProjection).FullName!,
            "counter-counts",
            "counter-42");
        IRenderedComponent<CascadingValue<PendingCommandRowIdentity?>> host =
            Render<CascadingValue<PendingCommandRowIdentity?>>(parameters => parameters
                .Add(component => component.Value, row)
                .Add(component => component.IsFixed, true)
                .AddChildContent<SameSourceTargetCommandForm>());

        host.Find("form").Submit();

        host.WaitForAssertion(() => {
            pending.GetByMessageId(AcceptedMessageId).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Rejected);
            state.Value.State.ShouldBe(CommandLifecycleState.Rejected);
        });
        indicators.Snapshot("counter-counts").ShouldBeEmpty();
    }

    private static PendingCommandRegistration Registration(string correlationId, string messageId) =>
        new(correlationId, messageId, "Test.Command");

    private void RegisterProviderMode(ProviderFailureMode mode) {
        switch (mode) {
            case ProviderFailureMode.Missing:
                break;
            case ProviderFailureMode.Duplicate:
                Services.AddScoped<ICommandTargetIdentityProvider<ProviderTargetCommand>>(_ => new SuccessfulProvider());
                Services.AddScoped<ICommandTargetIdentityProvider<ProviderTargetCommand>>(_ => new SuccessfulProvider());
                break;
            case ProviderFailureMode.Timeout:
                _ignoringCancellationProvider = new IgnoringCancellationProvider();
                Services.AddScoped<ICommandTargetIdentityProvider<ProviderTargetCommand>>(
                    _ => _ignoringCancellationProvider!);
                break;
            case ProviderFailureMode.Failure:
                Services.AddScoped<ICommandTargetIdentityProvider<ProviderTargetCommand>>(_ => new ThrowingProvider());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    private void RegisterProviderContractFailure(ProviderContractFailureMode mode) {
        switch (mode) {
            case ProviderContractFailureMode.FixedViewMismatch:
                Services.AddScoped<ICommandTargetIdentityProvider<ProviderTargetCommand>>(
                    _ => new FixedViewMismatchProvider());
                break;
            case ProviderContractFailureMode.ExpectedStatusMismatch:
                Services.AddScoped<ICommandTargetIdentityProvider<ExpectedStatusProviderTargetCommand>>(
                    _ => new ExpectedStatusMismatchProvider());
                break;
            case ProviderContractFailureMode.ExpectedStatusMissing:
                Services.AddScoped<ICommandTargetIdentityProvider<ExpectedStatusProviderTargetCommand>>(
                    _ => new ExpectedStatusMissingProvider());
                break;
            case ProviderContractFailureMode.InvalidIdentity:
                Services.AddScoped<ICommandTargetIdentityProvider<ProviderTargetCommand>>(
                    _ => new InvalidIdentityProvider());
                break;
            case ProviderContractFailureMode.IncompleteStatusMove:
                Services.AddScoped<ICommandTargetIdentityProvider<StatusMoveProviderTargetCommand>>(
                    _ => new IncompleteStatusMoveProvider());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    private void SubmitProviderContractFailure(ProviderContractFailureMode mode) {
        switch (mode) {
            case ProviderContractFailureMode.FixedViewMismatch:
            case ProviderContractFailureMode.InvalidIdentity:
                Render<ProviderTargetCommandForm>().Find("form").Submit();
                break;
            case ProviderContractFailureMode.ExpectedStatusMismatch:
            case ProviderContractFailureMode.ExpectedStatusMissing:
                Render<ExpectedStatusProviderTargetCommandForm>().Find("form").Submit();
                break;
            case ProviderContractFailureMode.IncompleteStatusMove:
                Render<StatusMoveProviderTargetCommandForm>().Find("form").Submit();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    public enum ProviderFailureMode {
        Missing,
        Duplicate,
        Timeout,
        Failure,
    }

    public enum ProviderContractFailureMode {
        FixedViewMismatch,
        ExpectedStatusMismatch,
        ExpectedStatusMissing,
        InvalidIdentity,
        IncompleteStatusMove,
    }

    private sealed class SuccessfulProvider : ICommandTargetIdentityProvider<ProviderTargetCommand> {
        public ValueTask<CommandTargetIdentity?> ResolveAsync(
            ProviderTargetCommand command,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<CommandTargetIdentity?>(new("Counter:Counter.Domain.CounterProjection", "counter-provider"));
    }

    private sealed class SuccessfulDeleteProvider : ICommandTargetIdentityProvider<DeleteProviderTargetCommand> {
        public ValueTask<CommandTargetIdentity?> ResolveAsync(
            DeleteProviderTargetCommand command,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<CommandTargetIdentity?>(
                new("Counter:Counter.Domain.CounterProjection", "counter-provider-delete"));
    }

    private sealed class MutatingProvider : ICommandTargetIdentityProvider<ProviderTargetCommand> {
        public ValueTask<CommandTargetIdentity?> ResolveAsync(
            ProviderTargetCommand command,
            CancellationToken cancellationToken) {
            command.Name = "provider-mutated";
            return ValueTask.FromResult<CommandTargetIdentity?>(
                new("Counter:Counter.Domain.CounterProjection", "counter-provider"));
        }
    }

    private sealed class CountingBlockingCloneProvider : ICommandTargetIdentityProvider<BlockingCloneProviderTargetCommand> {
        public int DispatchCount { get; private set; }

        public ValueTask<CommandTargetIdentity?> ResolveAsync(
            BlockingCloneProviderTargetCommand command,
            CancellationToken cancellationToken) {
            DispatchCount++;
            return ValueTask.FromResult<CommandTargetIdentity?>(
                new("Counter:Counter.Domain.CounterProjection", "counter-provider"));
        }
    }

    private sealed class SynchronouslyBlockingProvider : ICommandTargetIdentityProvider<ProviderTargetCommand>, IDisposable {
        private readonly ManualResetEventSlim _release = new(initialState: false);
        private int _disposed;

        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask<CommandTargetIdentity?> ResolveAsync(
            ProviderTargetCommand command,
            CancellationToken cancellationToken) {
            Started.TrySetResult();
            _release.Wait(CancellationToken.None);
            return ValueTask.FromResult<CommandTargetIdentity?>(
                new("Counter:Counter.Domain.CounterProjection", "counter-provider"));
        }

        public void Release() {
            if (Volatile.Read(ref _disposed) == 0) {
                _release.Set();
            }
        }

        public void Dispose() {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) {
                return;
            }

            _release.Set();
            _release.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    private sealed class FixedViewMismatchProvider : ICommandTargetIdentityProvider<ProviderTargetCommand> {
        public ValueTask<CommandTargetIdentity?> ResolveAsync(
            ProviderTargetCommand command,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<CommandTargetIdentity?>(new("Specimens:Counter.Specimens.Domain.SpecimenStatusProjection", "counter-provider"));
    }

    private sealed class ExpectedStatusMismatchProvider : ICommandTargetIdentityProvider<ExpectedStatusProviderTargetCommand> {
        public ValueTask<CommandTargetIdentity?> ResolveAsync(
            ExpectedStatusProviderTargetCommand command,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<CommandTargetIdentity?>(
                new("Counter:Counter.Domain.CounterProjection", "counter-provider", ExpectedStatus: "Rejected"));
    }

    private sealed class ExpectedStatusMissingProvider : ICommandTargetIdentityProvider<ExpectedStatusProviderTargetCommand> {
        public ValueTask<CommandTargetIdentity?> ResolveAsync(
            ExpectedStatusProviderTargetCommand command,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<CommandTargetIdentity?>(
                new("Counter:Counter.Domain.CounterProjection", "counter-provider"));
    }

    private sealed class InvalidIdentityProvider : ICommandTargetIdentityProvider<ProviderTargetCommand> {
        public ValueTask<CommandTargetIdentity?> ResolveAsync(
            ProviderTargetCommand command,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<CommandTargetIdentity?>(new("Counter:Counter.Domain.CounterProjection", "   "));
    }

    private sealed class IncompleteStatusMoveProvider : ICommandTargetIdentityProvider<StatusMoveProviderTargetCommand> {
        public ValueTask<CommandTargetIdentity?> ResolveAsync(
            StatusMoveProviderTargetCommand command,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<CommandTargetIdentity?>(new("Counter:Counter.Domain.CounterProjection", "counter-provider"));
    }

    private sealed class SuccessfulStatusMoveProvider : ICommandTargetIdentityProvider<StatusMoveProviderTargetCommand> {
        public ValueTask<CommandTargetIdentity?> ResolveAsync(
            StatusMoveProviderTargetCommand command,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<CommandTargetIdentity?>(
                new("Counter:Counter.Domain.CounterProjection", "counter-provider", "Draft", "Approved"));
    }

    private sealed class BlockingProvider : ICommandTargetIdentityProvider<ProviderTargetCommand> {
        private readonly TaskCompletionSource<CommandTargetIdentity?> _never =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask<CommandTargetIdentity?> ResolveAsync(
            ProviderTargetCommand command,
            CancellationToken cancellationToken) {
            Started.TrySetResult();
            return new ValueTask<CommandTargetIdentity?>(_never.Task);
        }

        public void Release() => _never.TrySetResult(null);
    }

    private sealed class ThrowingProvider : ICommandTargetIdentityProvider<ProviderTargetCommand> {
        public ValueTask<CommandTargetIdentity?> ResolveAsync(
            ProviderTargetCommand command,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("provider failed");
    }

    private sealed class IgnoringCancellationProvider : ICommandTargetIdentityProvider<ProviderTargetCommand> {
        private readonly TaskCompletionSource<CommandTargetIdentity?> _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async ValueTask<CommandTargetIdentity?> ResolveAsync(
            ProviderTargetCommand command,
            CancellationToken cancellationToken) {
            CommandTargetIdentity? result = await _release.Task.ConfigureAwait(false);
            Completed.TrySetResult();
            return result;
        }

        public void Release() => _release.TrySetResult(null);
    }

    private sealed class ThrowingUserContextAccessor : IUserContextAccessor {
        public string? TenantId => throw new InvalidOperationException("tenant unavailable");

        public string? UserId => throw new InvalidOperationException("user unavailable");
    }

    private sealed class TargetCaptureThrowingTimeProvider : TimeProvider {
        private int _targetCaptureFailures;

        public override DateTimeOffset GetUtcNow() {
            if (new StackTrace().GetFrames().Any(frame =>
                frame.GetMethod()?.Name.Contains("ResolveCommandTargetCoreAsync", StringComparison.Ordinal) == true
                || frame.GetMethod()?.DeclaringType?.Name.Contains("ResolveCommandTargetCoreAsync", StringComparison.Ordinal) == true)
                && Interlocked.Exchange(ref _targetCaptureFailures, 1) == 0) {
                throw new InvalidOperationException("target clock unavailable");
            }

            return TimeProvider.System.GetUtcNow();
        }
    }

    private class EarlyTerminalCommandService(string messageId, bool emitTerminal = true) : ICommandServiceWithLifecycleObservations {
        public int DispatchCount { get; private set; }

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class => DispatchAsync(command, null, cancellationToken);

        public virtual Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleObservation>? onLifecycleObservation,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            DispatchCount++;
            if (emitTerminal) {
                onLifecycleObservation?.Invoke(new CommandLifecycleObservation(
                    CommandLifecycleState.Confirmed,
                    messageId,
                    CommandMateriality.Material,
                    TimeProvider.System.GetUtcNow()));
            }

            return Task.FromResult(new CommandResult(messageId, "Accepted"));
        }
    }

    private sealed class RecordingCommandService : ICommandServiceWithLifecycleObservations {
        public int DispatchCount { get; private set; }

        public string? DispatchedName { get; private set; }

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : class => DispatchAsync(command, null, cancellationToken);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleObservation>? onLifecycleObservation,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            DispatchCount++;
            DispatchedName = (command as ProviderTargetCommand)?.Name;
            return Task.FromResult(new CommandResult(AcceptedMessageId, "Accepted"));
        }
    }

    private sealed class RejectedRecordingCommandService : ICommandServiceWithLifecycleObservations {
        public int DispatchCount { get; private set; }

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : class => DispatchAsync(command, null, cancellationToken);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleObservation>? onLifecycleObservation,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            DispatchCount++;
            return Task.FromResult(new CommandResult(AcceptedMessageId, "Rejected"));
        }
    }

    private sealed class MultiEarlyTerminalCommandService : EarlyTerminalCommandService {
        public MultiEarlyTerminalCommandService()
            : base(AcceptedMessageId, emitTerminal: false) {
        }

        public override Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleObservation>? onLifecycleObservation,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            foreach (string id in new[] { WrongMessageId, AcceptedMessageId, SecondWrongMessageId }) {
                onLifecycleObservation?.Invoke(MaterialConfirmed(id));
            }

            return Task.FromResult(new CommandResult(AcceptedMessageId, "Accepted"));
        }
    }

    private sealed class PreAcceptCollisionCommandService(
        IPendingCommandOutcomeCoordinator coordinator) : ICommandServiceWithLifecycleObservations {
        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : class => DispatchAsync(command, null, cancellationToken);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleObservation>? onLifecycleObservation,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            coordinator.AssociateAccepted(Registration("01DPZ3NDEKTSV4RRFFQ69G5FAV", WrongMessageId))
                .Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
            onLifecycleObservation.ShouldNotBeNull().Invoke(MaterialConfirmed(WrongMessageId));
            onLifecycleObservation.Invoke(MaterialConfirmed(AcceptedMessageId));
            return Task.FromResult(new CommandResult(AcceptedMessageId, "Accepted"));
        }
    }

    private sealed class HeldManyTerminalCommandService : ICommandServiceWithLifecycleObservations {
        public TaskCompletionSource ObservationsSent { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReturnResult { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : class => DispatchAsync(command, null, cancellationToken);

        public async Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleObservation>? onLifecycleObservation,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            onLifecycleObservation?.Invoke(MaterialConfirmed($"  {WrongMessageId.ToLowerInvariant()}  "));
            onLifecycleObservation?.Invoke(MaterialConfirmed(WrongMessageId));
            onLifecycleObservation?.Invoke(MaterialConfirmed(SecondWrongMessageId));
            onLifecycleObservation?.Invoke(MaterialConfirmed(ThirdWrongMessageId));
            ObservationsSent.TrySetResult();
            await ReturnResult.Task.ConfigureAwait(false);
            return new CommandResult(AcceptedMessageId, "Accepted");
        }
    }

    private sealed class HeldAcceptedCommandService : ICommandServiceWithLifecycleObservations {
        public TaskCompletionSource DispatchStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReturnAccepted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : class => DispatchAsync(command, null, cancellationToken);

        public async Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleObservation>? onLifecycleObservation,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            DispatchStarted.TrySetResult();
            await ReturnAccepted.Task.ConfigureAwait(false);
            return new CommandResult(AcceptedMessageId, "Accepted");
        }
    }

    private sealed class HeldAcceptedWithEarlyTerminalCommandService : ICommandServiceWithLifecycleObservations {
        public TaskCompletionSource ServerAccepted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReturnAccepted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : class => DispatchAsync(command, null, cancellationToken);

        public async Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleObservation>? onLifecycleObservation,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            onLifecycleObservation?.Invoke(MaterialConfirmed(AcceptedMessageId));
            ServerAccepted.TrySetResult();
            await ReturnAccepted.Task.ConfigureAwait(false);
            return new CommandResult(AcceptedMessageId, "Accepted");
        }
    }

    private sealed class HeldResultCommandService(CommandResult result) : ICommandServiceWithLifecycleObservations {
        public TaskCompletionSource DispatchStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReturnResult { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Action<CommandLifecycleObservation>? RetainedCallback { get; private set; }

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : class => DispatchAsync(command, null, cancellationToken);

        public async Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleObservation>? onLifecycleObservation,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            RetainedCallback = onLifecycleObservation;
            DispatchStarted.TrySetResult();
            await ReturnResult.Task.ConfigureAwait(false);
            return result;
        }

        public void EmitRetainedTerminal(string messageId) =>
            RetainedCallback.ShouldNotBeNull().Invoke(MaterialConfirmed(messageId));
    }

    private sealed class EarlyRejectedCommandService : ICommandServiceWithLifecycleObservations {
        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : class => DispatchAsync(command, null, cancellationToken);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleObservation>? onLifecycleObservation,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            onLifecycleObservation?.Invoke(new CommandLifecycleObservation(
                CommandLifecycleState.Rejected,
                AcceptedMessageId,
                CommandMateriality.Material,
                TimeProvider.System.GetUtcNow()));
            return Task.FromResult(new CommandResult(AcceptedMessageId, "Accepted"));
        }
    }

    private sealed class ThrowingOutcomeCoordinator : IPendingCommandOutcomeCoordinator {
        public PendingCommandOutcomeResolutionResult BufferBeforeAccepted(
            string ownerId,
            PendingCommandOutcomeObservation observation) =>
            new(PendingCommandOutcomeResolutionStatus.Buffered);

        public PendingCommandRegistrationResult AssociateAccepted(PendingCommandRegistration registration) =>
            throw new InvalidOperationException("association failed");

        public void DiscardBuffered(string? messageId) {
        }

        public void DiscardBufferedByOwner(string ownerId) {
            throw new InvalidOperationException("cleanup failed");
        }

        public PendingCommandOutcomeResolutionResult Resolve(PendingCommandOutcomeObservation observation) =>
            new(PendingCommandOutcomeResolutionStatus.Unknown);
    }

    private sealed class FixedPendingCommandStatusQuery(PendingCommandOutcomeObservation observation)
        : IPendingCommandStatusQuery {
        public ValueTask<PendingCommandOutcomeObservation?> QueryAsync(
            PendingCommandEntry pendingCommand,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<PendingCommandOutcomeObservation?>(observation);
    }

    private sealed class ThrowingAfterTerminalCommandService : ICommandServiceWithLifecycleObservations {
        public int DispatchCount { get; private set; }

        public Action<CommandLifecycleObservation>? RetainedCallback { get; private set; }

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class => DispatchAsync(command, null, cancellationToken);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleObservation>? onLifecycleObservation,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            DispatchCount++;
            RetainedCallback = onLifecycleObservation;
            onLifecycleObservation?.Invoke(MaterialConfirmed(WrongMessageId));
            throw new CommandRejectedException("Rejected", "Retry");
        }

        public void EmitRetainedTerminal(string messageId) =>
            RetainedCallback.ShouldNotBeNull().Invoke(MaterialConfirmed(messageId));
    }

    private sealed class CancelAfterTerminalCommandService : ICommandServiceWithLifecycleObservations {
        public TaskCompletionSource CallbackSent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class => DispatchAsync(command, null, cancellationToken);

        public async Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleObservation>? onLifecycleObservation,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            onLifecycleObservation?.Invoke(MaterialConfirmed(WrongMessageId));
            CallbackSent.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            throw new UnreachableException();
        }
    }

    private sealed class LateTerminalCommandService : ICommandServiceWithLifecycleObservations {
        public Action<CommandLifecycleObservation>? Callback { get; private set; }

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class => DispatchAsync(command, null, cancellationToken);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleObservation>? onLifecycleObservation,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            Callback = onLifecycleObservation;
            return Task.FromResult(new CommandResult(AcceptedMessageId, "Accepted"));
        }

        public void EmitConfirmed() => Callback.ShouldNotBeNull().Invoke(MaterialConfirmed(AcceptedMessageId));

        public void EmitSyncing() => Callback.ShouldNotBeNull().Invoke(new CommandLifecycleObservation(
            CommandLifecycleState.Syncing,
            AcceptedMessageId,
            CommandMateriality.Unknown,
            TimeProvider.System.GetUtcNow()));
    }

    private sealed class SynchronousSyncingCommandService : ICommandServiceWithLifecycleObservations {
        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : class => DispatchAsync(command, null, cancellationToken);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleObservation>? onLifecycleObservation,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            onLifecycleObservation?.Invoke(new CommandLifecycleObservation(
                CommandLifecycleState.Syncing,
                AcceptedMessageId,
                CommandMateriality.Unknown,
                TimeProvider.System.GetUtcNow()));
            return Task.FromResult(new CommandResult(AcceptedMessageId, CommandResultStatus.Accepted));
        }
    }

    private sealed class SynchronousTerminalReplayLifecycleService(string correlationId) : ILifecycleStateService {
        private sealed class Subscription(Action onDispose) : IDisposable {
            public void Dispose() => onDispose();
        }

        public bool SubscriptionDisposed { get; private set; }

        public IDisposable Subscribe(string subscribedCorrelationId, Action<CommandLifecycleTransition> onTransition) {
            subscribedCorrelationId.ShouldBe(correlationId);
            DateTimeOffset now = TimeProvider.System.GetUtcNow();
            onTransition(new CommandLifecycleTransition(
                correlationId,
                CommandLifecycleState.Acknowledged,
                CommandLifecycleState.Confirmed,
                AcceptedMessageId,
                now,
                now,
                IdempotencyResolved: false));
            return new Subscription(() => SubscriptionDisposed = true);
        }

        public CommandLifecycleState GetState(string requestedCorrelationId) => CommandLifecycleState.Confirmed;

        public string? GetMessageId(string requestedCorrelationId) => AcceptedMessageId;

        public IEnumerable<string> GetActiveCorrelationIds() => [correlationId];

        public void Transition(string requestedCorrelationId, CommandLifecycleState newState, string? messageId = null) {
        }

        public void Transition(
            string requestedCorrelationId,
            CommandLifecycleState newState,
            string? messageId,
            bool idempotencyResolved) {
        }

        public void Dispose() {
        }
    }

    private sealed class ThrowingStaleSubscriptionLifecycleService(
        string throwingCorrelationId,
        bool fatal = false) : ILifecycleStateService {
        private sealed class Subscription(Action dispose) : IDisposable {
            public void Dispose() => dispose();
        }

        public List<string> Disposed { get; } = [];

        public List<string> Subscribed { get; } = [];

        public List<(string CorrelationId, CommandLifecycleState State)> Transitions { get; } = [];

        public IDisposable Subscribe(string correlationId, Action<CommandLifecycleTransition> onTransition) {
            Subscribed.Add(correlationId);
            return new Subscription(() => {
                Disposed.Add(correlationId);
                if (string.Equals(correlationId, throwingCorrelationId, StringComparison.Ordinal)) {
                    if (fatal) {
                        _ = ThrowFatal<object?>();
                    }

                    throw new InvalidOperationException("subscription cleanup failed");
                }
            });
        }

        public CommandLifecycleState GetState(string correlationId) => CommandLifecycleState.Idle;

        public string? GetMessageId(string correlationId) => null;

        public IEnumerable<string> GetActiveCorrelationIds() => [];

        public void Transition(string correlationId, CommandLifecycleState newState, string? messageId = null) =>
            Transitions.Add((correlationId, newState));

        public void Transition(
            string correlationId,
            CommandLifecycleState newState,
            string? messageId,
            bool idempotencyResolved) =>
            Transitions.Add((correlationId, newState));

        public void Dispose() {
        }
    }

    private static T ThrowFatal<T>() {
        System.Runtime.ExceptionServices.ExceptionDispatchInfo
            .Capture((Exception)Activator.CreateInstance(
                typeof(OutOfMemoryException),
                "fatal test exception")!)
            .Throw();
        return default!;
    }

    private static CommandLifecycleObservation MaterialConfirmed(string messageId) =>
        new(
            CommandLifecycleState.Confirmed,
            messageId,
            CommandMateriality.Material,
            TimeProvider.System.GetUtcNow());
}
