namespace Hexalith.FrontComposer.Contracts.Freshness;

/// <summary>Caller-owned transport, state, and announcement callbacks for one operation.</summary>
/// <remarks>Callbacks must not retain sensitive payloads. Re-reads are idempotent and honor cancellation.</remarks>
public sealed record FcOptimisticReconcileRequest
{
    /// <summary>Gets the optimistic state application callback.</summary>
    public required Action ApplyOptimistic { get; init; }

    /// <summary>Gets the command callback; true means accepted, not yet reconciled.</summary>
    public required Func<CancellationToken, Task<bool>> IssueCommand { get; init; }

    /// <summary>Gets the idempotent record re-read callback.</summary>
    public required Func<CancellationToken, Task<FcFreshnessStatus>> Reconcile { get; init; }

    /// <summary>Gets the optimistic rollback callback.</summary>
    public required Action Revert { get; init; }

    /// <summary>Gets the callback for a non-throwing command rejection.</summary>
    public required Action Rejected { get; init; }

    /// <summary>Gets the caller's mapping of a command exception to failure feedback.</summary>
    public required Action<Exception> Failed { get; init; }

    /// <summary>Gets routine announcement callbacks; the routine never moves focus.</summary>
    public required Action<FcReconcileState> Announce { get; init; }

    /// <summary>Gets a callback registering confirmations and returning their lifetime handle. Asynchronous handles are drained at teardown.</summary>
    public required Func<Action, IDisposable> Subscribe { get; init; }

    /// <summary>Gets optional transport readiness before command dispatch. Omit only for synchronous registration.</summary>
    public Func<IDisposable, CancellationToken, Task>? WaitForSubscriptionAsync { get; init; }

    /// <summary>Gets a callback reporting whether the confirmation transport is connected.</summary>
    public required Func<bool> IsConnected { get; init; }

    /// <summary>Gets the fallback driver, supplied a re-read, completion predicate, and lifetime token. Observe connectivity until completion or cancellation, resuming reads on each disconnection.</summary>
    public required Func<Func<CancellationToken, Task>, Func<bool>, CancellationToken, Task> RunFallback { get; init; }

    /// <summary>Gets optional coarse feedback for a retryable re-read failure; never log payloads.</summary>
    public Action? ReadFailed { get; init; }
}
