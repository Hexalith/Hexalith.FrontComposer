using System.Diagnostics.CodeAnalysis;

using Hexalith.FrontComposer.Contracts.Freshness;

namespace Hexalith.FrontComposer.Shell.Services.Feedback;

/// <summary>Runs one optimistic operation using caller-owned transport and domain mappings.</summary>
public sealed class FcOptimisticReconciler
{
    /// <summary>Applies, sends, and waits for a current record; cancellation abandons feedback silently.</summary>
    /// <param name="request">Operation callbacks.</param>
    /// <param name="cancellationToken">The caller's operation lifetime.</param>
    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Preserves the published instance service API used by DI adapters.")]
    public async Task ExecuteAsync(FcOptimisticReconcileRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        request.ApplyOptimistic();
        request.Announce(FcReconcileState.Saving);
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        CancellationToken token = lifetime.Token;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        // Serializing asynchronous reads fences simultaneous confirmations and late responses.
        // The gate remains reachable by a captured late callback, so it is intentionally not disposed.
        var gate = new SemaphoreSlim(1, 1);
        bool done = false;
        bool accepted = false;
        bool commandStarted = false;
        int pendingConfirmation = 0;
        async Task TryReconcileAsync(CancellationToken ignored)
        {
            if (!Volatile.Read(ref commandStarted))
            {
                return;
            }
            if (!Volatile.Read(ref accepted))
            {
                Interlocked.Exchange(ref pendingConfirmation, 1);
                if (!Volatile.Read(ref accepted))
                {
                    return;
                }
            }
            try
            {
                await gate.WaitAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }

            try
            {
                if (done || token.IsCancellationRequested)
                {
                    return;
                }

                FcFreshnessStatus freshness;
                try
                {
                    freshness = await request.Reconcile(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception)
                {
                    if (!token.IsCancellationRequested)
                    {
                        request.ReadFailed?.Invoke();
                    }
                    return;
                }

                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (freshness == FcFreshnessStatus.Current)
                {
                    done = true;
                    request.Announce(FcReconcileState.Reconciled);
                    completion.TrySetResult();
                }
                else
                {
                    request.Announce(FcReconcileState.Degraded);
                }
            }
            catch (Exception exception)
            {
                // Subscription callbacks are fire-and-forget; deliver callback faults to
                // the operation owner so it can terminate and dispose the subscription.
                done = true;
                completion.TrySetException(exception);
            }
            finally
            {
                gate.Release();
            }
        }

        IDisposable subscription;
        try
        {
            subscription = request.Subscribe(() => _ = TryReconcileAsync(token));
        }
        catch (Exception exception)
        {
            if (!token.IsCancellationRequested)
            {
                request.Revert();
                request.Failed(exception);
            }
            return;
        }
        using CancellationTokenRegistration registration = token.Register(() => completion.TrySetCanceled(token));
        Task polling = Task.CompletedTask;
        try
        {
            bool commandAccepted;
            try
            {
                if (request.WaitForSubscriptionAsync is { } ready)
                {
                    await ready(subscription, token).ConfigureAwait(false);
                }
                token.ThrowIfCancellationRequested();
                Volatile.Write(ref commandStarted, true);
                commandAccepted = await request.IssueCommand(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                request.Revert();
                request.Failed(exception);
                return;
            }
            if (token.IsCancellationRequested)
            {
                return;
            }
            if (!commandAccepted)
            {
                request.Revert();
                request.Rejected();
                return;
            }
            Volatile.Write(ref accepted, true);
            if (Interlocked.Exchange(ref pendingConfirmation, 0) == 1)
            {
                await TryReconcileAsync(token).ConfigureAwait(false);
            }
            polling = ObserveFallbackAsync();
            await completion.Task.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Caller abandonment has no failure announcement or focus effect.
        }
        finally
        {
            try
            {
                try
                {
                    await lifetime.CancelAsync().ConfigureAwait(false);
                }
                finally
                {
                    if (subscription is IAsyncDisposable asynchronous)
                    {
                        await asynchronous.DisposeAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        subscription.Dispose();
                    }
                }
            }
            finally
            {
                try
                {
                    await polling.ConfigureAwait(false);
                }
                finally
                {
                    // Even a cancellation or disposal fault must drain every serialized read.
                    await gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                    gate.Release();
                }
            }
        }

        async Task ObserveFallbackAsync()
        {
            try
            {
                // The caller's driver observes connectivity for the whole operation, including reconnects.
                await request.RunFallback(
                    pollToken => token.IsCancellationRequested || request.IsConnected()
                        ? Task.CompletedTask : TryReconcileAsync(pollToken),
                    () => Volatile.Read(ref done), token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
        }
    }
}
