using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed class FrontComposerSkillMcpResource(
    SkillResourceDescriptor descriptor,
    FrontComposerSkillResourceProvider provider) : McpServerResource, IMcpServerPrimitive {
    private readonly ResourceTemplate _resourceTemplate = new() {
        UriTemplate = descriptor.ResourceUri,
        Name = descriptor.Id,
        Title = descriptor.Title,
        Description = descriptor.Description,
        MimeType = descriptor.ContentType,
    };

    private readonly Resource _resource = new() {
        Uri = descriptor.ResourceUri,
        Name = descriptor.Id,
        Title = descriptor.Title,
        Description = descriptor.Description,
        MimeType = descriptor.ContentType,
    };

    public SkillResourceDescriptor Descriptor => descriptor;

    public override Resource ProtocolResource => _resource;

    string IMcpServerPrimitive.Id => descriptor.ResourceUri;

    public override ResourceTemplate ProtocolResourceTemplate
        => _resourceTemplate;

    public override IReadOnlyList<object> Metadata { get; } = [descriptor];

    // P-37: URIs are canonical lowercase Ordinal — match exactly.
    public override bool IsMatch(string uri)
        => string.Equals(uri, descriptor.ResourceUri, StringComparison.Ordinal);

    public override ValueTask<ReadResourceResult> ReadAsync(
        RequestContext<ReadResourceRequestParams> request,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        string? uri = request.Params?.Uri;
        SkillResourceReadResult result = string.IsNullOrWhiteSpace(uri)
            ? SkillResourceReadResult.Failure(FrontComposerMcpFailureCategory.MalformedRequest)
            : provider.Read(uri, cancellationToken);

        // P-12: echo the requested URI when present so the response Uri matches the request,
        // not the descriptor (which would lie about the routed URI for a malformed/missing
        // request). Fall back to the descriptor's URI only when the caller supplied none.
        string responseUri = string.IsNullOrWhiteSpace(uri) ? descriptor.ResourceUri : uri;

        return ValueTask.FromResult(new ReadResourceResult {
            Contents = [
                new TextResourceContents {
                    Uri = responseUri,
                    MimeType = result.ContentType,
                    Text = result.Markdown,
                },
            ],
        });
    }
}
