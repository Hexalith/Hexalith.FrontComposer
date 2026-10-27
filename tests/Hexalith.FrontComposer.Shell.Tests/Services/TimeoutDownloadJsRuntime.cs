using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

internal sealed class TimeoutDownloadJsRuntime : IJSRuntime, IJSObjectReference
{
    public bool Disposed { get; private set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

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
