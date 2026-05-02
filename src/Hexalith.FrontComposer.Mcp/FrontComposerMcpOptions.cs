using System.Reflection;
using System.Security.Claims;

using Hexalith.FrontComposer.Contracts.Mcp;

namespace Hexalith.FrontComposer.Mcp;

/// <summary>
/// Options for the FrontComposer MCP adapter.
/// </summary>
public sealed class FrontComposerMcpOptions {
    public IList<McpManifest> Manifests { get; } = [];

    public IList<Assembly> ManifestAssemblies { get; } = [];

    public string EndpointPattern { get; set; } = "/mcp";

    public string ApiKeyHeaderName { get; set; } = "X-FrontComposer-Mcp-Key";

    public IDictionary<string, FrontComposerMcpApiKeyIdentity> ApiKeys { get; } = new Dictionary<string, FrontComposerMcpApiKeyIdentity>(StringComparer.Ordinal);

    public int MaxArgumentBytes { get; set; } = 32 * 1024;

    public int DefaultResourceTake { get; set; } = 50;

    public int MaxResourceTake { get; set; } = 200;

    public int MaxRowsPerResource { get; set; } = 50;

    public int MaxFieldsPerResource { get; set; } = 8;

    public int MaxVisibleToolListItems { get; set; } = 50;

    public int MaxSuggestionCandidates { get; set; } = 100;

    public int MaxToolNameLength { get; set; } = 160;

    public int MaxToolDisplayTextLength { get; set; } = 240;

    public IList<string> TenantClaimTypes { get; } = [
        "TenantId",
        "tenant_id",
        "tid",
        "http://schemas.microsoft.com/identity/claims/tenantid",
    ];

    public IList<string> UserClaimTypes { get; } = [
        "UserId",
        "sub",
        ClaimTypes.NameIdentifier,
        "oid",
        "http://schemas.microsoft.com/identity/claims/objectidentifier",
    ];
}

public sealed record FrontComposerMcpApiKeyIdentity(string TenantId, string UserId);
