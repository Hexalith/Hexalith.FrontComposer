using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp.Invocation;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.FrontComposer.Mcp;

internal sealed class FrontComposerMcpTool(McpCommandDescriptor descriptor) : McpServerTool {
    private readonly Tool _tool = new() {
        Name = descriptor.ProtocolName,
        Title = descriptor.Title,
        Description = descriptor.Description,
        InputSchema = McpJsonSchemaBuilder.BuildInputSchema(descriptor.Parameters),
    };

    public override Tool ProtocolTool => _tool;

    public override IReadOnlyList<object> Metadata { get; } = [descriptor];

    public override async ValueTask<CallToolResult> InvokeAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken = default) {
        if (request.Services is null) {
            return BuildResult(FrontComposerMcpResult.Failure(FrontComposerMcpFailureCategory.DownstreamFailed));
        }

        FrontComposerMcpCommandInvoker invoker = request.Services.GetRequiredService<FrontComposerMcpCommandInvoker>();
        FrontComposerMcpResult result = await invoker.InvokeAsync(
            descriptor.ProtocolName,
            request.Params?.Arguments is null ? null : new Dictionary<string, JsonElement>(request.Params.Arguments, StringComparer.Ordinal),
            cancellationToken).ConfigureAwait(false);

        return BuildResult(result);
    }

    private static CallToolResult BuildResult(FrontComposerMcpResult result) => new() {
        IsError = result.IsError,
        StructuredContent = result.StructuredContent?.Deserialize<JsonElement>(),
        Content = [new TextContentBlock { Text = result.Text }],
    };
}
