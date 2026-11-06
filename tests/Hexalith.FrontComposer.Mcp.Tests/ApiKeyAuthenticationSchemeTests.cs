using System.Security.Claims;

using Hexalith.FrontComposer.Mcp.Extensions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests;

/// <summary>
/// E14R-AI-3 / D6: the FrontComposer MCP API-key scheme outcomes, observed through
/// <see cref="AuthenticationHttpContextExtensions.AuthenticateAsync(HttpContext, string)"/>.
/// </summary>
public sealed class ApiKeyAuthenticationSchemeTests {
    private const string McpApiKeyHeader = "X-FrontComposer-Mcp-Key";
    private const string ValidKey = "scheme-valid-key-01";

    [Fact]
    public async Task MissingHeaderYieldsNoResult() {
        await using ServiceProvider provider = BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        DefaultHttpContext http = CreateContext(scope);

        AuthenticateResult result = await http.AuthenticateAsync(FrontComposerMcpAuthenticationDefaults.ApiKeyScheme).ConfigureAwait(true);

        result.None.ShouldBeTrue();
        result.Succeeded.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("scheme-unknown-key-02")]
    [InlineData(" scheme-valid-key-01")]
    public async Task BlankOrUnmatchedHeaderFailsWithoutEchoingTheKey(string candidate) {
        await using ServiceProvider provider = BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        DefaultHttpContext http = CreateContext(scope);
        http.Request.Headers[McpApiKeyHeader] = candidate;

        AuthenticateResult result = await http.AuthenticateAsync(FrontComposerMcpAuthenticationDefaults.ApiKeyScheme).ConfigureAwait(true);

        AssertFixedFailure(result, candidate);
    }

    [Fact]
    public async Task MultiValuedHeaderFailsEvenWhenEveryValueMatches() {
        await using ServiceProvider provider = BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        DefaultHttpContext http = CreateContext(scope);
        http.Request.Headers[McpApiKeyHeader] = new StringValues([ValidKey, ValidKey]);

        AuthenticateResult result = await http.AuthenticateAsync(FrontComposerMcpAuthenticationDefaults.ApiKeyScheme).ConfigureAwait(true);

        AssertFixedFailure(result, ValidKey);
    }

    [Fact]
    public async Task MatchedKeyBuildsTenantAndUserIdentityNamedAfterTheScheme() {
        await using ServiceProvider provider = BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        DefaultHttpContext http = CreateContext(scope);
        http.Request.Headers[McpApiKeyHeader] = ValidKey;

        AuthenticateResult result = await http.AuthenticateAsync(FrontComposerMcpAuthenticationDefaults.ApiKeyScheme).ConfigureAwait(true);

        result.Succeeded.ShouldBeTrue();
        ClaimsPrincipal principal = result.Principal.ShouldNotBeNull();
        principal.Identity.ShouldNotBeNull().IsAuthenticated.ShouldBeTrue();
        principal.Identity.AuthenticationType.ShouldBe(FrontComposerMcpAuthenticationDefaults.ApiKeyScheme);
        principal.Claims.Select(claim => (claim.Type, claim.Value)).ShouldBe([("TenantId", "tenant-a"), ("UserId", "agent-a")]);
        result.Ticket.ShouldNotBeNull().AuthenticationScheme.ShouldBe(FrontComposerMcpAuthenticationDefaults.ApiKeyScheme);
    }

    [Fact]
    public async Task CustomSchemeNameAndHeaderNameAreHonored() {
        const string CustomScheme = "AgentKeys";
        const string CustomHeader = "X-Agent-Key";
        await using ServiceProvider provider = BuildProvider(CustomScheme, options => options.ApiKeyHeaderName = CustomHeader);

        await using AsyncServiceScope defaultHeaderScope = provider.CreateAsyncScope();
        DefaultHttpContext defaultHeader = CreateContext(defaultHeaderScope);
        defaultHeader.Request.Headers[McpApiKeyHeader] = ValidKey;
        (await defaultHeader.AuthenticateAsync(CustomScheme).ConfigureAwait(true)).None.ShouldBeTrue();

        await using AsyncServiceScope customHeaderScope = provider.CreateAsyncScope();
        DefaultHttpContext customHeader = CreateContext(customHeaderScope);
        customHeader.Request.Headers[CustomHeader] = ValidKey;
        AuthenticateResult result = await customHeader.AuthenticateAsync(CustomScheme).ConfigureAwait(true);

        result.Succeeded.ShouldBeTrue();
        result.Principal.ShouldNotBeNull().Identity.ShouldNotBeNull().AuthenticationType.ShouldBe(CustomScheme);
    }

