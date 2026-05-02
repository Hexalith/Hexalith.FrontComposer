using System.Security.Claims;

namespace Hexalith.FrontComposer.Mcp;

public interface IFrontComposerMcpAgentContextAccessor {
    FrontComposerMcpAgentContext GetContext();
}

public sealed record FrontComposerMcpAgentContext(string TenantId, string UserId, ClaimsPrincipal Principal);
