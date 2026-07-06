using System.Text.Json.Nodes;

using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp.Invocation;

using Microsoft.Extensions.DependencyInjection;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Hexalith.FrontComposer.Mcp;

internal sealed class FrontComposerMcpResource(McpResourceDescriptor descriptor) : McpServerResource, IMcpServerPrimitive {
    private readonly ResourceTemplate _resourceTemplate = new() {
        UriTemplate = descriptor.ProtocolUri,
        Name = descriptor.Name,
        Title = descriptor.Title,
        Description = descriptor.Description,
        MimeType = "text/markdown",
    };

    private readonly Resource _resource = new() {
        Uri = descriptor.ProtocolUri,
        Name = descriptor.Name,
        Title = descriptor.Title,
        Description = descriptor.Description,
        MimeType = "text/markdown",
    };

    public override Resource ProtocolResource => _resource;

    string IMcpServerPrimitive.Id => descriptor.ProtocolUri;

    public override ResourceTemplate ProtocolResourceTemplate
        => _resourceTemplate;

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
            return BuildResult(FrontComposerMcpProjectionFailureMapper.ToResult(FrontComposerMcpFailureCategory.MalformedRequest));
        }

        FrontComposerMcpResult result = await reader.ReadAsync(requestedUri, cancellationToken).ConfigureAwait(false);
        return BuildResult(result);
    }

    private ReadResourceResult BuildResult(FrontComposerMcpResult result) => new() {
        Contents = [
            new TextResourceContents {
                Uri = descriptor.ProtocolUri,
                MimeType = result.IsError ? "text/plain" : "text/markdown",
                Text = BuildText(result),
                Meta = result.StructuredContent,
            },
        ],
    };

    private static string BuildText(FrontComposerMcpResult result) {
        if (!result.IsError || result.StructuredContent is null) {
            return result.Text;
        }

        if (TryGetBoolean(result.StructuredContent, "isHiddenEquivalent", out bool hiddenEquivalent)
            && hiddenEquivalent) {
            return result.Text;
        }

        return TryGetString(result.StructuredContent, "category", out string? category)
            && !string.IsNullOrWhiteSpace(category)
            && !result.Text.Contains(category, StringComparison.Ordinal)
            ? category + Environment.NewLine + result.Text
            : result.Text;
    }

    private static bool TryGetBoolean(JsonObject metadata, string key, out bool value) {
        if (metadata.TryGetPropertyValue(key, out JsonNode? node) && node is not null) {
            value = node.GetValue<bool>();
            return true;
        }

        value = false;
        return false;
    }

    private static bool TryGetString(JsonObject metadata, string key, out string? value) {
        if (metadata.TryGetPropertyValue(key, out JsonNode? node) && node is not null) {
            value = node.GetValue<string>();
            return true;
        }

        value = null;
        return false;
    }
}
