using System.Security.Claims;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Auth;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Auth;

public sealed class FrontComposerCredentialIsolationTests
{
    [Theory]
    [InlineData("shared", "tenant-b")]
    [InlineData("shared", "tenant-a")]
    [InlineData("other", "tenant-b")]
    public async Task ConcurrentCircuits_RelayOnlyTheirSessionCredential(string secondSubject, string secondTenant)
    {
        FakeTimeProvider time = new();
        FrontComposerUserTokenStore store = new(time);
        FrontComposerTokenSession a = new("session-a", "shared", "tenant-a");
        FrontComposerTokenSession b = new("session-b", secondSubject, secondTenant);
        store.Set(a, "token-a", time.GetUtcNow().AddMinutes(5));
        store.Set(b, "token-b", time.GetUtcNow().AddMinutes(5));
        CircuitServicesAccessor circuits = new();
        // A stale request from B must not override the active A circuit.
        HttpContextAccessor http = new() { HttpContext = new DefaultHttpContext { User = Principal(b) } };
        IOptions<FrontComposerAuthenticationOptions> options = Options();
        FrontComposerAccessTokenProvider tokens = Provider(http, circuits, store, options);
        using HttpMessageInvoker gateway = Gateway(http, circuits, store, options);
        async Task verify(FrontComposerTokenSession session, string expected)
        {
            using ServiceProvider services = Services(Principal(session));
            circuits.Services = services;
            await Task.Yield();
            ServerCircuitUserContextAccessor userContext = new(http, circuits, options,
                NullLogger<ServerCircuitUserContextAccessor>.Instance);
            userContext.TenantId.ShouldBe(session.TenantId);
            userContext.UserId.ShouldBe(session.UserId);
            Func<CancellationToken, ValueTask<string?>> captured = (await tokens.CaptureCurrentUserAccessTokenProviderAsync(TestContext.Current.CancellationToken).ConfigureAwait(true))!;
            for (int i = 0; i < 10; i++)
            {
                using HttpRequestMessage request = new(HttpMethod.Get, "https://gateway.invalid/query");
                using HttpResponseMessage response = await gateway.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
                ((await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)) == expected).ShouldBeTrue();
                ((await tokens.GetAccessTokenAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)) == expected).ShouldBeTrue();
                await Task.Yield();
            }
            circuits.Services = null;
            ((await captured(TestContext.Current.CancellationToken).ConfigureAwait(true)) == expected).ShouldBeTrue();
        }
        await Task.WhenAll(verify(a, "token-a"), verify(b, "token-b"));
    }

    [Theory]
    [InlineData("missing-session")]
    [InlineData("unknown-session")]
    [InlineData("other-tenant")]
    [InlineData("other-user")]
    [InlineData("anonymous")]
    [InlineData("duplicate-tenant")]
    [InlineData("conflicting-tenant")]
    [InlineData("missing-tenant")]
    [InlineData("duplicate-session")]
    [InlineData("duplicate-subject")]
    public async Task InvalidBinding_CannotRecoverAnExistingCredential(string scenario)
    {
        FrontComposerTokenSession session = new("session-a", "shared", "tenant-a");
        FrontComposerUserTokenStore store = new();
        store.Set(session, "token-a", DateTimeOffset.UtcNow.AddMinutes(5));
        ClaimsPrincipal user = Principal(session);
        ClaimsIdentity identity = (ClaimsIdentity)user.Identity!;
        switch (scenario)
        {
            case "missing-session": identity.RemoveClaim(identity.FindFirst(FrontComposerTokenSession.ClaimType)!); break;
            case "unknown-session": user = Principal(session with { SessionId = "unknown" }); break;
            case "other-tenant": user = Principal(session with { TenantId = "tenant-b" }); break;
            case "other-user": user = Principal(session with { UserId = "other" }); break;
            case "anonymous": user = new ClaimsPrincipal(new ClaimsIdentity(identity.Claims)); break;
            case "duplicate-tenant": identity.AddClaim(new("tenant_id", "tenant-a")); break;
            case "conflicting-tenant": identity.AddClaim(new("current_tenant", "tenant-b")); break;
            case "missing-tenant": identity.RemoveClaim(identity.FindFirst("tenant_id")!); break;
            case "duplicate-session": identity.AddClaim(new(FrontComposerTokenSession.ClaimType, "session-a")); break;
            case "duplicate-subject": identity.AddClaim(new("sub", "shared")); break;
        }
        using ServiceProvider services = Services(user);
        CircuitServicesAccessor circuits = new() { Services = services };
        HttpContextAccessor http = new() { HttpContext = new DefaultHttpContext { User = Principal(session) } };
        IOptions<FrontComposerAuthenticationOptions> options = Options();
        using HttpMessageInvoker gateway = Gateway(http, circuits, store, options);
        using HttpRequestMessage request = new(HttpMethod.Get, "https://gateway.invalid/query");
        using HttpResponseMessage response = await gateway.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBeEmpty();
        FrontComposerAccessTokenProvider tokens = Provider(http, circuits, store, options);
        FrontComposerAuthenticationException error = await Should.ThrowAsync<FrontComposerAuthenticationException>(() => tokens.GetAccessTokenAsync(TestContext.Current.CancellationToken).AsTask());
        error.DiagnosticId.ShouldBe(FcDiagnosticIds.HFC2013_AuthenticationTokenRelayFailed);
        error.Message.ShouldNotContain("token-a");
        Func<CancellationToken, ValueTask<string?>> captured = (await tokens.CaptureCurrentUserAccessTokenProviderAsync(TestContext.Current.CancellationToken).ConfigureAwait(true))!;
        circuits.Services = Services(Principal(session));
        (await captured(TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBeNull();
        ((IDisposable)circuits.Services).Dispose();
    }

    [Fact]
    public async Task CapturedProvider_RechecksExpiryAndRevocation_WithoutAdoptingAmbientSession()
    {
        FakeTimeProvider time = new();
        FrontComposerUserTokenStore store = new(time);
        FrontComposerTokenSession a = new("session-a", "shared", "tenant-a");
        FrontComposerTokenSession b = new("session-b", "shared", "tenant-b");
        store.Set(a, "token-a", time.GetUtcNow().AddMinutes(1));
        store.Set(b, "token-b", time.GetUtcNow().AddMinutes(5));
        using ServiceProvider servicesA = Services(Principal(a));
        using ServiceProvider servicesB = Services(Principal(b));
        CircuitServicesAccessor circuits = new() { Services = servicesA };
        HttpContextAccessor http = new();
        FrontComposerAccessTokenProvider tokens = Provider(http, circuits, store, Options());
        Func<CancellationToken, ValueTask<string?>> captured = (await tokens.CaptureCurrentUserAccessTokenProviderAsync(TestContext.Current.CancellationToken).ConfigureAwait(true))!;
        circuits.Services = servicesB;
        ((await captured(TestContext.Current.CancellationToken).ConfigureAwait(true)) == "token-a").ShouldBeTrue();
        time.Advance(TimeSpan.FromMinutes(1));
        (await captured(TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBeNull();
        circuits.Services = servicesA;
        _ = await Should.ThrowAsync<FrontComposerAuthenticationException>(() => tokens.GetAccessTokenAsync(TestContext.Current.CancellationToken).AsTask());
        using HttpMessageInvoker gateway = Gateway(http, circuits, store, Options());
        using HttpRequestMessage request = new(HttpMethod.Get, "https://gateway.invalid/query");
        using HttpResponseMessage response = await gateway.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBeEmpty();
        circuits.Services = servicesB;
        ((await tokens.GetAccessTokenAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)) == "token-b").ShouldBeTrue();
        Func<CancellationToken, ValueTask<string?>> capturedB = (await tokens.CaptureCurrentUserAccessTokenProviderAsync(TestContext.Current.CancellationToken).ConfigureAwait(true))!;
        store.Remove(b);
        (await capturedB(TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task CapturedProvider_RejectsLaterTenantOrAuthenticationStateChanges()
    {
        FrontComposerTokenSession a = new("session-a", "shared", "tenant-a");
        FrontComposerUserTokenStore store = new();
        store.Set(a, "token-a", DateTimeOffset.UtcNow.AddMinutes(5));
        using ServiceProvider services = Services(Principal(a));
        MutableCircuitAuthenticationStateProvider state = (MutableCircuitAuthenticationStateProvider)services.GetRequiredService<AuthenticationStateProvider>();
        CircuitServicesAccessor circuits = new() { Services = services };
        FrontComposerAccessTokenProvider tokens = Provider(new HttpContextAccessor(), circuits, store, Options());
        Func<CancellationToken, ValueTask<string?>> captured = (await tokens.CaptureCurrentUserAccessTokenProviderAsync(TestContext.Current.CancellationToken).ConfigureAwait(true))!;
        state.Principal = Principal(a with { TenantId = "tenant-b" });
        (await captured(TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBeNull();
        state.Principal = new ClaimsPrincipal(new ClaimsIdentity());
        (await captured(TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBeNull();
    }

    [Fact]
    public async Task OpaqueSubject_WithColon_RemainsSeparateFromApplicationUserAlias()
    {
        FrontComposerTokenSession session = new("opaque-session", "issuer:user", "tenant-a");
        FrontComposerUserTokenStore store = new();
        store.Set(session, "opaque-token", DateTimeOffset.UtcNow.AddMinutes(5));
        ClaimsPrincipal principal = Principal(session);
        ((ClaimsIdentity)principal.Identity!).AddClaim(new("email", "alice@example.test"));
        IOptions<FrontComposerAuthenticationOptions> options = Options();
        options.Value.UserClaimTypes.Clear();
        options.Value.UserClaimTypes.Add("email");
        using ServiceProvider services = Services(principal);
        CircuitServicesAccessor circuits = new() { Services = services };
        HttpContextAccessor http = new();
        FrontComposerAccessTokenProvider provider = Provider(http, circuits, store, options);
        ((await provider.GetAccessTokenAsync(TestContext.Current.CancellationToken)) == "opaque-token").ShouldBeTrue();
        ServerCircuitUserContextAccessor userContext = new(http, circuits, options,
            NullLogger<ServerCircuitUserContextAccessor>.Instance);
        userContext.UserId.ShouldBe("alice@example.test");
        userContext.TenantId.ShouldBe("tenant-a");
    }

    private static ClaimsPrincipal Principal(FrontComposerTokenSession session) => new(new ClaimsIdentity([
        new Claim("sub", session.UserId), new Claim("tenant_id", session.TenantId),
        new Claim(FrontComposerTokenSession.ClaimType, session.SessionId)], "Test"));

    private static ServiceProvider Services(ClaimsPrincipal principal) => new ServiceCollection()
        .AddSingleton<AuthenticationStateProvider>(new MutableCircuitAuthenticationStateProvider(principal)).BuildServiceProvider();

    private static IOptions<FrontComposerAuthenticationOptions> Options() => Microsoft.Extensions.Options.Options.Create(
        new FrontComposerAuthenticationOptions
        {
            TenantClaimTypes = { "tenant_id", "current_tenant" }, UserClaimTypes = { "sub" },
            TokenRelay = { CircuitTokenSourceEnabled = true },
        });

    private static FrontComposerAccessTokenProvider Provider(HttpContextAccessor http, CircuitServicesAccessor circuits,
        FrontComposerUserTokenStore store, IOptions<FrontComposerAuthenticationOptions> options)
        => new(http, circuits, store, options, NullLogger<FrontComposerAccessTokenProvider>.Instance);

    private static HttpMessageInvoker Gateway(HttpContextAccessor http, CircuitServicesAccessor circuits,
        FrontComposerUserTokenStore store, IOptions<FrontComposerAuthenticationOptions> options)
        => new(new FrontComposerGatewayAuthorizationHandler(http, circuits, store, options) { InnerHandler = new CredentialEchoHandler() });
}
