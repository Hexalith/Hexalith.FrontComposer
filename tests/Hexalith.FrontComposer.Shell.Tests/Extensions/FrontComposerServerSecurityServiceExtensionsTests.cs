using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Services.Auth;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Extensions;

public sealed class FrontComposerServerSecurityServiceExtensionsTests {
    [Fact]
    public async Task AddHexalithFrontComposerServerAuthenticationState_ReplacesNullProviderWithServerProvider() {
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposer();
        // Sanity: the Quickstart/base registers the fail-closed anonymous provider.
        _ = services.AddHexalithFrontComposerServerAuthenticationState();

        await using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>()
            .ShouldBeOfType<ServerAuthenticationStateProvider>();
    }

    [Fact]
    public async Task AddHexalithFrontComposerTokenRelay_RegistersCircuitSafeRelayServices() {
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposerTokenRelay();

        await using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<FrontComposerUserTokenStore>().ShouldNotBeNull();
        provider.GetRequiredService<CircuitServicesAccessor>().ShouldNotBeNull();
        using IServiceScope scope = provider.CreateScope();
        scope.ServiceProvider.GetServices<CircuitHandler>()
            .ShouldContain(handler => handler is FrontComposerCircuitServicesHandler);
        scope.ServiceProvider.GetRequiredService<FrontComposerGatewayAuthorizationHandler>().ShouldNotBeNull();
    }

    [Fact]
    public async Task GatewayAuthorizationHandler_AttachesBearerForAuthenticatedUserWithStoredToken() {
        FrontComposerUserTokenStore store = new();
        const string token = "access-token-1";
        store.Set("user-1", token, DateTimeOffset.UtcNow.AddMinutes(5));
        HttpContextAccessor httpContextAccessor = new() {
            HttpContext = new DefaultHttpContext {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-1")], "test")),
            },
        };
        CapturingHandler inner = new();
        FrontComposerGatewayAuthorizationHandler handler = new(httpContextAccessor, new CircuitServicesAccessor(), store) {
            InnerHandler = inner,
        };
        using HttpMessageInvoker invoker = new(handler);

        using HttpRequestMessage request = new(HttpMethod.Get, "https://gateway.test/resource");
        using HttpResponseMessage response = await invoker.SendAsync(request, TestContext.Current.CancellationToken);

        inner.LastRequest!.Headers.Authorization!.Scheme.ShouldBe("Bearer");
        (inner.LastRequest.Headers.Authorization.Parameter == token).ShouldBeTrue("the outbound request should receive the stored bearer token");
    }

    [Fact]
    public async Task GatewayAuthorizationHandler_LeavesAnonymousRequestUntouched() {
        FrontComposerUserTokenStore store = new();
        HttpContextAccessor httpContextAccessor = new() {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) },
        };
        CapturingHandler inner = new();
        FrontComposerGatewayAuthorizationHandler handler = new(httpContextAccessor, new CircuitServicesAccessor(), store) {
            InnerHandler = inner,
        };
        using HttpMessageInvoker invoker = new(handler);

        using HttpRequestMessage request = new(HttpMethod.Get, "https://gateway.test/resource");
        using HttpResponseMessage response = await invoker.SendAsync(request, TestContext.Current.CancellationToken);

        inner.LastRequest!.Headers.Authorization.ShouldBeNull();
    }

    [Fact]
    public async Task AddHexalithFrontComposerServerSecurity_ComposesBridgeStateAndRelay() {
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposer();
        _ = services.AddHexalithFrontComposerServerSecurity(options => {
            options.CustomBrokered.Enabled = true;
            options.TenantClaimTypes.Add("eventstore:tenant");
            options.UserClaimTypes.Add("sub");
            options.TokenRelay.HostAccessTokenProvider = _ => ValueTask.FromResult<string?>("token");
        });

        await using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        // Auth bridge swapped the user-context seam: the server security wiring replaces the
        // HttpContext-only accessor with the circuit-aware ServerCircuitUserContextAccessor so
        // interactive Server components resolve the signed-in user when HttpContext is null.
        scope.ServiceProvider.GetRequiredService<IUserContextAccessor>()
            .ShouldBeOfType<ServerCircuitUserContextAccessor>();
        // Server auth-state provider in place.
        scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>()
            .ShouldBeOfType<ServerAuthenticationStateProvider>();
        // Token relay registered.
        provider.GetRequiredService<FrontComposerUserTokenStore>().ShouldNotBeNull();
    }

    [Fact]
    public async Task AddHexalithFrontComposerServerSecurity_AllowsOidcCircuitTokenSource() {
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposer();
        _ = services.AddHexalithFrontComposerServerSecurity(options => options.UseKeycloak(
            new Uri("https://keycloak.test/realms/test"),
            clientId: "client",
            clientSecret: "secret",
            tenantClaimType: "eventstore:tenant",
            userClaimType: "sub"));

        await using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<FrontComposerUserTokenStore>().ShouldNotBeNull();
    }

    private sealed class CapturingHandler : HttpMessageHandler {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
