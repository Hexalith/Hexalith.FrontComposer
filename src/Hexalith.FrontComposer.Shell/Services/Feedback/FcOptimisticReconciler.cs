using Hexalith.FrontComposer.Contracts.Freshness;

namespace Hexalith.FrontComposer.Shell.Services.Feedback;

/// <summary>Runs one optimistic operation using caller-owned transport and domain mappings.</summary>
public sealed class FcOptimisticReconciler
{
    /// <summary>Applies, sends, and waits for a current record; cancellation abandons feedback silently.</summary>
    /// <param name="request">Operation callbacks.</param>
    /// <param name="cancellationToken">The caller's operation lifetime.</param>
    public async Task ExecuteAsync(FcOptimisticReconcileRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        request.ApplyOptimistic();
        request.Announce(FcReconcileState.Saving);
        bool accepted;
        try
        {
            accepted = await request.IssueCommand(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            request.Revert();
            request.Failed(exception);
            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        if (!accepted)
        {
            request.Revert();
            request.Rejected();
            return;
        }

        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        CancellationToken token = lifetime.Token;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        // Serializing asynchronous reads fences simultaneous confirmations and late responses.
        // The gate remains reachable by a captured late callback, so it is intentionally not disposed.
        var gate = new SemaphoreSlim(1, 1);
        bool done = false;
        async Task TryReconcileAsync(CancellationToken ignored)
        {
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

        using IDisposable subscription = request.Subscribe(() => _ = TryReconcileAsync(token));
        using CancellationTokenRegistration registration = token.Register(() => completion.TrySetCanceled(token));
        try
        {
            if (!request.IsConnected())
            {
                await request.RunFallback(TryReconcileAsync, () => Volatile.Read(ref done), token).ConfigureAwait(false);
            }

            await completion.Task.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Caller abandonment has no failure announcement or focus effect.
        }
        finally
        {
            await lifetime.CancelAsync().ConfigureAwait(false);
            // Drain any asynchronous re-read; a late result cannot announce after teardown.
            await gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            gate.Release();
        }
    }
}
