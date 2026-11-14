using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

internal sealed class TimeoutDownloadJsRuntime : IJSRuntime, IJSObjectReference
{
    public bool Disposed { get; private set; }

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

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => identifier switch
        {
            "import" => ValueTask.FromResult((TValue)(object)this),
            "cancelDownload" => ValueTask.FromException<TValue>(new TaskCanceledException("interop timeout")),
            _ => ValueTask.FromResult(default(TValue)!),
        };

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}
