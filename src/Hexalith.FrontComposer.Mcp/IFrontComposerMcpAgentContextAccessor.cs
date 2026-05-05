using System.Security.Claims;

using Hexalith.FrontComposer.Contracts.Schema;

namespace Hexalith.FrontComposer.Mcp;

public interface IFrontComposerMcpAgentContextAccessor {
    SchemaFingerprint? ClientFingerprintHint => null;

    IServiceProvider? RequestServices => null;

    FrontComposerMcpAgentContext GetContext();
}

public sealed record FrontComposerMcpAgentContext(string TenantId, string UserId, ClaimsPrincipal Principal);
