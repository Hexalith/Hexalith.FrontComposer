using System.Net;
using System.Text;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

/// <summary>Deterministic accepted transport with a stalled retry send or response body.</summary>
internal sealed class StalledPostAcknowledgementHandler(bool stallSend, bool holdNextSubmit = false) : HttpMessageHandler
{
    /// <summary>Gets the request bodies, including the original MessageId on retry.</summary>
    public List<string> Bodies { get; } = [];

    /// <summary>Signals entry into the optional second send.</summary>
    public TaskCompletionSource<object> RetryEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Allows cleanup of a transport that deliberately ignores cancellation.</summary>
    public TaskCompletionSource<HttpResponseMessage> ReleaseRetry { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Signals entry into a subsequent submit before it has accepted headers.</summary>
    public TaskCompletionSource NextSubmitEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Allows a subsequent submit to receive its accepted headers.</summary>
    public TaskCompletionSource<HttpResponseMessage> ReleaseNextSubmit { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets the optional retry's cancellation token.</summary>
    public CancellationToken RetryToken { get; private set; }

    /// <summary>Gets the stalled optional retry body.</summary>
    public StallingAcceptedContent StalledBody { get; } = new();

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
        if (Bodies.Count == 1)
        {
            return new(HttpStatusCode.Accepted) { Content = new StringContent("{", Encoding.UTF8, "application/json") };
        }

        if (holdNextSubmit && Bodies.Count == 3)
        {
            NextSubmitEntered.TrySetResult();
            return await ReleaseNextSubmit.Task.ConfigureAwait(false);
        }

        RetryToken = cancellationToken;
        RetryEntered.TrySetResult(new object());
        return stallSend
            ? await ReleaseRetry.Task.ConfigureAwait(false)
            : new(HttpStatusCode.Accepted) { Content = StalledBody };
    }
}
