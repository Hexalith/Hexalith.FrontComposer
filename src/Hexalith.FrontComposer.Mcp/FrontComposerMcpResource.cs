using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp.Invocation;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.FrontComposer.Mcp;

internal sealed class FrontComposerMcpResource(McpResourceDescriptor descriptor) : McpServerResource {
    private readonly Resource _resource = new() {
        Uri = descriptor.ProtocolUri,
        Name = descriptor.Name,
        Title = descriptor.Title,
        Description = descriptor.Description,
        MimeType = "text/markdown",
    };

    public override Resource ProtocolResource => _resource;

    // Story 8-1 ships only plain Resources; resource templates with URI parameters
    // are owned by Story 8-6 (schema versioning / multi-surface abstraction).
    public override ResourceTemplate ProtocolResourceTemplate
        => throw new NotSupportedException("FrontComposer MCP does not expose resource templates in v1.");

    public override IReadOnlyList<object> Metadata { get; } = [descriptor];

    public override bool IsMatch(string uri)
        => string.Equals(uri, descriptor.ProtocolUri, StringComparison.Ordinal);

    public override async ValueTask<ReadResourceResult> ReadAsync(
        RequestContext<ReadResourceRequestParams> request,
        CancellationToken cancellationToken = default) {
        if (request.Services is null) {
            return BuildResult(FrontComposerMcpResult.Failure(FrontComposerMcpFailureCategory.DownstreamFailed));
        }

        FrontComposerMcpProjectionReader reader = request.Services.GetRequiredService<FrontComposerMcpProjectionReader>();
        string? requestedUri = request.Params?.Uri;
        if (string.IsNullOrWhiteSpace(requestedUri)) {
            return BuildResult(FrontComposerMcpResult.Failure(FrontComposerMcpFailureCategory.MalformedRequest));
        }

        FrontComposerMcpResult result = await reader.ReadAsync(requestedUri, cancellationToken).ConfigureAwait(false);
        return BuildResult(result);
    }

    private ReadResourceResult BuildResult(FrontComposerMcpResult result) => new() {
        Contents = [
            new TextResourceContents {
                Uri = descriptor.ProtocolUri,
                MimeType = result.IsError ? "text/plain" : "text/markdown",
                Text = result.Text,
            },
        ],
    };
}
