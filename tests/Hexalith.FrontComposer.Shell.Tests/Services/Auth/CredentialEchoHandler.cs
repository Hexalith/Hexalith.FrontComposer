using System.Net;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Auth;

internal sealed class CredentialEchoHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(request.Headers.Authorization?.Parameter ?? string.Empty),
        });
}
