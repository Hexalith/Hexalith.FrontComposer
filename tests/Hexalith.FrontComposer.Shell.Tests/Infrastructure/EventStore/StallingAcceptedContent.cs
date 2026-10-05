using System.Net;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

/// <summary>An accepted response body whose stream never arrives until cancelled.</summary>
internal sealed class StallingAcceptedContent : HttpContent {
    /// <summary>Signals that the optional body read began.</summary>
    public TaskCompletionSource<object> ReadAttempted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <inheritdoc />
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => Task.CompletedTask;

    /// <inheritdoc />
    protected override bool TryComputeLength(out long length) {
        length = 0;
        return false;
    }

    /// <inheritdoc />
    protected override Task<Stream> CreateContentReadStreamAsync() => CreateContentReadStreamAsync(CancellationToken.None);

    /// <inheritdoc />
    protected override Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken) {
        ReadAttempted.TrySetResult(new object());
        TaskCompletionSource<Stream> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = cancellationToken.Register(() => pending.TrySetCanceled(cancellationToken));
        return pending.Task;
    }
}
