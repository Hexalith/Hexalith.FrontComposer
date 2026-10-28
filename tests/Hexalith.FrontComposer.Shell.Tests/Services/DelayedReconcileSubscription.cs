namespace Hexalith.FrontComposer.Shell.Tests.Services;

/// <summary>A controllable dual-interface handle that proves which disposal path is awaited.</summary>
internal sealed class DelayedReconcileSubscription(Task removal, Exception? failure = null) : IDisposable, IAsyncDisposable
{
    /// <summary>Gets the start of asynchronous disposal.</summary>
    public TaskCompletionSource DisposalStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets synchronous disposal calls.</summary>
    public int DisposeCalls { get; private set; }

    /// <summary>Gets asynchronous disposal calls.</summary>
    public int AsyncDisposeCalls { get; private set; }

    /// <inheritdoc />
    public void Dispose() => DisposeCalls++;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        AsyncDisposeCalls++;
        DisposalStarted.TrySetResult();
        await removal.ConfigureAwait(false);
        if (failure is not null)
        {
            throw failure;
        }
    }
}
