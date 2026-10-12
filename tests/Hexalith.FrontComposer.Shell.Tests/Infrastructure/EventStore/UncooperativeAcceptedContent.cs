using System.Net;
using System.Text;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

/// <summary>Accepted content whose optional read ignores cancellation until the test releases it.</summary>
internal sealed class UncooperativeAcceptedContent : HttpContent
{
    private readonly TaskCompletionSource<Stream> _body = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Signals entry into the optional read.</summary>
    public TaskCompletionSource ReadAttempted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Signals settlement of the optional read.</summary>
    public TaskCompletionSource ReadCompleted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets the abandoned read's cancellation token.</summary>
    public CancellationToken ObservedToken { get; private set; }

    /// <summary>Completes the abandoned read with a valid correlation body.</summary>
    public void CompleteSuccessfully()
        => _body.TrySetResult(new MemoryStream(Encoding.UTF8.GetBytes("{\"correlationId\":\"late\"}")));

    /// <summary>Fails the abandoned read after local acceptance has returned.</summary>
    public void FailLate() => _body.TrySetException(new IOException("late optional read fault"));

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
