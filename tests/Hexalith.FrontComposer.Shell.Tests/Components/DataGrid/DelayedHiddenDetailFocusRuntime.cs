using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Tests.Components.DataGrid;

/// <summary>Controls completion of the hidden-detail focus module import.</summary>
internal sealed class DelayedHiddenDetailFocusRuntime : IJSRuntime
{
    /// <summary>Gets the pending module import.</summary>
    public TaskCompletionSource<IJSObjectReference> Import { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets the number of requested imports.</summary>
    public int ImportCalls { get; private set; }

    /// <summary>Gets or sets how many imports should report actual interop timeout cancellation.</summary>
    public int TimeoutImportsRemaining { get; set; }

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        try
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or OperationCanceledException)
        {
            return ValueTask.FromException<TValue>(ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        ImportCalls++;
        if (TimeoutImportsRemaining > 0)
        {
            TimeoutImportsRemaining--;
            throw new TaskCanceledException("interop timeout");
        }
        return (TValue)await Import.Task.ConfigureAwait(false);
    }

}
