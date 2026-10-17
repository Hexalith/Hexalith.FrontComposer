using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Tests.Components.DataGrid;

/// <summary>Records actual hidden-detail focus calls after a delayed module import.</summary>
internal sealed class RecordingHiddenDetailFocusModule : IJSObjectReference
{
    /// <summary>Gets the number of focus requests.</summary>
    public int FocusCalls { get; private set; }

    /// <summary>Gets the number of detail listener registrations.</summary>
    public int TrackingCalls { get; private set; }

    /// <summary>Gets or sets how many focus calls should time out.</summary>
    public int TimeoutFocusCallsRemaining { get; set; }

    /// <summary>Gets or sets whether module cleanup should report a timeout.</summary>
    public bool TimeoutDisposal { get; set; }

    /// <summary>Gets or sets whether focus cancellation should fail.</summary>
    public bool FailCancellation { get; set; }

    /// <summary>Gets the number of focus cancellation requests.</summary>
    public int CancellationCalls { get; private set; }

    /// <summary>Signals that the awaited focus attempt has released the module.</summary>
    public TaskCompletionSource<object> Released { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        if (identifier is "focusFirstButtonWithin" or "focusReplacementHeading") { FocusCalls++; }
        if (identifier == "trackDetailFocus") { TrackingCalls++; }
        if (identifier == "cancelReplacementFocus")
        {
            CancellationCalls++;
            if (FailCancellation) { return ValueTask.FromException<TValue>(new JSException("cancel failed")); }
        }
        if (TimeoutFocusCallsRemaining > 0)
        {
            TimeoutFocusCallsRemaining--;
            return ValueTask.FromException<TValue>(new TaskCanceledException("focus timeout"));
        }
        return ValueTask.FromResult(typeof(TValue) == typeof(bool) ? (TValue)(object)true : default!);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Released.TrySetResult(new object());
        return TimeoutDisposal ? ValueTask.FromException(new TaskCanceledException("cleanup timeout")) : ValueTask.CompletedTask;
    }
}
