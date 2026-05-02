using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests;

/// <summary>
/// Story 8-1 closure pass — AC11 / T8 auth-accessor coverage:
/// missing auth, malformed/empty/multi-valued API key, claim-missing, spoofed-tenant header, IdP claim preservation.
/// </summary>
public sealed class AuthContextAccessorTests {
    [Fact]
    public void NoAuth_NoApiKey_FailsClosed_AuthFailed() {
        var sut = BuildAccessor(out _, configure: null);
        FrontComposerMcpException ex = Should.Throw<FrontComposerMcpException>(() => sut.GetContext());
        ex.Category.ShouldBe(FrontComposerMcpFailureCategory.AuthFailed);
    }

    [Fact]
    public void EmptyApiKeyHeader_FailsClosed() {
        var sut = BuildAccessor(out HttpContext http, configure: o => o.ApiKeys["valid-key"] = new("tenant-a", "agent-a"));
        http.Request.Headers["X-FrontComposer-Mcp-Key"] = string.Empty;

        FrontComposerMcpException ex = Should.Throw<FrontComposerMcpException>(() => sut.GetContext());
        ex.Category.ShouldBe(FrontComposerMcpFailureCategory.AuthFailed);
    }

    [Fact]
    public void WhitespaceApiKeyHeader_FailsClosed() {
        var sut = BuildAccessor(out HttpContext http, configure: o => o.ApiKeys["valid-key"] = new("tenant-a", "agent-a"));
        http.Request.Headers["X-FrontComposer-Mcp-Key"] = "   ";

        FrontComposerMcpException ex = Should.Throw<FrontComposerMcpException>(() => sut.GetContext());
        ex.Category.ShouldBe(FrontComposerMcpFailureCategory.AuthFailed);
    }

    [Fact]
    public void MultiValuedApiKeyHeader_FailsClosed() {
        var sut = BuildAccessor(out HttpContext http, configure: o => o.ApiKeys["valid-key"] = new("tenant-a", "agent-a"));
        http.Request.Headers["X-FrontComposer-Mcp-Key"] = new StringValues(["valid-key", "extra"]);

        FrontComposerMcpException ex = Should.Throw<FrontComposerMcpException>(() => sut.GetContext());
        ex.Category.ShouldBe(FrontComposerMcpFailureCategory.AuthFailed);
    }

    [Fact]
    public void UnknownApiKey_FailsClosed() {
        var sut = BuildAccessor(out HttpContext http, configure: o => o.ApiKeys["valid-key"] = new("tenant-a", "agent-a"));
        http.Request.Headers["X-FrontComposer-Mcp-Key"] = "wrong-key";

        FrontComposerMcpException ex = Should.Throw<FrontComposerMcpException>(() => sut.GetContext());
        ex.Category.ShouldBe(FrontComposerMcpFailureCategory.AuthFailed);
    }

    [Fact]
    public void ValidApiKey_ResolvesContext() {
        var sut = BuildAccessor(out HttpContext http, configure: o => o.ApiKeys["valid-key"] = new("tenant-a", "agent-a"));
        http.Request.Headers["X-FrontComposer-Mcp-Key"] = "valid-key";

        FrontComposerMcpAgentContext context = sut.GetContext();
        context.TenantId.ShouldBe("tenant-a");
        context.UserId.ShouldBe("agent-a");
    }

    [Fact]
    public void ApiKey_TakesPrecedence_OverIdentityClaims() {
        // When both API key and authenticated claims are present, the API key wins. This avoids
        // privilege ambiguity if an attacker leaks both an API key and a stale session token.
        var sut = BuildAccessor(
            out HttpContext http,
            configure: o => o.ApiKeys["valid-key"] = new("tenant-from-key", "agent-from-key"));
        http.Request.Headers["X-FrontComposer-Mcp-Key"] = "valid-key";
        http.User = AuthenticatedUser(tenant: "tenant-from-claim", user: "agent-from-claim");

        FrontComposerMcpAgentContext context = sut.GetContext();
        context.TenantId.ShouldBe("tenant-from-key");
        context.UserId.ShouldBe("agent-from-key");
    }

