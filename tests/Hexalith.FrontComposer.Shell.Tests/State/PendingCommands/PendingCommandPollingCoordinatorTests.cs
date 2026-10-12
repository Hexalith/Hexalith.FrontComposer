using System.Net;
using System.Text;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure.Tenancy;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.PendingCommands;

public sealed class PendingCommandPollingCoordinatorTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HeldProviderCannotRetainQueryCleanupOrReopenDeadline(bool faultAfterDeadline)
    {
        const string message = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        FakeTimeProvider time = new();
        FcShellOptions options = new() { MaxPendingCommandPollingDurationMs = 120_000 };
        using PendingCommandStateService state = new(Microsoft.Extensions.Options.Options.Create(options),
            CreateLifecycle(), new FixedUserContext(), time);
        state.Register(Registration(CorrelationId, message, time.GetUtcNow()));
        TaskCompletionSource<PendingCommandOutcomeObservation?> held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken queryToken = default;
        IPendingCommandStatusQuery query = new CallbackStatusQuery((_, token) => {
            queryToken = token;
            return new ValueTask<PendingCommandOutcomeObservation?>(held.Task);
        });
        PendingCommandPollingCoordinator sut = new(state, new PendingCommandOutcomeResolver(state), query,
            Microsoft.Extensions.Options.Options.Create(options), timeProvider: time);

        Task<int> tick = sut.PollOnceAsync(TestContext.Current.CancellationToken);
        queryToken.CanBeCanceled.ShouldBeTrue();
        time.Advance(TimeSpan.FromMilliseconds(119_999));
        tick.IsCompleted.ShouldBeFalse();
        time.Advance(TimeSpan.FromMilliseconds(1));
        // The state service and poller can race to close the same deadline; only
        // the stored first terminal matters here.
        _ = await tick.ConfigureAwait(true);
        state.GetByMessageId(message).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.DegradedExhausted);
        // Cleanup owns the token only for its bounded cancellation phase, not for
        // the provider's potentially unbounded task lifetime.
        SpinWait.SpinUntil(() => {
            try { _ = queryToken.WaitHandle; return false; }
            catch (ObjectDisposedException) { return true; }
        }, TimeSpan.FromSeconds(2)).ShouldBeTrue();

        if (faultAfterDeadline) { held.SetException(new InvalidOperationException("late provider fault")); }
        else { held.SetResult(new(PendingCommandOutcomeSource.FallbackPolling,
            PendingCommandTerminalOutcome.Confirmed, MessageId: message)); }
        await Task.Yield();
        state.GetByMessageId(message).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.DegradedExhausted);
        (await sut.PollOnceAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
    }

    [Fact]
    public async Task BudgetOneAlternatesTransportAndTerminalConvergenceWhilePendingPersists()
    {
        const string terminalMessage = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        const string pendingMessage = "01BRZ3NDEKTSV4RRFFQ69G5FAV";
        FakeTimeProvider time = new();
        FcShellOptions options = new() { MaxPendingCommandPollingPerTick = 1 };
        bool acceptLifecycle = false;
        CommandLifecycleState lifecycleState = CommandLifecycleState.Idle;
        string? lifecycleMessage = null;
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        lifecycle.GetState(CorrelationId).Returns(_ => lifecycleState);
        lifecycle.GetMessageId(CorrelationId).Returns(_ => lifecycleMessage);
        lifecycle.When(x => x.Transition(CorrelationId, Arg.Any<CommandLifecycleState>(), Arg.Any<string?>(), Arg.Any<bool>()))
            .Do(call => {
                if (acceptLifecycle) {
                    lifecycleState = call.ArgAt<CommandLifecycleState>(1);
                    lifecycleMessage = call.ArgAt<string?>(2);
                }
            });
        using PendingCommandStateService state = new(Microsoft.Extensions.Options.Options.Create(options),
            lifecycle, new FixedUserContext(), time);
        state.Register(Registration(CorrelationId, terminalMessage, time.GetUtcNow()));
        state.ResolveTerminal(PendingCommandTerminalObservation.Confirmed(terminalMessage)).Status
            .ShouldBe(PendingCommandResolutionStatus.LifecycleDispatchFailed);
        state.Register(Registration(SecondCorrelationId, pendingMessage, time.GetUtcNow()));
        acceptLifecycle = true;
        int transportQueries = 0;
        IPendingCommandStatusQuery query = new CallbackStatusQuery((_, _) => {
            transportQueries++;
            return ValueTask.FromResult<PendingCommandOutcomeObservation?>(null);
        });
        PendingCommandPollingCoordinator sut = new(state, new PendingCommandOutcomeResolver(state), query,
            Microsoft.Extensions.Options.Options.Create(options), timeProvider: time);

        (await sut.PollOnceAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
        transportQueries.ShouldBe(1);
        (await sut.PollOnceAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
        lifecycleState.ShouldBe(CommandLifecycleState.Confirmed);
        lifecycleMessage.ShouldBe(terminalMessage);
        transportQueries.ShouldBe(1);
        (await sut.PollOnceAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
        transportQueries.ShouldBe(2);
        state.GetByMessageId(pendingMessage).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Pending);
    }

    [Fact]
    public async Task LaterAcceptedCommandUsesNextTickWhileFirstQueryIsHeld()
    {
        FakeTimeProvider time = new();
        FcShellOptions options = new() { MaxPendingCommandPollingPerTick = 2 };
        using PendingCommandStateService state = new(Microsoft.Extensions.Options.Options.Create(options),
            CreateLifecycle(), new FixedUserContext(), time);
        const string first = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        const string second = "01BRZ3NDEKTSV4RRFFQ69G5FAV";
        state.Register(Registration(CorrelationId, first, time.GetUtcNow()));
        TaskCompletionSource<PendingCommandOutcomeObservation?> held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int firstQueries = 0;
        IPendingCommandStatusQuery query = new CallbackStatusQuery((entry, _) => entry.MessageId == first
            ? FirstQuery()
            : ValueTask.FromResult<PendingCommandOutcomeObservation?>(new(
                PendingCommandOutcomeSource.FallbackPolling, PendingCommandTerminalOutcome.Confirmed,
                MessageId: second)));
        ValueTask<PendingCommandOutcomeObservation?> FirstQuery()
        {
            Interlocked.Increment(ref firstQueries);
            return new ValueTask<PendingCommandOutcomeObservation?>(held.Task);
        }
        PendingCommandPollingCoordinator polling = new(state, new PendingCommandOutcomeResolver(state), query,
            Microsoft.Extensions.Options.Options.Create(options), timeProvider: time);

        Task<int> firstTick = polling.PollOnceAsync(TestContext.Current.CancellationToken);
        state.Register(Registration(SecondCorrelationId, second, time.GetUtcNow()));
        (await polling.PollOnceAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBe(1);
        state.GetByMessageId(second).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Confirmed);
        firstQueries.ShouldBe(1);
        held.TrySetResult(null);
        (await firstTick.ConfigureAwait(true)).ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PollingRejectsResolverWithoutCapturedEntryCapability(bool delegated)
    {
        FakeTimeProvider time = new();
        FcShellOptions options = new();
        using PendingCommandStateService state = new(Microsoft.Extensions.Options.Options.Create(options),
            CreateLifecycle(), new FixedUserContext(), time);
        const string message = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        state.Register(Registration(CorrelationId, message, time.GetUtcNow()));
        PendingCommandEntry original = state.GetByMessageId(message).ShouldNotBeNull();
        IPendingCommandOutcomeResolver unsafeResolver = Substitute.For<IPendingCommandOutcomeResolver>();
        unsafeResolver.Resolve(Arg.Any<PendingCommandOutcomeObservation>()).Returns(call =>
        {
            // A resolver allowed past the separate precheck could replace and close this identity.
            state.Clear("replacement during resolve");
            state.Register(Registration(SecondCorrelationId, message, time.GetUtcNow()));
            state.ResolveTerminal(PendingCommandTerminalObservation.Confirmed(message));
            return new PendingCommandOutcomeResolutionResult(PendingCommandOutcomeResolutionStatus.Resolved);
        });
        IPendingCommandOutcomeResolver resolver = delegated
            ? new PendingCommandOutcomeResolver(unsafeResolver, state, null, null, time, null, null)
            : unsafeResolver;
        IPendingCommandStatusQuery query = new CallbackStatusQuery((pendingCommand, cancellationToken) =>
            ValueTask.FromResult<PendingCommandOutcomeObservation?>(new(
                PendingCommandOutcomeSource.FallbackPolling, PendingCommandTerminalOutcome.Confirmed,
                MessageId: message)));
        InvalidOperationException error = Should.Throw<InvalidOperationException>(() => new PendingCommandPollingCoordinator(
            state, resolver, query, Microsoft.Extensions.Options.Options.Create(options), timeProvider: time));
        error.Message.ShouldContain(nameof(IPendingCommandCapturedEntryResolver));
        unsafeResolver.DidNotReceive().Resolve(Arg.Any<PendingCommandOutcomeObservation>());
        state.GetByMessageId(message).ShouldBeSameAs(original);
        original.Status.ShouldBe(PendingCommandStatus.Pending);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CustomCapturedEntryResolverClosesTheOriginalPendingCommand(bool delegated)
    {
        FakeTimeProvider time = new();
        FcShellOptions options = new();
        using PendingCommandStateService state = new(Microsoft.Extensions.Options.Options.Create(options),
            CreateLifecycle(), new FixedUserContext(), time);
        const string message = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        state.Register(Registration(CorrelationId, message, time.GetUtcNow()));
        PendingCommandEntry original = state.GetByMessageId(message).ShouldNotBeNull();
        IPendingCommandCapturedEntryResolver custom = Substitute.For<IPendingCommandCapturedEntryResolver>();
        custom.ResolveForEntry(Arg.Any<PendingCommandOutcomeObservation>(), Arg.Any<PendingCommandEntry>())
            .Returns(call => {
                PendingCommandOutcomeObservation observation = call.ArgAt<PendingCommandOutcomeObservation>(0);
                PendingCommandEntry expected = call.ArgAt<PendingCommandEntry>(1);
                return PendingCommandOutcomeResolutionResult.From(state.ResolveTerminalIfCurrent(
                    new PendingCommandTerminalObservation(observation.MessageId!, observation.Outcome), expected));
            });
        IPendingCommandOutcomeResolver resolver = delegated
            ? new PendingCommandOutcomeResolver(custom, state, null, null, time, null, null)
            : custom;
        IPendingCommandStatusQuery query = new CallbackStatusQuery((_, _) =>
            ValueTask.FromResult<PendingCommandOutcomeObservation?>(new(
                PendingCommandOutcomeSource.FallbackPolling, PendingCommandTerminalOutcome.Confirmed,
                MessageId: message)));
        PendingCommandPollingCoordinator polling = new(state, resolver, query,
            Microsoft.Extensions.Options.Options.Create(options), timeProvider: time);

        (await polling.PollOnceAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
        custom.Received(1).ResolveForEntry(Arg.Any<PendingCommandOutcomeObservation>(), original);
        state.GetByMessageId(message).ShouldNotBeSameAs(original);
        state.GetByMessageId(message)!.Status.ShouldBe(PendingCommandStatus.Confirmed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CustomCapturedEntryResolverCannotCloseReplacementWithTheSameMessageId(bool delegated)
    {
        FakeTimeProvider time = new();
        FcShellOptions options = new();
        using PendingCommandStateService state = new(Microsoft.Extensions.Options.Options.Create(options),
            CreateLifecycle(), new FixedUserContext(), time);
        const string message = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        state.Register(Registration(CorrelationId, message, time.GetUtcNow()));
        IPendingCommandCapturedEntryResolver custom = Substitute.For<IPendingCommandCapturedEntryResolver>();
        custom.ResolveForEntry(Arg.Any<PendingCommandOutcomeObservation>(), Arg.Any<PendingCommandEntry>())
            .Returns(call => PendingCommandOutcomeResolutionResult.From(state.ResolveTerminalIfCurrent(
                PendingCommandTerminalObservation.Confirmed(message), call.ArgAt<PendingCommandEntry>(1))));
        IPendingCommandOutcomeResolver resolver = delegated
            ? new PendingCommandOutcomeResolver(custom, state, null, null, time, null, null)
            : custom;
        TaskCompletionSource<PendingCommandOutcomeObservation?> held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IPendingCommandStatusQuery query = new CallbackStatusQuery((_, _) => new(held.Task));
        PendingCommandPollingCoordinator polling = new(state, resolver, query,
            Microsoft.Extensions.Options.Options.Create(options), timeProvider: time);
        Task<int> poll = polling.PollOnceAsync(TestContext.Current.CancellationToken);

        state.Clear("scope reset");
        state.Register(Registration(SecondCorrelationId, message, time.GetUtcNow()));
        PendingCommandEntry replacement = state.GetByMessageId(message).ShouldNotBeNull();
        held.SetResult(new PendingCommandOutcomeObservation(PendingCommandOutcomeSource.FallbackPolling,
            PendingCommandTerminalOutcome.Confirmed, MessageId: message));

        (await poll.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).ShouldBe(0);
        custom.DidNotReceive().ResolveForEntry(Arg.Any<PendingCommandOutcomeObservation>(), Arg.Any<PendingCommandEntry>());
        state.GetByMessageId(message).ShouldBeSameAs(replacement);
        replacement.Status.ShouldBe(PendingCommandStatus.Pending);
    }

    [Fact]
    public async Task CapturedResolverCanUseApplicationStateAdapterWithoutLosingTerminalEvidence()
    {
        FakeTimeProvider time = new();
        FcShellOptions options = new();
        using PendingCommandStateService backing = new(Microsoft.Extensions.Options.Options.Create(options),
            CreateLifecycle(), new FixedUserContext(), time);
        const string message = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        backing.Register(Registration(CorrelationId, message, time.GetUtcNow()));
        IPendingCommandStateService state = Substitute.For<IPendingCommandStateService>();
        state.Snapshot().Returns(_ => backing.Snapshot());
        state.GetByMessageId(message).Returns(_ => backing.GetByMessageId(message));
        IPendingCommandCapturedEntryResolver custom = Substitute.For<IPendingCommandCapturedEntryResolver>();
        custom.ResolveForEntry(Arg.Any<PendingCommandOutcomeObservation>(), Arg.Any<PendingCommandEntry>())
            .Returns(call => PendingCommandOutcomeResolutionResult.From(backing.ResolveTerminalIfCurrent(
                PendingCommandTerminalObservation.Confirmed(message), call.ArgAt<PendingCommandEntry>(1))));
        PendingCommandOutcomeResolver adapted = new(custom, state, null, null, time, null, null);
        IPendingCommandStatusQuery query = new CallbackStatusQuery((_, _) =>
            ValueTask.FromResult<PendingCommandOutcomeObservation?>(new(
                PendingCommandOutcomeSource.FallbackPolling, PendingCommandTerminalOutcome.Confirmed,
                MessageId: message)));
        Should.Throw<InvalidOperationException>(() => new PendingCommandPollingCoordinator(state,
            new PendingCommandOutcomeResolver(state), query, Microsoft.Extensions.Options.Options.Create(options),
            timeProvider: time)).Message.ShouldContain(nameof(IPendingCommandCapturedEntryResolver));
        PendingCommandPollingCoordinator polling = new(state, adapted, query,
            Microsoft.Extensions.Options.Options.Create(options), timeProvider: time);

        (await polling.PollOnceAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
        backing.GetByMessageId(message)!.Status.ShouldBe(PendingCommandStatus.Confirmed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OldStatusQueryCannotResolveReplacementWithTheSameMessageId(bool timeout)
    {
        FakeTimeProvider time = new();
        FcShellOptions options = new() { MaxPendingCommandPollingDurationMs = 30_000 };
        ILifecycleStateService lifecycle = CreateLifecycle();
        using PendingCommandStateService state = new(Microsoft.Extensions.Options.Options.Create(options),
            lifecycle, new FixedUserContext(), time);
        const string message = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        state.Register(Registration(CorrelationId, message, time.GetUtcNow()));
        TaskCompletionSource<PendingCommandOutcomeObservation?> held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IPendingCommandStatusQuery query = new CallbackStatusQuery((pendingCommand, cancellationToken) => new(held.Task));
        PendingCommandPollingCoordinator polling = new(state, new PendingCommandOutcomeResolver(state), query,
            Microsoft.Extensions.Options.Options.Create(options), timeProvider: time);
        Task<int> poll = polling.PollOnceAsync(TestContext.Current.CancellationToken);

        time.Advance(TimeSpan.FromSeconds(1));
        state.Clear("scope reset");
        state.Register(Registration(SecondCorrelationId, message, time.GetUtcNow()));
        PendingCommandEntry replacement = state.GetByMessageId(message).ShouldNotBeNull();
        if (timeout)
        {
            time.Advance(TimeSpan.FromMilliseconds(29_000));
        }
        else
        {
            held.SetResult(new PendingCommandOutcomeObservation(PendingCommandOutcomeSource.FallbackPolling,
                PendingCommandTerminalOutcome.Confirmed, MessageId: message));
        }

        (await poll.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).ShouldBe(0);
        state.GetByMessageId(message).ShouldBeSameAs(replacement);
        replacement.Status.ShouldBe(PendingCommandStatus.Pending);
        lifecycle.DidNotReceive().Transition(SecondCorrelationId, CommandLifecycleState.Confirmed, message, false);
        lifecycle.DidNotReceive().Transition(SecondCorrelationId, CommandLifecycleState.DegradedExhausted, message, false);
        held.TrySetResult(null);
    }

    [Fact]
    public async Task HeldOldestStatusQueryDoesNotStarveLaterEligibleEntry()
    {
        FakeTimeProvider time = new();
        FcShellOptions options = new() { MaxPendingCommandPollingPerTick = 2 };
        using PendingCommandStateService state = new(Microsoft.Extensions.Options.Options.Create(options),
            CreateLifecycle(), new FixedUserContext(), time);
        const string first = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        const string second = "01BRZ3NDEKTSV4RRFFQ69G5FAV";
        state.Register(Registration(CorrelationId, first, time.GetUtcNow()));
        time.Advance(TimeSpan.FromMilliseconds(1));
        state.Register(Registration(SecondCorrelationId, second, time.GetUtcNow()));
        TaskCompletionSource<PendingCommandOutcomeObservation?> held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource laterResolved = new(TaskCreationOptions.RunContinuationsAsynchronously);
        state.Changed += (_, _) =>
        {
            if (state.GetByMessageId(second)?.Status == PendingCommandStatus.Confirmed)
            {
                laterResolved.TrySetResult();
            }
        };
        IPendingCommandStatusQuery query = new CallbackStatusQuery((entry, cancellationToken) =>
            entry.MessageId == first
                ? new ValueTask<PendingCommandOutcomeObservation?>(held.Task)
                : ValueTask.FromResult<PendingCommandOutcomeObservation?>(new(
                    PendingCommandOutcomeSource.FallbackPolling, PendingCommandTerminalOutcome.Confirmed,
                    MessageId: second)));
        PendingCommandPollingCoordinator polling = new(state, new PendingCommandOutcomeResolver(state), query,
            Microsoft.Extensions.Options.Options.Create(options), timeProvider: time);

        Task<int> poll = polling.PollOnceAsync(TestContext.Current.CancellationToken);
        await laterResolved.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        state.GetByMessageId(first).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Pending);
        held.SetResult(null);
        (await poll.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Theory]
    [InlineData(120000)]
    [InlineData(30000)]
    public async Task HeaderDeadlineClosesTheQuietIntervalAfterANonterminalQuery(int duration)
    {
        FakeTimeProvider time = new();
        FcShellOptions options = new() { MaxPendingCommandPollingDurationMs = duration };
        ILifecycleStateService lifecycle = CreateLifecycle();
        PendingCommandStateService state = new(Microsoft.Extensions.Options.Options.Create(options), lifecycle, new FixedUserContext(), time);
        const string message = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        DateTimeOffset headers = time.GetUtcNow();
        time.Advance(TimeSpan.FromSeconds(2));
        state.Register(Registration(CorrelationId, message, headers));
        HeldPendingCommandStatusQuery query = new();
        query.Completion.SetResult(null);
        PendingCommandPollingCoordinator polling = new(state, new PendingCommandOutcomeResolver(state), query,
            Microsoft.Extensions.Options.Options.Create(options), timeProvider: time);
        time.Advance(TimeSpan.FromMilliseconds(duration - 2001));
        (await polling.PollOnceAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
        state.GetByMessageId(message).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Pending);
        CommandExecutionAdmissionGate admission = new(state, time);
        using (CommandExecutionAdmission blocked = admission.TryAcquire(new("Counter.Next")))
        {
            blocked.IsAdmitted.ShouldBeFalse();
        }
        time.Advance(TimeSpan.FromMilliseconds(1));
        state.Snapshot().Count(entry => entry.Status == PendingCommandStatus.Pending).ShouldBe(0);
        state.GetByMessageId(message).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.DegradedExhausted);
        lifecycle.Received(1).Transition(CorrelationId, CommandLifecycleState.DegradedExhausted, message, false);
        (await polling.PollOnceAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
        query.Calls.ShouldBe(1);
        using (CommandExecutionAdmission next = admission.TryAcquire(new("Counter.Next")))
        {
            next.IsAdmitted.ShouldBeTrue();
        }
        state.ResolveTerminal(PendingCommandTerminalObservation.Confirmed(message)).Status.ShouldBe(PendingCommandResolutionStatus.DuplicateIgnored);
        state.GetByMessageId(message).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.DegradedExhausted);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HeldQueryPreservesCallerCancellationAndFatalPropagation(bool fatal)
    {
        FakeTimeProvider time = new();
        FcShellOptions options = new();
        PendingCommandStateService state = new(Microsoft.Extensions.Options.Options.Create(options), CreateLifecycle(), new FixedUserContext(), time);
        const string message = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        state.Register(Registration(CorrelationId, message, time.GetUtcNow()));
        HeldPendingCommandStatusQuery query = new();
        PendingCommandPollingCoordinator polling = new(state, new PendingCommandOutcomeResolver(state), query,
            Microsoft.Extensions.Options.Options.Create(options), timeProvider: time);
        using CancellationTokenSource cancellation = new();
        Task<int> poll = polling.PollOnceAsync(cancellation.Token);
        if (fatal)
        {
            query.Completion.TrySetException((OutOfMemoryException)Activator.CreateInstance(typeof(OutOfMemoryException), "fatal test provider")!);
            await Should.ThrowAsync<OutOfMemoryException>(() => poll);
        }
        else
        {
            await cancellation.CancelAsync();
            await Should.ThrowAsync<OperationCanceledException>(() => poll);
            query.Completion.TrySetResult(null);
        }
        if (!fatal)
        {
            await query.CancellationFinished.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }
        state.GetByMessageId(message).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Pending);
    }

    private sealed class FixedUserContext : IUserContextAccessor {
        public string TenantId => "tenant-a";
        public string UserId => "user-a";
    }

    private const string CorrelationId = "01CPZ3NDEKTSV4RRFFQ69G5FAV";
    private const string SecondCorrelationId = "01DPZ3NDEKTSV4RRFFQ69G5FAV";

    [Fact]
    public async Task PollOnce_ProcessesPendingCommandsOldestFirstWithinCap() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        ILifecycleStateService lifecycle = CreateLifecycle();
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 1 }),
            lifecycle,
            new FixedUserContext(),
            time,
            NullLogger<PendingCommandStateService>.Instance);
        state.Register(Registration(CorrelationId, "01ARZ3NDEKTSV4RRFFQ69G5FAV", time.GetUtcNow()));
        time.Advance(TimeSpan.FromSeconds(1));
        state.Register(Registration(SecondCorrelationId, "01BRZ3NDEKTSV4RRFFQ69G5FAV", time.GetUtcNow()));

        IPendingCommandStatusQuery query = Substitute.For<IPendingCommandStatusQuery>();
#pragma warning disable CA2012 // NSubstitute owns the callback-produced ValueTask and the coordinator awaits it once.
        query.QueryAsync(Arg.Any<PendingCommandEntry>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                PendingCommandEntry entry = call[0] as PendingCommandEntry
                    ?? throw new InvalidOperationException("Expected a pending command entry argument.");
                return ValueTask.FromResult<PendingCommandOutcomeObservation?>(new(
                    PendingCommandOutcomeSource.FallbackPolling,
                    PendingCommandTerminalOutcome.Confirmed,
                    entry.MessageId));
            });
#pragma warning restore CA2012

        PendingCommandPollingCoordinator sut = new(
            state,
            new PendingCommandOutcomeResolver(state),
            query,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 1 }),
            NullLogger<PendingCommandPollingCoordinator>.Instance,
            time);

        int processed = await sut.PollOnceAsync(TestContext.Current.CancellationToken);

        processed.ShouldBe(1);
        state.GetByMessageId("01ARZ3NDEKTSV4RRFFQ69G5FAV")!.Status.ShouldBe(PendingCommandStatus.Confirmed);
        state.GetByMessageId("01BRZ3NDEKTSV4RRFFQ69G5FAV")!.Status.ShouldBe(PendingCommandStatus.Pending);
    }

    [Fact]
    public async Task PollOnce_ZeroCapDisablesPendingPolling() {
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 0 }),
            CreateLifecycle(),
            new FixedUserContext(),
            new FakeTimeProvider(),
            NullLogger<PendingCommandStateService>.Instance);
        state.Register(Registration(CorrelationId, "01ARZ3NDEKTSV4RRFFQ69G5FAV", DateTimeOffset.UtcNow));

        IPendingCommandStatusQuery query = Substitute.For<IPendingCommandStatusQuery>();
        PendingCommandPollingCoordinator sut = new(
            state,
            new PendingCommandOutcomeResolver(state),
            query,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 0 }),
            NullLogger<PendingCommandPollingCoordinator>.Instance,
            new FakeTimeProvider());

        int processed = await sut.PollOnceAsync(TestContext.Current.CancellationToken);

        processed.ShouldBe(0);
        await query.DidNotReceiveWithAnyArgs().QueryAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task PollOnce_LifecycleConvergenceRunsBeforeAndWithoutStatusTransport() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
        CommandLifecycleState lifecycleState = CommandLifecycleState.Idle;
        string? lifecycleMessageId = null;
        bool applyTransition = false;
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        lifecycle.GetState(CorrelationId).Returns(_ => lifecycleState);
        lifecycle.GetMessageId(CorrelationId).Returns(_ => lifecycleMessageId);
        lifecycle
            .When(x => x.Transition(CorrelationId, Arg.Any<CommandLifecycleState>(), Arg.Any<string?>(), Arg.Any<bool>()))
            .Do(call => {
                if (!applyTransition) {
                    return;
                }

                lifecycleState = call.ArgAt<CommandLifecycleState>(1);
                lifecycleMessageId = call.ArgAt<string?>(2);
            });
        FcShellOptions options = new() { MaxPendingCommandPollingPerTick = 1 };
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(options),
            lifecycle,
            new FixedUserContext(),
            time,
            NullLogger<PendingCommandStateService>.Instance);
        state.Register(Registration(CorrelationId, "01ARZ3NDEKTSV4RRFFQ69G5FAV", time.GetUtcNow()));
        state.ResolveTerminal(PendingCommandTerminalObservation.Confirmed("01ARZ3NDEKTSV4RRFFQ69G5FAV")).Status
            .ShouldBe(PendingCommandResolutionStatus.LifecycleDispatchFailed);
        applyTransition = true;
        IPendingCommandStatusQuery query = Substitute.For<IPendingCommandStatusQuery>();
        PendingCommandPollingCoordinator sut = new(
            state,
            new PendingCommandOutcomeResolver(state),
            query,
            Microsoft.Extensions.Options.Options.Create(options),
            NullLogger<PendingCommandPollingCoordinator>.Instance,
            time);

        int processed = await sut.PollOnceAsync(TestContext.Current.CancellationToken);

        processed.ShouldBe(1);
        lifecycleState.ShouldBe(CommandLifecycleState.Confirmed);
        lifecycleMessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        await query.DidNotReceiveWithAnyArgs().QueryAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task PollOnce_FailingConvergenceItemIsAttemptedOnceAndDoesNotStarveTransport() {
        const string failedMessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        const string pendingMessageId = "01BRZ3NDEKTSV4RRFFQ69G5FAV";
        FakeTimeProvider time = new(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
        Dictionary<string, (CommandLifecycleState State, string? MessageId)> values = new(StringComparer.Ordinal);
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        lifecycle.GetState(Arg.Any<string>()).Returns(call =>
            values.TryGetValue(call.ArgAt<string>(0), out var value) ? value.State : CommandLifecycleState.Idle);
        lifecycle.GetMessageId(Arg.Any<string>()).Returns(call =>
            values.TryGetValue(call.ArgAt<string>(0), out var value) ? value.MessageId : null);
        lifecycle.When(service => service.Transition(
                SecondCorrelationId,
                Arg.Any<CommandLifecycleState>(),
                Arg.Any<string?>(),
                Arg.Any<bool>()))
            .Do(call => values[SecondCorrelationId] = (
                call.ArgAt<CommandLifecycleState>(1),
                call.ArgAt<string?>(2)));
        FcShellOptions options = new() { MaxPendingCommandPollingPerTick = 2 };
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(options),
            lifecycle,
            new FixedUserContext(),
            time,
            NullLogger<PendingCommandStateService>.Instance);
        state.Register(Registration(CorrelationId, failedMessageId, time.GetUtcNow()));
        state.ResolveTerminal(PendingCommandTerminalObservation.Confirmed(failedMessageId)).Status
            .ShouldBe(PendingCommandResolutionStatus.LifecycleDispatchFailed);
        state.Register(Registration(SecondCorrelationId, pendingMessageId, time.GetUtcNow()));
        RecordingFixedStatusQuery query = new(pendingMessageId);
        PendingCommandPollingCoordinator sut = new(
            state,
            new PendingCommandOutcomeResolver(state),
            query,
            Microsoft.Extensions.Options.Options.Create(options),
            NullLogger<PendingCommandPollingCoordinator>.Instance,
            time);

        int processed = await sut.PollOnceAsync(TestContext.Current.CancellationToken);

        processed.ShouldBe(1);
        state.GetByMessageId(pendingMessageId).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Confirmed);
        lifecycle.Received(2).Transition(CorrelationId, CommandLifecycleState.Confirmed, failedMessageId, false);
        query.MessageIds.ShouldBe([pendingMessageId]);
    }

    [Fact]
    public async Task PollOnce_SaturatedConvergenceWithMinimumBudget_ReservesTransportCapacity() {
        const string failedMessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        const string pendingMessageId = "01BRZ3NDEKTSV4RRFFQ69G5FAV";
        FakeTimeProvider time = new(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
        Dictionary<string, (CommandLifecycleState State, string? MessageId)> values = new(StringComparer.Ordinal);
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        lifecycle.GetState(Arg.Any<string>()).Returns(call =>
            values.TryGetValue(call.ArgAt<string>(0), out var value) ? value.State : CommandLifecycleState.Idle);
        lifecycle.GetMessageId(Arg.Any<string>()).Returns(call =>
            values.TryGetValue(call.ArgAt<string>(0), out var value) ? value.MessageId : null);
        lifecycle.When(service => service.Transition(
                SecondCorrelationId,
                Arg.Any<CommandLifecycleState>(),
                Arg.Any<string?>(),
                Arg.Any<bool>()))
            .Do(call => values[SecondCorrelationId] = (
                call.ArgAt<CommandLifecycleState>(1),
                call.ArgAt<string?>(2)));
        FcShellOptions options = new() { MaxPendingCommandPollingPerTick = 1 };
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(options),
            lifecycle,
            new FixedUserContext(),
            time,
            NullLogger<PendingCommandStateService>.Instance);
        state.Register(Registration(CorrelationId, failedMessageId, time.GetUtcNow()));
        state.ResolveTerminal(PendingCommandTerminalObservation.Confirmed(failedMessageId)).Status
            .ShouldBe(PendingCommandResolutionStatus.LifecycleDispatchFailed);
        state.Register(Registration(SecondCorrelationId, pendingMessageId, time.GetUtcNow()));
        RecordingFixedStatusQuery query = new(pendingMessageId);
        PendingCommandPollingCoordinator sut = new(
            state,
            new PendingCommandOutcomeResolver(state),
            query,
            Microsoft.Extensions.Options.Options.Create(options),
            NullLogger<PendingCommandPollingCoordinator>.Instance,
            time);

        int processed = await sut.PollOnceAsync(TestContext.Current.CancellationToken);

        processed.ShouldBe(1);
        state.GetByMessageId(pendingMessageId).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Confirmed);
        lifecycle.Received(1).Transition(CorrelationId, CommandLifecycleState.Confirmed, failedMessageId, false);
        query.MessageIds.ShouldBe([pendingMessageId]);
    }

    [Fact]
    public async Task PollOnce_ExpiredConvergenceDropsWithoutRequeueAndPermitsTransport() {
        const string expiredMessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        const string pendingMessageId = "01BRZ3NDEKTSV4RRFFQ69G5FAV";
        FakeTimeProvider time = new(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
        Dictionary<string, (CommandLifecycleState State, string? MessageId)> values = new(StringComparer.Ordinal);
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        lifecycle.GetState(Arg.Any<string>()).Returns(call =>
            values.TryGetValue(call.ArgAt<string>(0), out var value) ? value.State : CommandLifecycleState.Idle);
        lifecycle.GetMessageId(Arg.Any<string>()).Returns(call =>
            values.TryGetValue(call.ArgAt<string>(0), out var value) ? value.MessageId : null);
        lifecycle.When(service => service.Transition(
                SecondCorrelationId,
                Arg.Any<CommandLifecycleState>(),
                Arg.Any<string?>(),
                Arg.Any<bool>()))
            .Do(call => values[SecondCorrelationId] = (
                call.ArgAt<CommandLifecycleState>(1),
                call.ArgAt<string?>(2)));
        FcShellOptions options = new() {
            MaxPendingCommandPollingPerTick = 2,
            MaxPendingCommandPollingDurationMs = 1_000,
        };
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(options),
            lifecycle,
            new FixedUserContext(),
            time,
            NullLogger<PendingCommandStateService>.Instance);
        state.Register(Registration(CorrelationId, expiredMessageId, time.GetUtcNow()));
        state.ResolveTerminal(PendingCommandTerminalObservation.Confirmed(expiredMessageId)).Status
            .ShouldBe(PendingCommandResolutionStatus.LifecycleDispatchFailed);
        time.Advance(TimeSpan.FromMilliseconds(1_001));
        state.Register(Registration(SecondCorrelationId, pendingMessageId, time.GetUtcNow()));
        RecordingFixedStatusQuery query = new(pendingMessageId);
        PendingCommandPollingCoordinator sut = new(
            state,
            new PendingCommandOutcomeResolver(state),
            query,
            Microsoft.Extensions.Options.Options.Create(options),
            NullLogger<PendingCommandPollingCoordinator>.Instance,
            time);

        int processed = await sut.PollOnceAsync(TestContext.Current.CancellationToken);
        _ = await sut.PollOnceAsync(TestContext.Current.CancellationToken);

        processed.ShouldBe(1);
        lifecycle.Received(1).Transition(CorrelationId, CommandLifecycleState.Confirmed, expiredMessageId, false);
        query.MessageIds.ShouldBe([pendingMessageId]);
    }

    [Fact]
    public async Task PollOnce_SkipsAlreadyResolvedEntriesMidTick() {
        // P16 — live nudge resolves entries between snapshot capture and per-entry query; coordinator
        // should re-check the entry status to skip wasted HTTP calls.
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        ILifecycleStateService lifecycle = CreateLifecycle();
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 5 }),
            lifecycle,
            new FixedUserContext(),
            time,
            NullLogger<PendingCommandStateService>.Instance);
        state.Register(Registration(CorrelationId, "01ARZ3NDEKTSV4RRFFQ69G5FAV", time.GetUtcNow()));

        // Resolve the only pending entry BEFORE the coordinator queries.
        state.ResolveTerminal(PendingCommandTerminalObservation.Confirmed("01ARZ3NDEKTSV4RRFFQ69G5FAV"));

        IPendingCommandStatusQuery query = Substitute.For<IPendingCommandStatusQuery>();
        PendingCommandPollingCoordinator sut = new(
            state,
            new PendingCommandOutcomeResolver(state),
            query,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 5 }),
            NullLogger<PendingCommandPollingCoordinator>.Instance,
            time);

        int processed = await sut.PollOnceAsync(TestContext.Current.CancellationToken);

        processed.ShouldBe(0);
        await query.DidNotReceiveWithAnyArgs().QueryAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task PollOnce_StatusQueryThrows_LogsAndContinues() {
        // P9 — polling-coordinator catch should preserve stack trace in the log and not swallow
        // OperationCanceledException issued from the user-supplied cancellation.
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        ILifecycleStateService lifecycle = CreateLifecycle();
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 5 }),
            lifecycle,
            new FixedUserContext(),
            time,
            NullLogger<PendingCommandStateService>.Instance);
        state.Register(Registration(CorrelationId, "01ARZ3NDEKTSV4RRFFQ69G5FAV", time.GetUtcNow()));
        state.Register(Registration(SecondCorrelationId, "01BRZ3NDEKTSV4RRFFQ69G5FAV", time.GetUtcNow()));

        IPendingCommandStatusQuery query = Substitute.For<IPendingCommandStatusQuery>();
