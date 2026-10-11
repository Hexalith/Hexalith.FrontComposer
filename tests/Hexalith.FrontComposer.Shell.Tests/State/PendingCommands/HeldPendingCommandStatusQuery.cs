using Hexalith.FrontComposer.Shell.State.PendingCommands;

namespace Hexalith.FrontComposer.Shell.Tests.State.PendingCommands;

/// <summary>Holds provider evidence beyond cancellation so deadline tests can release every operation.</summary>
internal sealed class HeldPendingCommandStatusQuery : IPendingCommandStatusQuery
{
    /// <summary>Gets the provider evidence controlled by the test.</summary>
    public TaskCompletionSource<PendingCommandOutcomeObservation?> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets the signal that cancellation was delivered to the provider.</summary>
    public TaskCompletionSource<object> CancellationObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets the signal that the provider's cancellation callback finished.</summary>
    public TaskCompletionSource<object> CancellationFinished { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets or sets an optional gate for a blocking provider cancellation callback.</summary>
    public ManualResetEventSlim? CancellationRelease { get; set; }

    /// <summary>Gets the number of provider queries.</summary>
    public int Calls { get; private set; }

    /// <inheritdoc />
    public async ValueTask<PendingCommandOutcomeObservation?> QueryAsync(PendingCommandEntry pendingCommand, CancellationToken cancellationToken = default)
    {
        Calls++;
        using CancellationTokenRegistration registration = cancellationToken.Register(() =>
        {
            CancellationObserved.TrySetResult(new object());
            CancellationRelease?.Wait();
            CancellationFinished.TrySetResult(new object());
        });
        return await Completion.Task.ConfigureAwait(false);
    }
}
