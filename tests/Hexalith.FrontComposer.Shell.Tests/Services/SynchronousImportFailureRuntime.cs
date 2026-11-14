using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

/// <summary>Throws on its first module import and records calls through the recovered module.</summary>
internal sealed class SynchronousImportFailureRuntime : IJSRuntime, IJSObjectReference {
    /// <summary>Gets the number of attempted module imports.</summary>
    public int ImportCalls { get; private set; }

    /// <summary>Gets the invoked module operation names.</summary>
    public List<string> Invocations { get; } = [];

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) {
        if (identifier == "import") {
            if (++ImportCalls == 1) throw new JSException("synchronous import failure");
            return ValueTask.FromResult((TValue)(object)this);
        }
        Invocations.Add(identifier);
        object? result = identifier == "isFocusWithinDataGrid" ? true
            : identifier == "copyToClipboard" ? "Success" : default(TValue);
        return ValueTask.FromResult((TValue)result!);
    }

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) {
        if (identifier == "import") {
            if (++ImportCalls == 1) throw new JSException("synchronous import failure");
            return ValueTask.FromResult((TValue)(object)this);
        }
        Invocations.Add(identifier);
        object? result = identifier == "isFocusWithinDataGrid" ? true
            : identifier == "copyToClipboard" ? "Success" : default(TValue);
        return ValueTask.FromResult((TValue)result!);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