    [Fact]
    public void AuthenticatedClaims_MissingTenant_FailsClosed_TenantMissing() {
        var sut = BuildAccessor(out HttpContext http, configure: null);
        http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "agent-a")], "jwt"));

        FrontComposerMcpException ex = Should.Throw<FrontComposerMcpException>(() => sut.GetContext());
        ex.Category.ShouldBe(FrontComposerMcpFailureCategory.TenantMissing);
    }

    [Fact]
    public void AuthenticatedClaims_WhitespaceTenant_FailsClosed_TenantMissing() {
        var sut = BuildAccessor(out HttpContext http, configure: null);
        http.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("TenantId", "   "),
            new Claim(ClaimTypes.NameIdentifier, "agent-a"),
        ], "jwt"));

        FrontComposerMcpException ex = Should.Throw<FrontComposerMcpException>(() => sut.GetContext());
        ex.Category.ShouldBe(FrontComposerMcpFailureCategory.TenantMissing);
    }

    [Fact]
    public void AuthenticatedClaims_NameIdentifierUri_Resolves() {
        // P-2 — the default claim list must resolve a real OIDC/JWT principal whose user id is
        // mapped to the WS-* nameidentifier URI.
        var sut = BuildAccessor(out HttpContext http, configure: null);
        http.User = AuthenticatedUser(tenant: "tenant-a", user: "agent-a");

        FrontComposerMcpAgentContext context = sut.GetContext();
        context.TenantId.ShouldBe("tenant-a");
        context.UserId.ShouldBe("agent-a");
    }

    [Fact]
    public void AuthenticatedClaims_PreservesIdpRoles_ForFutureGate() {
        // P-6 — Story 8-2 will read role/group claims from context.Principal; the synthetic
        // principal must carry through every claim other than TenantId/UserId duplicates.
        var sut = BuildAccessor(out HttpContext http, configure: null);
        http.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("TenantId", "tenant-a"),
            new Claim(ClaimTypes.NameIdentifier, "agent-a"),
            new Claim(ClaimTypes.Role, "Approver"),
            new Claim("custom-claim", "value"),
        ], "jwt"));

        FrontComposerMcpAgentContext context = sut.GetContext();
        context.Principal.HasClaim(ClaimTypes.Role, "Approver").ShouldBeTrue();
        context.Principal.HasClaim("custom-claim", "value").ShouldBeTrue();

        // TenantId/UserId are normalized — only one of each, with the trimmed value.
        context.Principal.FindAll("TenantId").Count().ShouldBe(1);
        context.Principal.FindAll("UserId").Count().ShouldBe(1);
    }

    [Fact]
    public void SpoofedTenantHeader_DoesNotInfluenceAuthDecision() {
        // Tenant only flows from API-key identity or authenticated claims; arbitrary HTTP headers
        // must never resolve to a tenant context.
        var sut = BuildAccessor(out HttpContext http, configure: null);
        http.Request.Headers["X-Tenant-Id"] = "attacker-tenant";

        FrontComposerMcpException ex = Should.Throw<FrontComposerMcpException>(() => sut.GetContext());
        ex.Category.ShouldBe(FrontComposerMcpFailureCategory.AuthFailed);
    }

    [Fact]
    public void EmptyStringApiKey_InOptions_NeverMatches() {
        // Misconfiguration guard — registering ApiKeys[""] = ... must not authenticate empty headers.
        var sut = BuildAccessor(out HttpContext http, configure: o => o.ApiKeys[""] = new("tenant-bad", "agent-bad"));
        http.Request.Headers["X-FrontComposer-Mcp-Key"] = string.Empty;

        FrontComposerMcpException ex = Should.Throw<FrontComposerMcpException>(() => sut.GetContext());
        ex.Category.ShouldBe(FrontComposerMcpFailureCategory.AuthFailed);
    }

    private static IFrontComposerMcpAgentContextAccessor BuildAccessor(
        out HttpContext http,
        Action<FrontComposerMcpOptions>? configure) {
        DefaultHttpContext context = new();
        FrontComposerMcpOptions options = new();
        configure?.Invoke(options);
        TestHttpContextAccessor accessor = new(context);
        IOptions<FrontComposerMcpOptions> wrapped = Options.Create(options);
        http = context;
        return new HttpFrontComposerMcpAgentContextAccessor(accessor, wrapped);
    }

    private static ClaimsPrincipal AuthenticatedUser(string tenant, string user)
        => new(new ClaimsIdentity([
            new Claim("TenantId", tenant),
            new Claim(ClaimTypes.NameIdentifier, user),
        ], authenticationType: "jwt"));

    private sealed class TestHttpContextAccessor(HttpContext context) : IHttpContextAccessor {
        public HttpContext? HttpContext { get; set; } = context;
    }
}
