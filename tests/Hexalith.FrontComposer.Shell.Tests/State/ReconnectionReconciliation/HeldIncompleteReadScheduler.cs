using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

namespace Hexalith.FrontComposer.Shell.Tests.State.ReconnectionReconciliation;

/// <summary>Returns one incomplete pass whose read ignores cancellation, then a genuine changed pass.</summary>
internal sealed class HeldIncompleteReadScheduler : IProjectionFallbackRefreshScheduler, IProjectionIncompleteReadWaiter
{
    private readonly TaskCompletionSource<bool> _held = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<ProjectionReconciliationRefreshResult> _heldRecheck = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly bool _holdRecheck;
    private int _passes;

    internal HeldIncompleteReadScheduler(bool holdRecheck = false) => _holdRecheck = holdRecheck;

    internal TaskCompletionSource RecheckStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal TaskCompletionSource WaitStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal TaskCompletionSource CancellationEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal ManualResetEventSlim? CancellationRelease { get; set; }

    /// <summary>Completes the held read after its scoped reconciliation wait has expired.</summary>
    internal void Release() => _held.TrySetResult(true);

    internal void ReleaseRecheck() => _heldRecheck.TrySetResult(new ProjectionReconciliationRefreshResult(1, ["orders"]));

    /// <inheritdoc />
    public IDisposable RegisterLane(ProjectionFallbackLane lane) => new CancellationTokenSource();

    /// <inheritdoc />
    public Task<int> TriggerFallbackOnceAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

    /// <inheritdoc />
    public Task<int> TriggerNudgeRefreshAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default)
        => Task.FromResult(0);

    /// <inheritdoc />
    public Task<ProjectionReconciliationRefreshResult> TriggerReconciliationOnceAsync(long epoch, CancellationToken cancellationToken = default)
        => Task.FromResult(Interlocked.Increment(ref _passes) == 1
            ? new ProjectionReconciliationRefreshResult(0, []) { HasIncompleteEligibleReads = true, IncompleteViewKeys = ["orders"], IncompleteReadTasks = [_held.Task] }
            : new ProjectionReconciliationRefreshResult(1, ["orders"]));

    /// <inheritdoc />
    public Task<ProjectionReconciliationRefreshResult> RecheckIncompleteReadsAsync(
        long epoch, IReadOnlyCollection<string> viewKeys, CancellationToken cancellationToken)
    {
        RecheckStarted.TrySetResult();
        return _holdRecheck ? _heldRecheck.Task : TriggerReconciliationOnceAsync(epoch, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> WaitForIncompleteReadAsync(long epoch, ProjectionReconciliationRefreshResult pass, CancellationToken cancellationToken)
    {
        ManualResetEventSlim? release = CancellationRelease;
        using CancellationTokenRegistration registration = release is null ? default : cancellationToken.Register(() => {
            CancellationEntered.TrySetResult();
            release.Wait(CancellationToken.None);
        });
        WaitStarted.TrySetResult();
        return await _held.Task.ConfigureAwait(false);
    }
}
