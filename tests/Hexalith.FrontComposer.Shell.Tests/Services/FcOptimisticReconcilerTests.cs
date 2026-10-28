using Hexalith.FrontComposer.Contracts.Freshness;
using Hexalith.FrontComposer.Shell.Services.Feedback;
using NSubstitute;
using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

public sealed class FcOptimisticReconcilerTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AcceptanceUsesConfirmOrFallbackAndDisposesSubscription(bool connected)
    {
        Action? confirm = null;
        var handle = Substitute.For<IDisposable>();
        var states = new List<FcReconcileState>();
        int reads = 0;
        var request = NewRequest(states) with
        {
            IsConnected = () => connected,
            RunFallback = (poll, _, token) => connected ? Task.CompletedTask : poll(token),
            Subscribe = callback => { confirm = callback; return handle; },
            Reconcile = _ => { reads++; return Task.FromResult(FcFreshnessStatus.Current); },
        };
        Task run = new FcOptimisticReconciler().ExecuteAsync(request, TestContext.Current.CancellationToken);
        if (connected)
        {
            reads.ShouldBe(0);
            confirm!();
            confirm();
        }
        await run.ConfigureAwait(true);
        confirm!();
        reads.ShouldBe(1);
        states.ShouldBe([FcReconcileState.Saving, FcReconcileState.Reconciled]);
        handle.Received(1).Dispose();
    }

    [Fact]
    public async Task SimultaneousAndLateConfirmationsCannotApplyTwice()
    {
        Action? confirm = null;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var read = new TaskCompletionSource<FcFreshnessStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
        int reads = 0;
        var states = new List<FcReconcileState>();
        var request = NewRequest(states) with
        {
            IsConnected = () => true,
            Subscribe = callback => { confirm = callback; return Substitute.For<IDisposable>(); },
            Reconcile = _ => { reads++; entered.TrySetResult(); return read.Task; },
        };
        Task run = new FcOptimisticReconciler().ExecuteAsync(request, TestContext.Current.CancellationToken);
        confirm!();
        await entered.Task.ConfigureAwait(true);
        confirm();
        read.SetResult(FcFreshnessStatus.Current);
        await run.ConfigureAwait(true);
        confirm();
        reads.ShouldBe(1);
        states.Count(s => s == FcReconcileState.Reconciled).ShouldBe(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RejectionAndCommandFailureRevertOnce(bool throws)
    {
        int reverts = 0;
        int failures = 0;
        var request = NewRequest([]) with
        {
            IssueCommand = _ => throws ? throw new InvalidOperationException("synthetic") : Task.FromResult(false),
            Revert = () => reverts++,
            Rejected = () => failures++,
            Failed = _ => failures++,
            Reconcile = _ => throw new InvalidOperationException("must not reconcile"),
        };
        await new FcOptimisticReconciler().ExecuteAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
        reverts.ShouldBe(1);
        failures.ShouldBe(1);
    }

    [Fact]
    public async Task StaleAndRetryableFailureContinueUntilCurrent()
    {
        Action? confirm = null;
        int reads = 0;
        int failures = 0;
        var states = new List<FcReconcileState>();
        var request = NewRequest(states) with
        {
            IsConnected = () => true,
            Subscribe = callback => { confirm = callback; return Substitute.For<IDisposable>(); },
            Reconcile = _ => ++reads switch
            {
                1 => Task.FromResult(FcFreshnessStatus.Stale),
                2 => throw new InvalidOperationException("synthetic"),
                _ => Task.FromResult(FcFreshnessStatus.Current),
            },
            ReadFailed = () => failures++,
        };
        Task run = new FcOptimisticReconciler().ExecuteAsync(request, TestContext.Current.CancellationToken);
        confirm!();
        confirm();
        run.IsCompleted.ShouldBeFalse();
        confirm();
        await run.ConfigureAwait(true);
        failures.ShouldBe(1);
        states.ShouldBe([FcReconcileState.Saving, FcReconcileState.Degraded, FcReconcileState.Reconciled]);
    }

    [Fact]
    public async Task CancellationDuringReadSuppressesLateFeedbackAndDisposes()
    {
        using var cancellation = new CancellationTokenSource();
        Action? confirm = null;
        var handle = Substitute.For<IDisposable>();
        var states = new List<FcReconcileState>();
        var read = new TaskCompletionSource<FcFreshnessStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
        var request = NewRequest(states) with
        {
            IsConnected = () => true,
            Subscribe = callback => { confirm = callback; return handle; },
            Reconcile = _ => read.Task,
        };
        Task run = new FcOptimisticReconciler().ExecuteAsync(request, cancellation.Token);
        confirm!();
        await cancellation.CancelAsync().ConfigureAwait(true);
        read.SetResult(FcFreshnessStatus.Current);
        await run.ConfigureAwait(true);
        confirm();
        states.ShouldBe([FcReconcileState.Saving]);
        handle.Received(1).Dispose();
    }

    [Fact]
    public async Task CommandFaultAfterCallerCancellationAbandonsWithoutFailureFeedback()
    {
        using var cancellation = new CancellationTokenSource();
        var command = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var states = new List<FcReconcileState>();
        var request = NewRequest(states) with { IssueCommand = _ => command.Task };
        Task run = new FcOptimisticReconciler().ExecuteAsync(request, cancellation.Token);
        await cancellation.CancelAsync();
        command.SetException(new InvalidOperationException("abandoned command"));
        await run.WaitAsync(TestContext.Current.CancellationToken);
        states.ShouldBe([FcReconcileState.Saving]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RejectionCallbackFaultPropagatesWithoutSecondRollbackOrFailure(bool rollbackThrows)
    {
        int reverts = 0;
        int failures = 0;
        var fault = new InvalidOperationException("caller callback");
        var request = NewRequest([]) with
        {
            IssueCommand = _ => Task.FromResult(false),
            Revert = () => { reverts++; if (rollbackThrows) throw fault; },
            Rejected = () => throw fault,
            Failed = _ => failures++,
        };
        (await Should.ThrowAsync<InvalidOperationException>(() => new FcOptimisticReconciler()
            .ExecuteAsync(request, TestContext.Current.CancellationToken))).ShouldBeSameAs(fault);
        reverts.ShouldBe(1);
        failures.ShouldBe(0);
    }

    [Theory]
    [InlineData("reconciled", true)]
    [InlineData("degraded", true)]
    [InlineData("read-failed", true)]
    [InlineData("reconciled", false)]
    [InlineData("read-failed", false)]
    public async Task ReconcileCallbackFaultTerminatesAndDisposes(string callback, bool connected)
    {
        Action? confirm = null;
        var handle = Substitute.For<IDisposable>();
        var fault = new InvalidOperationException("caller callback");
        int reads = 0;
        var request = NewRequest([]) with
        {
            IsConnected = () => connected,
            Subscribe = action => { confirm = action; return handle; },
            Reconcile = _ =>
            {
                reads++;
                return callback == "read-failed" ? throw new IOException("retryable read")
                    : Task.FromResult(callback == "degraded" ? FcFreshnessStatus.Stale : FcFreshnessStatus.Current);
            },
            ReadFailed = () => throw fault,
            Announce = state => { if (state != FcReconcileState.Saving) throw fault; },
        };
        Task run = new FcOptimisticReconciler().ExecuteAsync(request, TestContext.Current.CancellationToken);
        confirm!();
        (await Should.ThrowAsync<InvalidOperationException>(() => run.WaitAsync(
            TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken))).ShouldBeSameAs(fault);
        confirm!();
        reads.ShouldBe(1);
        handle.Received(1).Dispose();
    }

    [Fact]
    public async Task ConfirmationDuringCommandIsRecoveredByInitialRead()
    {
        Action? confirm = null;
        bool published = false;
        int reads = 0;
        var states = new List<FcReconcileState>();
        var request = NewRequest(states) with
        {
            IssueCommand = _ => { published = true; confirm?.Invoke(); return Task.FromResult(true); },
            Subscribe = callback => { confirm = callback; return Substitute.For<IDisposable>(); },
            Reconcile = _ => { reads++; return Task.FromResult(published ? FcFreshnessStatus.Current : FcFreshnessStatus.Stale); },
            IsConnected = () => true,
        };
        await new FcOptimisticReconciler().ExecuteAsync(request, TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        reads.ShouldBe(1);
        states.ShouldBe([FcReconcileState.Saving, FcReconcileState.Reconciled]);
    }

    [Fact]
    public async Task DelayedTransportReadinessRecoversWithoutAnotherConfirmation()
    {
        Action? ready = null;
        var registrationReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool published = false;
        int reads = 0;
        var states = new List<FcReconcileState>();
        var request = NewRequest(states) with
        {
            WaitForSubscriptionAsync = (_, token) => registrationReady.Task.WaitAsync(token),
            IssueCommand = _ => { published = true; ready!(); return Task.FromResult(true); },
            Subscribe = callback => { ready = callback; return Substitute.For<IDisposable>(); },
            IsConnected = () => true,
            RunFallback = (_, _, _) => Task.CompletedTask,
            Reconcile = _ => { reads++; return Task.FromResult(published ? FcFreshnessStatus.Current : FcFreshnessStatus.Stale); },
        };
        Task run = new FcOptimisticReconciler().ExecuteAsync(request, TestContext.Current.CancellationToken);
        run.IsCompleted.ShouldBeFalse();
        published.ShouldBeFalse();
        reads.ShouldBe(0);
        registrationReady.SetResult();
        await run.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        reads.ShouldBe(1);
        states.Count(state => state == FcReconcileState.Reconciled).ShouldBe(1);
    }

    [Fact]
    public async Task FallbackObservesRepeatedDisconnectionsAndIsDrainedAtCompletion()
    {
        using var ticks = new SemaphoreSlim(0);
        using var observed = new SemaphoreSlim(0);
        bool connected = true;
        bool current = false;
        bool driverActive = false;
        int reads = 0;
        var request = NewRequest([]) with
        {
            IsConnected = () => connected,
            Reconcile = _ => { reads++; return Task.FromResult(current ? FcFreshnessStatus.Current : FcFreshnessStatus.Stale); },
            RunFallback = async (poll, done, token) =>
            {
                driverActive = true;
                try
                {
                    while (!done())
                    {
                        await ticks.WaitAsync(token).ConfigureAwait(false);
                        if (!connected) await poll(token).ConfigureAwait(false);
                        observed.Release();
                    }
                }
                finally
                {
                    driverActive = false;
                }
            },
        };
        Task run = new FcOptimisticReconciler().ExecuteAsync(request, TestContext.Current.CancellationToken);
        driverActive.ShouldBeTrue();
        connected = false;
        ticks.Release();
        await observed.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        reads.ShouldBe(1);
        connected = true;
        ticks.Release();
        await observed.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        reads.ShouldBe(1);
        connected = false;
        current = true;
        ticks.Release();
        await run.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        reads.ShouldBe(2);
        driverActive.ShouldBeFalse();
    }

    [Fact]
    public async Task ConnectedDirectPollFallbackCannotReadOldCurrent()
    {
        using var cancellation = new CancellationTokenSource();
        Action? confirm = null;
        int reads = 0;
        var states = new List<FcReconcileState>();
        Task run = new FcOptimisticReconciler().ExecuteAsync(NewRequest(states) with
        {
            Subscribe = callback => { confirm = callback; return Substitute.For<IDisposable>(); },
            IsConnected = () => true,
            RunFallback = (poll, _, token) => poll(token),
            Reconcile = _ => { reads++; return Task.FromResult(FcFreshnessStatus.Current); },
        }, cancellation.Token);
        reads.ShouldBe(0);
        run.IsCompleted.ShouldBeFalse();
        confirm!();
        await run.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        reads.ShouldBe(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BufferedConfirmationCannotReconcileRejectedOrCancelledCommand(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        Action? confirm = null;
        int reads = 0;
        int reverts = 0;
        var handle = Substitute.For<IDisposable>();
        await new FcOptimisticReconciler().ExecuteAsync(NewRequest([]) with
        {
            Subscribe = callback => { confirm = callback; return handle; },
            IssueCommand = token =>
            {
                confirm!();
                if (cancel)
                {
                    cancellation.Cancel();
                    token.ThrowIfCancellationRequested();
                }
                return Task.FromResult(false);
            },
            Reconcile = _ => { reads++; return Task.FromResult(FcFreshnessStatus.Current); },
            Revert = () => reverts++,
            Rejected = () => { },
        }, cancellation.Token).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        reads.ShouldBe(0);
        reverts.ShouldBe(cancel ? 0 : 1);
        handle.Received(1).Dispose();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SubscriptionAcquisitionFailureRevertsOnceOrSilentlyAbandonsBeforeDispatch(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        int commands = 0;
        int reverts = 0;
        int failures = 0;
        var fault = new InvalidOperationException("synthetic subscribe failure");
        await new FcOptimisticReconciler().ExecuteAsync(NewRequest([]) with
        {
            Subscribe = _ =>
            {
                if (cancel) cancellation.Cancel();
                throw fault;
            },
            IssueCommand = _ => { commands++; return Task.FromResult(true); },
            Revert = () => reverts++,
            Failed = exception => { exception.ShouldBeSameAs(fault); failures++; },
        }, cancellation.Token).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        commands.ShouldBe(0);
        reverts.ShouldBe(cancel ? 0 : 1);
        failures.ShouldBe(cancel ? 0 : 1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AsynchronousSubscriptionDisposalIsAwaitedExactlyOnce(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        Action? confirm = null;
        var removal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handle = new DelayedReconcileSubscription(removal.Task);
        Task run = new FcOptimisticReconciler().ExecuteAsync(NewRequest([]) with
        {
            Subscribe = callback => { confirm = callback; return handle; },
        }, cancellation.Token);
        if (cancel) await cancellation.CancelAsync();
        else confirm!();
        await handle.DisposalStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        run.IsCompleted.ShouldBeFalse();
        handle.DisposeCalls.ShouldBe(0);
        removal.SetResult();
        await run.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        handle.AsyncDisposeCalls.ShouldBe(1);
        handle.DisposeCalls.ShouldBe(0);
    }

    [Fact]
    public async Task ThrowingDisposalStillDrainsFallbackAndActiveRead()
    {
        using var cancellation = new CancellationTokenSource();
        Action? confirm = null;
        var read = new TaskCompletionSource<FcFreshnessStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
        var readExited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var driver = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var fault = new InvalidOperationException("synthetic asynchronous disposal failure");
        var handle = new DelayedReconcileSubscription(Task.CompletedTask, fault);
        Task run = new FcOptimisticReconciler().ExecuteAsync(NewRequest([]) with
        {
            Subscribe = callback => { confirm = callback; return handle; },
            IsConnected = () => true,
            Reconcile = async _ =>
            {
                try { return await read.Task.ConfigureAwait(false); }
                finally { readExited.TrySetResult(); }
            },
            RunFallback = (_, _, _) => driver.Task,
        }, cancellation.Token);
        confirm!();
        await cancellation.CancelAsync();
        await handle.DisposalStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        run.IsCompleted.ShouldBeFalse();
        read.SetResult(FcFreshnessStatus.Current);
        await readExited.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        run.IsCompleted.ShouldBeFalse();
        driver.SetResult();
        (await Should.ThrowAsync<InvalidOperationException>(() => run.WaitAsync(
            TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken))).ShouldBeSameAs(fault);
        handle.AsyncDisposeCalls.ShouldBe(1);
        handle.DisposeCalls.ShouldBe(0);
    }

    [Fact]
    public async Task CancellationCallbackFailureStillAwaitsRemovalAndFallback()
    {
        Action? confirm = null;
        var removal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var driver = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handle = new DelayedReconcileSubscription(removal.Task);
        CancellationTokenRegistration faulting = default;
        Task run = new FcOptimisticReconciler().ExecuteAsync(NewRequest([]) with
        {
            Subscribe = callback => { confirm = callback; return handle; },
            Reconcile = token =>
            {
                faulting = token.Register(() => throw new InvalidOperationException("synthetic cancellation callback"));
                return Task.FromResult(FcFreshnessStatus.Current);
            },
            RunFallback = (_, _, _) => driver.Task,
        }, TestContext.Current.CancellationToken);
        confirm!();
        await handle.DisposalStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        run.IsCompleted.ShouldBeFalse();
        removal.SetResult();
        run.IsCompleted.ShouldBeFalse();
        driver.SetResult();
        await Should.ThrowAsync<AggregateException>(() => run.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        faulting.Dispose();
        handle.AsyncDisposeCalls.ShouldBe(1);
        handle.DisposeCalls.ShouldBe(0);
    }

    private static FcOptimisticReconcileRequest NewRequest(List<FcReconcileState> states) => new()
    {
        ApplyOptimistic = () => { },
        IssueCommand = _ => Task.FromResult(true),
        Reconcile = _ => Task.FromResult(FcFreshnessStatus.Current),
        Revert = () => throw new InvalidOperationException("unexpected rollback"),
        Rejected = () => throw new InvalidOperationException("unexpected rejection"),
        Failed = _ => throw new InvalidOperationException("unexpected failure"),
        Announce = states.Add,
        Subscribe = _ => Substitute.For<IDisposable>(),
        IsConnected = () => false,
        RunFallback = (_, _, _) => Task.CompletedTask,
    };
}
