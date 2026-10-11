using System.Net;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

/// <summary>Optional accepted content with a deterministic blocking or throwing cancellation callback.</summary>
internal sealed class BlockingAcceptedCancellationContent(bool throwOnCancellation) : HttpContent
{
    private readonly ManualResetEventSlim _release = new(initialState: false);
    private readonly TaskCompletionSource<Stream> _body = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Signals entry into the optional read.</summary>
    public TaskCompletionSource ReadAttempted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Signals entry into the abandoned read's cancellation callback.</summary>
    public TaskCompletionSource CancellationEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Signals that the callback has been released, including a throwing callback.</summary>
    public TaskCompletionSource CancellationCompleted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Signals that abandoned reading settled.</summary>
    public TaskCompletionSource ReadCompleted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets the owned optional-read cancellation token.</summary>
    public CancellationToken ObservedToken { get; private set; }

    /// <summary>Releases all deliberately blocked provider work.</summary>
    public void ReleaseCancellation() => _release.Set();

    /// <inheritdoc />
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => Task.CompletedTask;

    /// <inheritdoc />
    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }

    /// <inheritdoc />
    protected override Task<Stream> CreateContentReadStreamAsync() => CreateContentReadStreamAsync(CancellationToken.None);

    /// <inheritdoc />
    protected override async Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken)
    {
        ObservedToken = cancellationToken;
        using CancellationTokenRegistration registration = cancellationToken.Register(() =>
        {
            CancellationEntered.TrySetResult();
            _release.Wait();
            try
            {
                if (throwOnCancellation)
                {
                    throw new InvalidOperationException("provider cancellation failed");
                }
            }
            finally
            {
                _body.TrySetCanceled(cancellationToken);
                CancellationCompleted.TrySetResult();
            }
        });
        ReadAttempted.TrySetResult();
        try
        {
            return await _body.Task.ConfigureAwait(false);
        }
        finally
        {
            ReadCompleted.TrySetResult();
        }
    }
}
