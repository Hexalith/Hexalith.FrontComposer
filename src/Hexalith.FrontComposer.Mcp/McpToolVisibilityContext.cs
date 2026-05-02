using System.Security.Claims;

namespace Hexalith.FrontComposer.Mcp;

public sealed record McpToolVisibilityContext(string TenantId, string UserId, ClaimsPrincipal Principal) {
    public static McpToolVisibilityContext FromAgentContext(FrontComposerMcpAgentContext context) {
        ArgumentNullException.ThrowIfNull(context);

        return new(context.TenantId, context.UserId, context.Principal);
    }
}