    [Fact]
    public async Task SchemeAndInHandlerAccessorResolveTheSameTrimmedCallerForAPaddedIdentity() {
        await using ServiceProvider provider = BuildProvider(
            configure: options => options.ApiKeys[ValidKey] = new FrontComposerMcpApiKeyIdentity(" tenant-a ", " agent-a "));
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        DefaultHttpContext http = CreateContext(scope);
        http.Request.Headers[McpApiKeyHeader] = ValidKey;
        AuthenticateResult result = await http.AuthenticateAsync(FrontComposerMcpAuthenticationDefaults.ApiKeyScheme).ConfigureAwait(true);
        http.User = result.Principal.ShouldNotBeNull();

        FrontComposerMcpAgentContext context = ResolveContext(provider, scope, http);

        http.User.Claims.Select(claim => (claim.Type, claim.Value)).ShouldBe([("TenantId", "tenant-a"), ("UserId", "agent-a")]);
        context.TenantId.ShouldBe("tenant-a");
        context.UserId.ShouldBe("agent-a");
        context.Principal.FindFirst("TenantId")!.Value.ShouldBe(context.TenantId);
        context.Principal.FindFirst("UserId")!.Value.ShouldBe(context.UserId);
    }

    [Theory]
    [InlineData("TenantId", "tenant-b")]
    [InlineData("tid", "tenant-b")]
    [InlineData("sub", "agent-b")]
    [InlineData("UserId", "agent-b")]
    public async Task AccessorRejectsKeyWhenOtherCredentialsCarryAConflictingIdentity(string claimType, string claimValue) {
        await using ServiceProvider provider = BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        DefaultHttpContext http = await AuthenticateWithOtherCredentialsAsync(scope, new Claim(claimType, claimValue)).ConfigureAwait(true);

        FrontComposerMcpException error = Should.Throw<FrontComposerMcpException>(() => ResolveContext(provider, scope, http));

        error.Category.ShouldBe(FrontComposerMcpFailureCategory.AuthFailed);
    }

    [Fact]
    public async Task AccessorAcceptsKeyWhenOtherCredentialsCarryTheSameIdentity() {
        await using ServiceProvider provider = BuildProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        DefaultHttpContext http = await AuthenticateWithOtherCredentialsAsync(
            scope,
            new Claim("tid", " tenant-a "),
            new Claim("sub", "agent-a")).ConfigureAwait(true);

        FrontComposerMcpAgentContext context = ResolveContext(provider, scope, http);

        context.TenantId.ShouldBe("tenant-a");
        context.UserId.ShouldBe("agent-a");
    }

    [Fact]
    public void BlankSchemeNameIsRejected() {
        ServiceCollection services = new();

        _ = Should.Throw<ArgumentException>(() => services.AddAuthentication().AddFrontComposerMcpApiKey(" "));
    }

    private static ServiceProvider BuildProvider(
        string scheme = FrontComposerMcpAuthenticationDefaults.ApiKeyScheme,
        Action<FrontComposerMcpOptions>? configure = null) {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        _ = services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
        _ = services.AddAuthentication(scheme).AddFrontComposerMcpApiKey(scheme);
        _ = services.AddFrontComposerMcp(options => {
            options.ApiKeys[ValidKey] = new FrontComposerMcpApiKeyIdentity("tenant-a", "agent-a");
            configure?.Invoke(options);
        });
        return services.BuildServiceProvider();
    }

    // A mixed-scheme policy merges the API-key principal with the other scheme's identity, as simulated here.
    private static async Task<DefaultHttpContext> AuthenticateWithOtherCredentialsAsync(AsyncServiceScope scope, params Claim[] otherClaims) {
        DefaultHttpContext http = CreateContext(scope);
        http.Request.Headers[McpApiKeyHeader] = ValidKey;
        AuthenticateResult result = await http.AuthenticateAsync(FrontComposerMcpAuthenticationDefaults.ApiKeyScheme).ConfigureAwait(true);
        ClaimsPrincipal merged = result.Principal.ShouldNotBeNull();
        merged.AddIdentity(new ClaimsIdentity(otherClaims, "Bearer"));
        http.User = merged;
        return http;
    }

    private static FrontComposerMcpAgentContext ResolveContext(ServiceProvider provider, AsyncServiceScope scope, HttpContext http) {
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = http;
        return scope.ServiceProvider.GetRequiredService<IFrontComposerMcpAgentContextAccessor>().GetContext();
    }

    // Authentication handlers are cached per request scope, so every simulated request gets its own scope.
    private static DefaultHttpContext CreateContext(AsyncServiceScope scope)
        => new() { RequestServices = scope.ServiceProvider };

    private static void AssertFixedFailure(AuthenticateResult result, string candidate) {
        result.Succeeded.ShouldBeFalse();
        result.None.ShouldBeFalse();
        Exception failure = result.Failure.ShouldNotBeNull();
        failure.Message.ShouldBe(FrontComposerMcpApiKeyAuthenticationHandler.FailureMessage);
        if (!string.IsNullOrWhiteSpace(candidate)) {
            failure.Message.ShouldNotContain(candidate.Trim());
        }
    }
}
