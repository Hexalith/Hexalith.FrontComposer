using System.Security.Claims;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Auth;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Auth;

public sealed class FrontComposerAccessTokenProviderTests {
    [Fact]
    public async Task GetAccessTokenAsync_InvokesHostProviderPerOperation() {
        int calls = 0;
        FrontComposerAccessTokenProvider sut = Build(options => {
            options.CustomBrokered.Enabled = true;
            options.TokenRelay.HostAccessTokenProvider = _ => {
                calls++;
                return ValueTask.FromResult<string?>("token-" + calls.ToString(System.Globalization.CultureInfo.InvariantCulture));
            };
        });

        string? first = await sut.GetAccessTokenAsync(TestContext.Current.CancellationToken);
        string? second = await sut.GetAccessTokenAsync(TestContext.Current.CancellationToken);

        first.ShouldBe("token-1");
        second.ShouldBe("token-2");
        calls.ShouldBe(2);
    }

    [Fact]
    public async Task GetAccessTokenAsync_Fails_WhenGitHubOAuthHasNoBrokeredToken() {
        FrontComposerAccessTokenProvider sut = Build(options => {
            options.GitHubOAuth.Enabled = true;
        });

        FrontComposerAuthenticationException ex = await Should.ThrowAsync<FrontComposerAuthenticationException>(
            () => sut.GetAccessTokenAsync(TestContext.Current.CancellationToken).AsTask());

        ex.DiagnosticId.ShouldBe(FcDiagnosticIds.HFC2014_GitHubTokenExchangeRequired);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ObservesCancellationBeforeHostProvider() {
        bool called = false;
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();
        FrontComposerAccessTokenProvider sut = Build(options => {
            options.CustomBrokered.Enabled = true;
            options.TokenRelay.HostAccessTokenProvider = _ => {
                called = true;
                return ValueTask.FromResult<string?>("token");
            };
        });

        _ = await Should.ThrowAsync<OperationCanceledException>(
            () => sut.GetAccessTokenAsync(cts.Token).AsTask());

        called.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAccessTokenAsync_LogsHFC2013AtWarning_WhenHostProviderReturnsEmpty() {
        // P6/P26 — empty-token branch is now logged with HFC2013 at Warning so operators can find it.
        CapturingLogger<FrontComposerAccessTokenProvider> logger = new();
        FrontComposerAccessTokenProvider sut = Build(
            options => {
                options.CustomBrokered.Enabled = true;
                options.TokenRelay.HostAccessTokenProvider = _ => ValueTask.FromResult<string?>("");
            },
            logger);

        FrontComposerAuthenticationException ex = await Should.ThrowAsync<FrontComposerAuthenticationException>(
            () => sut.GetAccessTokenAsync(TestContext.Current.CancellationToken).AsTask());

        ex.DiagnosticId.ShouldBe(FcDiagnosticIds.HFC2013_AuthenticationTokenRelayFailed);
        logger.Entries.ShouldContain(e => e.Level == LogLevel.Warning
            && e.Message.Contains(FcDiagnosticIds.HFC2013_AuthenticationTokenRelayFailed, StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetAccessTokenAsync_PropagatesInnerExceptionFromHostProvider() {
        // P6 — adopter token-broker errors must surface as inner exception so operators can diagnose.
        InvalidOperationException root = new("token broker offline");
        FrontComposerAccessTokenProvider sut = Build(options => {
            options.CustomBrokered.Enabled = true;
            options.TokenRelay.HostAccessTokenProvider = _ => throw root;
        });

        FrontComposerAuthenticationException ex = await Should.ThrowAsync<FrontComposerAuthenticationException>(
            () => sut.GetAccessTokenAsync(TestContext.Current.CancellationToken).AsTask());

        ex.DiagnosticId.ShouldBe(FcDiagnosticIds.HFC2013_AuthenticationTokenRelayFailed);
        ex.InnerException.ShouldBe(root);
        ex.Message.ShouldNotContain("token broker offline"); // sanitized message
    }

    [Fact]
    public async Task GetAccessTokenAsync_ThrowsHFC2013_WhenHttpContextIsAbsent_AndHostProviderUnconfigured() {
        // P26 — non-HTTP scope (background SignalR reconnect, hosted service) must surface
        // a sanitized HFC2013 rather than NRE on null HttpContext.
        FrontComposerAccessTokenProvider sut = Build(options => {
            options.OpenIdConnect.Enabled = true;
            options.OpenIdConnect.Authority = new Uri("https://identity.test/realms/demo");
            options.OpenIdConnect.ClientId = "frontcomposer";
            options.OpenIdConnect.ClientSecret = "secret";
            options.OpenIdConnect.Audience = "api";
        });

        FrontComposerAuthenticationException ex = await Should.ThrowAsync<FrontComposerAuthenticationException>(
            () => sut.GetAccessTokenAsync(TestContext.Current.CancellationToken).AsTask());

        ex.DiagnosticId.ShouldBe(FcDiagnosticIds.HFC2013_AuthenticationTokenRelayFailed);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ReturnsTokenFromHttpContext_ForOidcSavedTokens() {
        // P25 — exercise the OIDC HttpContext.GetTokenAsync path. Cookie auth saves an
        // access_token property; provider should read it and return.
        DefaultHttpContext context = BuildContextWithSavedAccessToken("forwarded-token");
        HttpContextAccessor http = new() { HttpContext = context };

        FrontComposerAuthenticationOptions options = new();
        options.OpenIdConnect.Enabled = true;
        options.OpenIdConnect.Authority = new Uri("https://identity.test/realms/demo");
        options.OpenIdConnect.ClientId = "frontcomposer";
        options.OpenIdConnect.ClientSecret = "secret";
        options.OpenIdConnect.Audience = "api";
        options.TenantClaimTypes.Add("tenant_id");
        options.UserClaimTypes.Add("sub");
        FrontComposerAccessTokenProvider sut = new(http, Microsoft.Extensions.Options.Options.Create(options), NullLogger<FrontComposerAccessTokenProvider>.Instance);

        string? token = await sut.GetAccessTokenAsync(TestContext.Current.CancellationToken);

        token.ShouldBe("forwarded-token");
    }

    [Fact]
    public async Task GetAccessTokenAsync_GitHubPrincipalAuthenticatesContext_ButRelayFailsClosed() {
        // P25 — "GitHub OAuth sign-in CAN authenticate the user context but cannot satisfy
        // EventStore bearer-token relay unless a broker/custom access-token provider is
        // explicitly configured." Tests the second half: relay fails-fast with HFC2014.
        FrontComposerAuthenticationOptions options = new();
        options.GitHubOAuth.Enabled = true;
        options.GitHubOAuth.ClientId = "github-client";
        options.GitHubOAuth.ClientSecret = "github-secret";
        options.TenantClaimTypes.Add("tenant_id");
        options.UserClaimTypes.Add("id");
        FrontComposerAccessTokenProvider sut = new(
            new HttpContextAccessor(),
            Microsoft.Extensions.Options.Options.Create(options),
            NullLogger<FrontComposerAccessTokenProvider>.Instance);

        FrontComposerAuthenticationException ex = await Should.ThrowAsync<FrontComposerAuthenticationException>(
            () => sut.GetAccessTokenAsync(TestContext.Current.CancellationToken).AsTask());

        ex.DiagnosticId.ShouldBe(FcDiagnosticIds.HFC2014_GitHubTokenExchangeRequired);
    }

    private static DefaultHttpContext BuildContextWithSavedAccessToken(string token) {
        DefaultHttpContext context = new();
        Microsoft.Extensions.DependencyInjection.ServiceCollection services = new();
        services.AddSingleton<Microsoft.AspNetCore.Authentication.IAuthenticationService>(new FakeBearerAuthenticationService(token));
        context.RequestServices = services.BuildServiceProvider();
        return context;
    }

    private sealed class FakeBearerAuthenticationService(string token) : Microsoft.AspNetCore.Authentication.IAuthenticationService {
        public Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) {
            ClaimsIdentity identity = new(authenticationType: "Test");
            ClaimsPrincipal principal = new(identity);
            Microsoft.AspNetCore.Authentication.AuthenticationProperties props = new();
            props.StoreTokens([new Microsoft.AspNetCore.Authentication.AuthenticationToken { Name = "access_token", Value = token }]);
            return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(
                new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, props, scheme ?? "Test")));
        }

        public Task ChallengeAsync(HttpContext context, string? scheme, Microsoft.AspNetCore.Authentication.AuthenticationProperties? properties) => Task.CompletedTask;
        public Task ForbidAsync(HttpContext context, string? scheme, Microsoft.AspNetCore.Authentication.AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, Microsoft.AspNetCore.Authentication.AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignOutAsync(HttpContext context, string? scheme, Microsoft.AspNetCore.Authentication.AuthenticationProperties? properties) => Task.CompletedTask;
    }

    private static FrontComposerAccessTokenProvider Build(
        Action<FrontComposerAuthenticationOptions> configure,
        ILogger<FrontComposerAccessTokenProvider>? logger = null) {
        FrontComposerAuthenticationOptions options = new();
        configure(options);
        return new FrontComposerAccessTokenProvider(
            new HttpContextAccessor(),
            Microsoft.Extensions.Options.Options.Create(options),
            logger ?? NullLogger<FrontComposerAccessTokenProvider>.Instance);
    }
}
