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
            Subscribe = _ => throw new InvalidOperationException("must not subscribe"),
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
        RunFallback = (poll, _, token) => poll(token),
    };
}