#pragma warning disable CA2012 // NSubstitute owns the callback-produced ValueTask and the coordinator awaits it once.
        query.QueryAsync(Arg.Any<PendingCommandEntry>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<PendingCommandOutcomeObservation?>>(call => {
                PendingCommandEntry entry = call[0] as PendingCommandEntry
                    ?? throw new InvalidOperationException("Expected a pending command entry argument.");
                return entry.MessageId == "01ARZ3NDEKTSV4RRFFQ69G5FAV"
                    ? throw new InvalidOperationException("simulated transient failure")
                    : ValueTask.FromResult<PendingCommandOutcomeObservation?>(new(
                        PendingCommandOutcomeSource.FallbackPolling,
                        PendingCommandTerminalOutcome.Confirmed,
                        entry.MessageId));
            });
#pragma warning restore CA2012

        PendingCommandPollingCoordinator sut = new(
            state,
            new PendingCommandOutcomeResolver(state),
            query,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 5 }),
            NullLogger<PendingCommandPollingCoordinator>.Instance,
            time);

        // Failure on the first entry must not abort the loop; the second entry still resolves.
        int processed = await sut.PollOnceAsync(TestContext.Current.CancellationToken);

        processed.ShouldBe(1);
        state.GetByMessageId("01BRZ3NDEKTSV4RRFFQ69G5FAV")!.Status.ShouldBe(PendingCommandStatus.Confirmed);
    }

    [Fact]
    public async Task PollOnce_EventStoreStatusProvider_ResolvesPendingByMessageId() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero));
        ILifecycleStateService lifecycle = CreateLifecycle();
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 5 }),
            lifecycle,
            new FixedUserContext(),
            time,
            NullLogger<PendingCommandStateService>.Instance);
        state.Register(Registration(CorrelationId, "01ARZ3NDEKTSV4RRFFQ69G5FAV", time.GetUtcNow()));

        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent(
                """
                {
                  "correlationId": "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                  "status": "Completed",
                  "statusCode": 4,
                  "timestamp": "2026-06-04T00:00:00Z",
                  "aggregateId": "counter-1",
                  "eventCount": 1,
                  "rejectionEventType": null,
                  "failureReason": null,
                  "timeoutDuration": null
                }
                """,
                Encoding.UTF8,
                "application/json"),
        });
        EventStorePendingCommandStatusQuery query = new(
            new SingleClientFactory(handler),
            Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
                BaseAddress = new Uri("https://eventstore.test"),
                RequireAccessToken = false,
            }),
            EventStoreTestSupport.CreateClassifier(),
            NullLogger<EventStorePendingCommandStatusQuery>.Instance,
            new TestTenantContextAccessor());
        PendingCommandPollingCoordinator sut = new(
            state,
            new PendingCommandOutcomeResolver(state),
            query,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 5 }),
            NullLogger<PendingCommandPollingCoordinator>.Instance,
            time);

        int processed = await sut.PollOnceAsync(TestContext.Current.CancellationToken);

        processed.ShouldBe(1);
        state.GetByMessageId("01ARZ3NDEKTSV4RRFFQ69G5FAV")!.Status.ShouldBe(PendingCommandStatus.Confirmed);
        handler.Requests.Single().RequestUri!.PathAndQuery.ShouldBe("/api/v1/commands/status/01ARZ3NDEKTSV4RRFFQ69G5FAV");
        lifecycle.Received(1).Transition(CorrelationId, CommandLifecycleState.Confirmed, "01ARZ3NDEKTSV4RRFFQ69G5FAV", false);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(-1, false)]
    public async Task PollOnce_EventStoreCompletionPublishesOnlyKnownMaterialTarget(
        int eventCount,
        bool expectIndicator) {
        DateTimeOffset now = new(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);
        FakeTimeProvider time = new(now);
        FcShellOptions options = new() { MaxPendingCommandPollingPerTick = 5 };
        ILifecycleStateService lifecycle = CreateLifecycle();
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(options),
            lifecycle,
            new FixedUserContext(),
            time,
            NullLogger<PendingCommandStateService>.Instance);
        CommandTargetSnapshot target = new(
            "Counter.Count",
            "counter-counts",
            "counter-1",
            CommandTargetChangeKind.Update,
            "Draft",
            "Approved",
            "tenant-1",
            "user-1",
            now.AddSeconds(-1));
        PendingCommandRegistration registration = Registration(
            CorrelationId,
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            now) with { TargetSnapshot = target };
        state.Register(registration).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent(
                $$"""
                {
                  "correlationId": "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                  "status": "Completed",
                  "statusCode": 4,
                  "timestamp": "2026-06-04T12:00:00Z",
                  "aggregateId": "counter-1",
                  "eventCount": {{eventCount}},
                  "rejectionEventType": null,
                  "failureReason": null,
                  "timeoutDuration": null
                }
                """,
                Encoding.UTF8,
                "application/json"),
        });
        EventStorePendingCommandStatusQuery query = new(
            new SingleClientFactory(handler),
            Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
                BaseAddress = new Uri("https://eventstore.test"),
                RequireAccessToken = false,
            }),
            EventStoreTestSupport.CreateClassifier(),
            NullLogger<EventStorePendingCommandStatusQuery>.Instance,
            new TestTenantContextAccessor());
        using NewItemIndicatorStateService indicators = new(time);
        IUserContextAccessor userContext = Substitute.For<IUserContextAccessor>();
        _ = userContext.TenantId.Returns("tenant-1");
        _ = userContext.UserId.Returns("user-1");
        PendingCommandOutcomeResolver resolver = new(
            state,
            NullLogger<PendingCommandOutcomeResolver>.Instance,
            indicators,
            time,
            Microsoft.Extensions.Options.Options.Create(options),
            userContext);
        PendingCommandPollingCoordinator sut = new(
            state,
            resolver,
            query,
            Microsoft.Extensions.Options.Options.Create(options),
            NullLogger<PendingCommandPollingCoordinator>.Instance,
            time);

        int processed = await sut.PollOnceAsync(TestContext.Current.CancellationToken);

        processed.ShouldBe(1);
        state.GetByMessageId("01ARZ3NDEKTSV4RRFFQ69G5FAV")!.Status
            .ShouldBe(PendingCommandStatus.Confirmed);
        if (expectIndicator) {
            NewItemIndicatorEntry indicator = indicators.Snapshot("counter-counts").Single();
            indicator.ViewKey.ShouldBe("counter-counts");
            indicator.EntityKey.ShouldBe("counter-1");
            indicator.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
            indicator.CreatedAt.ShouldBe(now);
        }
        else {
            indicators.Snapshot("counter-counts").ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task PollOnce_ExpiredPendingCommand_ResolvesNeedsReviewWithoutQueryingProvider() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero));
        ILifecycleStateService lifecycle = CreateLifecycle();
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            lifecycle,
            new FixedUserContext(),
            time,
            NullLogger<PendingCommandStateService>.Instance);
        state.Register(Registration(CorrelationId, "01ARZ3NDEKTSV4RRFFQ69G5FAV", time.GetUtcNow()));
        time.Advance(TimeSpan.FromMilliseconds(120_000));

        IPendingCommandStatusQuery query = Substitute.For<IPendingCommandStatusQuery>();
        PendingCommandPollingCoordinator sut = new(
            state,
            new PendingCommandOutcomeResolver(state),
            query,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions {
                MaxPendingCommandPollingDurationMs = 120_000,
            }),
            NullLogger<PendingCommandPollingCoordinator>.Instance,
            time);

        int processed = await sut.PollOnceAsync(TestContext.Current.CancellationToken);

        processed.ShouldBe(0);
        PendingCommandEntry expired = state.GetByMessageId("01ARZ3NDEKTSV4RRFFQ69G5FAV")!;
        expired.Status.ShouldBe(PendingCommandStatus.DegradedExhausted);
        expired.RejectionTitle.ShouldBeNull();
        await query.DidNotReceiveWithAnyArgs().QueryAsync(default!, TestContext.Current.CancellationToken);
        lifecycle.Received(1).Transition(CorrelationId, CommandLifecycleState.DegradedExhausted, "01ARZ3NDEKTSV4RRFFQ69G5FAV", false);
    }

    [Fact]
    public async Task PollOnce_ExpiredNeedsReview_FirstWinsOverLateConfirmedObservation() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero));
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            CreateLifecycle(),
            new FixedUserContext(),
            time,
            NullLogger<PendingCommandStateService>.Instance);
        state.Register(Registration(CorrelationId, "01ARZ3NDEKTSV4RRFFQ69G5FAV", time.GetUtcNow()));
        time.Advance(TimeSpan.FromMilliseconds(120_000));

        PendingCommandPollingCoordinator sut = new(
            state,
            new PendingCommandOutcomeResolver(state),
            Substitute.For<IPendingCommandStatusQuery>(),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions {
                MaxPendingCommandPollingDurationMs = 120_000,
            }),
            NullLogger<PendingCommandPollingCoordinator>.Instance,
            time);

        _ = await sut.PollOnceAsync(TestContext.Current.CancellationToken);
        PendingCommandResolutionResult late = state.ResolveTerminal(PendingCommandTerminalObservation.Confirmed("01ARZ3NDEKTSV4RRFFQ69G5FAV"));

        late.Status.ShouldBe(PendingCommandResolutionStatus.DuplicateIgnored);
        PendingCommandEntry entry = state.GetByMessageId("01ARZ3NDEKTSV4RRFFQ69G5FAV")!;
        entry.Status.ShouldBe(PendingCommandStatus.DegradedExhausted);
        entry.DuplicateTerminalObservations.ShouldBe(1);
    }

    private static PendingCommandRegistration Registration(string correlationId, string messageId, DateTimeOffset submittedAt) =>
        new(
            CorrelationId: correlationId,
            MessageId: messageId,
            CommandTypeName: "Counter.Increment",
            ProjectionTypeName: "Counter.Count",
            LaneKey: "counter-counts",
            EntityKey: "counter-1",
            ExpectedStatusSlot: "Approved",
            PriorStatusSlot: "Draft",
            SubmittedAt: submittedAt);

    private static ILifecycleStateService CreateLifecycle() {
        Dictionary<string, (CommandLifecycleState State, string? MessageId)> values = new(StringComparer.Ordinal);
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        lifecycle.GetState(Arg.Any<string>()).Returns(call =>
            values.TryGetValue(call.ArgAt<string>(0)!, out var value)
                ? value.State
                : CommandLifecycleState.Idle);
        lifecycle.GetMessageId(Arg.Any<string>()).Returns(call =>
            values.TryGetValue(call.ArgAt<string>(0)!, out var value)
                ? value.MessageId
                : null);
        lifecycle
            .When(x => x.Transition(
                Arg.Any<string>(),
                Arg.Any<CommandLifecycleState>(),
                Arg.Any<string?>(),
                Arg.Any<bool>()))
            .Do(call => values[call.ArgAt<string>(0)] = (
                call.ArgAt<CommandLifecycleState>(1),
                call.ArgAt<string?>(2)));
        return lifecycle;
    }

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory {
        public HttpClient CreateClient(string name)
            => new(handler, disposeHandler: false) { BaseAddress = new Uri("https://eventstore.test") };
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Requests.Add(request);
            return Task.FromResult(responseFactory(request));
        }
    }

    private sealed class RecordingFixedStatusQuery(string terminalMessageId) : IPendingCommandStatusQuery {
        public List<string> MessageIds { get; } = [];

        public ValueTask<PendingCommandOutcomeObservation?> QueryAsync(
            PendingCommandEntry pendingCommand,
            CancellationToken cancellationToken = default) {
            MessageIds.Add(pendingCommand.MessageId);
            return ValueTask.FromResult<PendingCommandOutcomeObservation?>(new(
                PendingCommandOutcomeSource.FallbackPolling,
                PendingCommandTerminalOutcome.Confirmed,
                terminalMessageId));
        }
    }

    private sealed class CallbackStatusQuery(
        Func<PendingCommandEntry, CancellationToken, ValueTask<PendingCommandOutcomeObservation?>> query)
        : IPendingCommandStatusQuery {
        public ValueTask<PendingCommandOutcomeObservation?> QueryAsync(
            PendingCommandEntry pendingCommand,
            CancellationToken cancellationToken = default)
            => query(pendingCommand, cancellationToken);
    }
    [Theory]
    [InlineData(120000)]
    [InlineData(120001)]
    public async Task ConfirmationReturningAtThePollingBudgetClosesAsExhausted(int elapsed)
    {
        FakeTimeProvider time = new();
        ILifecycleStateService lifecycle = CreateLifecycle();
        PendingCommandStateService state = new(Microsoft.Extensions.Options.Options.Create(new FcShellOptions()), lifecycle,
            new FixedUserContext(), time, NullLogger<PendingCommandStateService>.Instance);
        const string message = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        state.Register(Registration(CorrelationId, message, time.GetUtcNow()));
        IPendingCommandStatusQuery query = Substitute.For<IPendingCommandStatusQuery>();
#pragma warning disable CA2012 // NSubstitute owns each callback-created ValueTask; the coordinator awaits it once.
        query.QueryAsync(Arg.Any<PendingCommandEntry>(), Arg.Any<CancellationToken>()).Returns(_ => {
            time.Advance(TimeSpan.FromMilliseconds(elapsed));
            return ValueTask.FromResult<PendingCommandOutcomeObservation?>(new(PendingCommandOutcomeSource.FallbackPolling,
                PendingCommandTerminalOutcome.Confirmed, MessageId: message));
        });
#pragma warning restore CA2012
        PendingCommandPollingCoordinator sut = new(state, new PendingCommandOutcomeResolver(state), query,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()), NullLogger<PendingCommandPollingCoordinator>.Instance, time);
        (await sut.PollOnceAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
        state.GetByMessageId(message).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.DegradedExhausted);
        lifecycle.DidNotReceive().Transition(CorrelationId, CommandLifecycleState.Confirmed, message, false);
    }

}
