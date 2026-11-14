using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>Holds a focus-module import across component disposal.</summary>
internal sealed class HeldRouteFocusRuntime : IJSRuntime
{
    /// <summary>Gets the pending import controlled by the test.</summary>
    public TaskCompletionSource<IJSObjectReference> Import { get; } = new();

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
        => (TValue)await Import.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
}
