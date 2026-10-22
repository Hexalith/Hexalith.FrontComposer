using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>Records module ownership and optionally holds focus completion.</summary>
internal sealed class RecordingRouteFocusModule : IJSObjectReference
{
    /// <summary>Gets the number of focus invocations.</summary>
    public int FocusCalls { get; private set; }

    /// <summary>Gets the number of disposal invocations.</summary>
    public int DisposalCount { get; private set; }

    /// <summary>Gets the signal that the module has been released.</summary>
    public TaskCompletionSource Disposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets the signal that mounted focus has started.</summary>
    public TaskCompletionSource FocusStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets or sets a pending focus completion.</summary>
    public TaskCompletionSource? FocusCompletion { get; set; }

    /// <summary>Gets or sets a disposal failure representing an ended browser circuit.</summary>
    public Exception? DisposalFailure { get; set; }

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    /// <inheritdoc />
    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        FocusCalls++;
        FocusStarted.TrySetResult();
        if (FocusCompletion is not null) await FocusCompletion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        return default!;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        DisposalCount++;
        Disposed.TrySetResult();
        return DisposalFailure is null ? ValueTask.CompletedTask : ValueTask.FromException(DisposalFailure);
    }
}
