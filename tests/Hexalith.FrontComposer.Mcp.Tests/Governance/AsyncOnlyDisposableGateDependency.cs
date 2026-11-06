namespace Hexalith.FrontComposer.Mcp.Tests.Governance;

/// <summary>
/// A scoped gate dependency that implements only <see cref="IAsyncDisposable"/>, which a synchronous scope disposal rejects.
/// </summary>
internal sealed class AsyncOnlyDisposableGateDependency : IAsyncDisposable {
    private static int _disposals;

    /// <summary>Gets the number of instances disposed so far in this process.</summary>
    internal static int Disposals => Volatile.Read(ref _disposals);

    /// <inheritdoc />
    public ValueTask DisposeAsync() {
        _ = Interlocked.Increment(ref _disposals);
        return ValueTask.CompletedTask;
    }
}
