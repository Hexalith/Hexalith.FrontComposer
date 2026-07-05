using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Auth;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Extensions;

public sealed class FrontComposerAuthenticationServiceExtensionsTests {
    [Fact]
    public async Task AddHexalithFrontComposerAuthentication_ReplacesDefaultAuthSeamsOnlyWhenConfigured() {
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposer();
        _ = services.AddHexalithFrontComposerAuthentication(options => {
            options.CustomBrokered.Enabled = true;
            options.TenantClaimTypes.Add("tenant_id");
            options.UserClaimTypes.Add("sub");
            options.TokenRelay.HostAccessTokenProvider = _ => ValueTask.FromResult<string?>("token");
        });

        await using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IUserContextAccessor>().ShouldBeOfType<ClaimsPrincipalUserContextAccessor>();
        provider.GetRequiredService<IAuthRedirector>().ShouldBeOfType<FrontComposerAuthRedirector>();
        provider.GetRequiredService<IOptions<FrontComposerAuthenticationOptions>>().Value.CustomBrokered.Enabled.ShouldBeTrue();
    }

    [Fact]
    public async Task AddHexalithFrontComposer_AloneLeavesNullSeamsInPlace() {
        // P20 — negative test: when AddHexalithFrontComposerAuthentication is NOT called,
        // IUserContextAccessor and IAuthRedirector resolve to the no-op defaults rather than
        // the auth bridge implementations.
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposer();

        await using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IUserContextAccessor>().ShouldNotBeOfType<ClaimsPrincipalUserContextAccessor>();
        provider.GetRequiredService<IAuthRedirector>().ShouldNotBeOfType<FrontComposerAuthRedirector>();
    }

    [Fact]
    public async Task AddHexalithFrontComposerAuthentication_WiresEventStoreAccessTokenProvider() {
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposer();
        _ = services.AddHexalithEventStore(options => options.BaseAddress = new Uri("https://eventstore.test"));
        _ = services.AddHexalithFrontComposerAuthentication(options => {
            options.CustomBrokered.Enabled = true;
            options.TenantClaimTypes.Add("tenant_id");
            options.UserClaimTypes.Add("sub");
            options.TokenRelay.HostAccessTokenProvider = _ => ValueTask.FromResult<string?>("token");
        });

        await using ServiceProvider provider = services.BuildServiceProvider();

        EventStoreOptions eventStore = provider.GetRequiredService<IOptions<EventStoreOptions>>().Value;
        eventStore.AccessTokenProvider.ShouldNotBeNull();
        // P34 — auth bridge forces RequireAccessToken=true.
        eventStore.RequireAccessToken.ShouldBeTrue();
        string? token = await eventStore.AccessTokenProvider!(TestContext.Current.CancellationToken);
        token.ShouldBe("token");
    }

    [Fact]
    public async Task AddHexalithFrontComposerAuthentication_AlwaysReplacesPreSetEventStoreTokenProvider() {
        // DN2/P32 — adopter-supplied AccessTokenProvider is replaced (not silently skipped) so
        // the GitHub broker check (HFC2014) and the token-relay diagnostics path remain reachable.
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposer();
        _ = services.AddHexalithEventStore(options => {
            options.BaseAddress = new Uri("https://eventstore.test");
            options.AccessTokenProvider = _ => ValueTask.FromResult<string?>("stale-adopter-token");
        });
        _ = services.AddHexalithFrontComposerAuthentication(options => {
            options.CustomBrokered.Enabled = true;
            options.TenantClaimTypes.Add("tenant_id");
            options.UserClaimTypes.Add("sub");
            options.TokenRelay.HostAccessTokenProvider = _ => ValueTask.FromResult<string?>("bridge-token");
        });

        await using ServiceProvider provider = services.BuildServiceProvider();

        EventStoreOptions eventStore = provider.GetRequiredService<IOptions<EventStoreOptions>>().Value;
        string? token = await eventStore.AccessTokenProvider!(TestContext.Current.CancellationToken);
        token.ShouldBe("bridge-token");
    }

    [Fact]
    public void AddHexalithFrontComposerAuthentication_RunsConfigureCallbackOnceForRegistration() {
        // P1 — `configure` is invoked exactly once at registration. The OptionsBuilder Configure
        // pipeline copies the captured snapshot rather than re-running adopter side-effects.
        int configureCalls = 0;
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposer();
        _ = services.AddHexalithFrontComposerAuthentication(options => {
            configureCalls++;
            options.CustomBrokered.Enabled = true;
            options.TenantClaimTypes.Add("tenant_id");
            options.UserClaimTypes.Add("sub");
            options.TokenRelay.HostAccessTokenProvider = _ => ValueTask.FromResult<string?>("token");
        });

        configureCalls.ShouldBe(1);
    }

    [Fact]
    public async Task AddHexalithFrontComposerAuthentication_DisablesInboundClaimMappingForOidc() {
        // P35 — regression for HFC2012 MissingClaim on `sub`: ASP.NET Core's default inbound claim
        // mapping renames `sub` to ClaimTypes.NameIdentifier, so the `sub` user-claim alias the
        // UseXxx recipes configure resolves to nothing and IUserContextAccessor.UserId is null
        // (self-scoped "My tenants" view fails closed). The OIDC handler must preserve raw claim
        // names so the alias contract holds.
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposer();
        _ = services.AddHexalithFrontComposerAuthentication(options => {
            options.UseKeycloak(
                new Uri("https://keycloak.test/realms/test"),
                clientId: "client",
                clientSecret: "secret",
                tenantClaimType: "eventstore:current-tenant",
                userClaimType: "sub");
            options.TokenRelay.HostAccessTokenProvider = _ => ValueTask.FromResult<string?>("token");
        });

        await using ServiceProvider provider = services.BuildServiceProvider();

        // Apply the registered configure actions to a fresh options instance rather than resolving
        // the post-configured handler (which pulls in data protection / the backchannel HttpClient).
        // A fresh OpenIdConnectOptions starts with MapInboundClaims=true, so a false result proves the
        // bridge's configure action ran.
        OpenIdConnectOptions oidc = new();
        string scheme = provider.GetRequiredService<IOptions<FrontComposerAuthenticationOptions>>()
            .Value.OpenIdConnect.ChallengeScheme;
        foreach (IConfigureOptions<OpenIdConnectOptions> configure in provider.GetServices<IConfigureOptions<OpenIdConnectOptions>>()) {
            if (configure is IConfigureNamedOptions<OpenIdConnectOptions> named) {
                named.Configure(scheme, oidc);
            }
            else {
                configure.Configure(oidc);
            }
        }

        oidc.MapInboundClaims.ShouldBeFalse();
    }

    [Fact]
    public async Task AddHexalithFrontComposerTokenRelay_CapturesOidcTokenExpiry() {
        DateTimeOffset now = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);
        FakeTimeProvider time = new(now);
        ServiceCollection services = new();
        services.AddSingleton<TimeProvider>(time);
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        _ = services.AddHexalithFrontComposerTokenRelay();

        await using ServiceProvider provider = services.BuildServiceProvider();
        OpenIdConnectOptions oidc = BuildOpenIdConnectOptions(provider);
        TokenValidatedContext context = BuildTokenValidatedContext(provider, oidc, "user-1", "opaque-token", expiresIn: "60");

        await oidc.Events.OnTokenValidated(context);

        FrontComposerUserTokenStore store = provider.GetRequiredService<FrontComposerUserTokenStore>();
        store.TryGet("user-1", out string token).ShouldBeTrue();
        (token == "opaque-token").ShouldBeTrue("the OIDC relay should store the access token before expiry");
        time.Advance(TimeSpan.FromSeconds(61));
        store.TryGet("user-1", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task SignOutEndpoint_RemovesStoredTokenForCurrentUser() {
        FrontComposerUserTokenStore store = new();
        const string token = "sign-out-token";
        store.Set("user-1", token, DateTimeOffset.UtcNow.AddMinutes(5));
        RecordingAuthenticationService auth = new();
        await using ServiceProvider provider = BuildEndpointProvider(store, auth, userClaimType: "email");
        RouteEndpoint endpoint = BuildEndpoint(provider, "/authentication/sign-out");
        DefaultHttpContext context = new() {
            RequestServices = provider,
            User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity([
                new System.Security.Claims.Claim("sub", "user-1"),
            ], "Test")),
        };

        await endpoint.RequestDelegate!(context);

        store.TryGet("user-1", out _).ShouldBeFalse();
        auth.SignOutCount.ShouldBe(1);
    }

    [Fact]
    public async Task SignOutEndpoint_RemovesStoredTokenByStableSubject_WhenUserAliasDiffers() {
        FrontComposerUserTokenStore store = new();
        const string token = "subject-token";
        store.Set("subject-1", token, DateTimeOffset.UtcNow.AddMinutes(5));
        RecordingAuthenticationService auth = new();
        await using ServiceProvider provider = BuildEndpointProvider(store, auth);
        RouteEndpoint endpoint = BuildEndpoint(provider, "/authentication/sign-out");
        DefaultHttpContext context = new() {
            RequestServices = provider,
            User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity([
                new System.Security.Claims.Claim("email", "alice@example.test"),
                new System.Security.Claims.Claim("sub", "subject-1"),
            ], "Test")),
        };

        await endpoint.RequestDelegate!(context);

        store.TryGet("subject-1", out _).ShouldBeFalse();
        auth.SignOutCount.ShouldBe(1);
    }

    [Fact]
    public async Task SignOutEndpoint_DoesNotThrowForAnonymousUser() {
        FrontComposerUserTokenStore store = new();
        RecordingAuthenticationService auth = new();
        await using ServiceProvider provider = BuildEndpointProvider(store, auth);
        RouteEndpoint endpoint = BuildEndpoint(provider, "/authentication/sign-out");
        DefaultHttpContext context = new() {
            RequestServices = provider,
            User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity()),
        };

        await endpoint.RequestDelegate!(context);

        auth.SignOutCount.ShouldBe(1);
    }

    [Fact]
    public async Task SignOutEndpoint_KeepsStoredToken_WhenSignOutFails() {
        FrontComposerUserTokenStore store = new();
        const string token = "sign-out-token";
        store.Set("user-1", token, DateTimeOffset.UtcNow.AddMinutes(5));
        RecordingAuthenticationService auth = new() { ThrowOnSignOut = true };
        await using ServiceProvider provider = BuildEndpointProvider(store, auth);
        RouteEndpoint endpoint = BuildEndpoint(provider, "/authentication/sign-out");
        DefaultHttpContext context = new() {
            RequestServices = provider,
            User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity([
                new System.Security.Claims.Claim("sub", "user-1"),
            ], "Test")),
        };

        _ = await Should.ThrowAsync<InvalidOperationException>(() => endpoint.RequestDelegate!(context));

        store.TryGet("user-1", out string stored).ShouldBeTrue();
        (stored == token).ShouldBeTrue("token eviction should happen only after the sign-out operation succeeds");
        auth.SignOutCount.ShouldBe(1);
    }

    [Fact]
    public void AddHexalithFrontComposerAuthentication_RejectsInvalidOptionsEagerly() {
        // P22 — eager validation throws before any handler is registered, preventing partial
        // DI state from leaking when ValidateOnStart later fires.
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposer();

        FrontComposerAuthenticationException ex = Should.Throw<FrontComposerAuthenticationException>(() =>
            services.AddHexalithFrontComposerAuthentication(options => {
                // No provider configured → should fail eagerly with HFC2011.
                options.TenantClaimTypes.Add("tenant_id");
                options.UserClaimTypes.Add("sub");
            }));

        ex.DiagnosticId.ShouldBe(Hexalith.FrontComposer.Contracts.Diagnostics.FcDiagnosticIds.HFC2011_AuthenticationConfigurationInvalid);
    }

    [Fact]
    public void AddHexalithFrontComposerAuthentication_RejectsOidcWithoutCircuitSafeTokenSource() {
        ServiceCollection services = new();
        _ = services.AddHexalithFrontComposer();

        FrontComposerAuthenticationException ex = Should.Throw<FrontComposerAuthenticationException>(() =>
            services.AddHexalithFrontComposerAuthentication(options => options.UseKeycloak(
                new Uri("https://keycloak.test/realms/test"),
                clientId: "client",
                clientSecret: "secret",
                tenantClaimType: "eventstore:current-tenant",
                userClaimType: "sub")));

        ex.DiagnosticId.ShouldBe(Hexalith.FrontComposer.Contracts.Diagnostics.FcDiagnosticIds.HFC2011_AuthenticationConfigurationInvalid);
        ex.Message.ShouldContain(Hexalith.FrontComposer.Contracts.Diagnostics.FcDiagnosticIds.HFC2013_AuthenticationTokenRelayFailed);
    }

    private static OpenIdConnectOptions BuildOpenIdConnectOptions(IServiceProvider provider) {
        OpenIdConnectOptions oidc = new();
        foreach (IConfigureOptions<OpenIdConnectOptions> configure in provider.GetServices<IConfigureOptions<OpenIdConnectOptions>>()) {
            if (configure is IConfigureNamedOptions<OpenIdConnectOptions> named) {
                named.Configure(FrontComposerTokenRelayServiceExtensions.OidcChallengeScheme, oidc);
            }
            else {
                configure.Configure(oidc);
            }
        }

        return oidc;
    }

    private static TokenValidatedContext BuildTokenValidatedContext(
        IServiceProvider provider,
        OpenIdConnectOptions oidc,
        string userId,
        string accessToken,
        string expiresIn) {
        DefaultHttpContext context = new() { RequestServices = provider };
        AuthenticationScheme scheme = new(
            FrontComposerTokenRelayServiceExtensions.OidcChallengeScheme,
            FrontComposerTokenRelayServiceExtensions.OidcChallengeScheme,
            typeof(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectHandler));
        System.Security.Claims.ClaimsPrincipal principal = new(new System.Security.Claims.ClaimsIdentity([
            new System.Security.Claims.Claim("sub", userId),
        ], "Test"));
        TokenValidatedContext validated = new(context, scheme, oidc, principal, new AuthenticationProperties()) {
            TokenEndpointResponse = new OpenIdConnectMessage {
                AccessToken = accessToken,
                ExpiresIn = expiresIn,
            },
        };
        return validated;
    }

    private static ServiceProvider BuildEndpointProvider(
        FrontComposerUserTokenStore store,
        RecordingAuthenticationService auth,
        string userClaimType = "sub") {
        ServiceCollection services = new();
        services.AddSingleton(store);
        services.AddSingleton<IAuthenticationService>(auth);
        services.AddOptions<FrontComposerAuthenticationOptions>()
            .Configure(options => {
                options.OpenIdConnect.Enabled = true;
                options.OpenIdConnect.Authority = new Uri("https://identity.test/realms/demo");
                options.OpenIdConnect.ClientId = "frontcomposer";
                options.OpenIdConnect.ClientSecret = "secret";
                options.TenantClaimTypes.Add("tenant_id");
                options.UserClaimTypes.Add(userClaimType);
            });
        return services.BuildServiceProvider();
    }

    private static RouteEndpoint BuildEndpoint(IServiceProvider provider, string path) {
        TestEndpointRouteBuilder endpoints = new(provider);
        _ = endpoints.MapHexalithFrontComposerAuthenticationEndpoints();
        return endpoints.DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint => string.Equals(endpoint.RoutePattern.RawText, path, StringComparison.Ordinal));
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);
    }

    private sealed class TestHostEnvironment : IHostEnvironment {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "FrontComposer.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class RecordingAuthenticationService : IAuthenticationService {
        public int SignOutCount { get; private set; }
        public bool ThrowOnSignOut { get; init; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignInAsync(
            HttpContext context,
            string? scheme,
            System.Security.Claims.ClaimsPrincipal principal,
            AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) {
            SignOutCount++;
            if (ThrowOnSignOut) {
                throw new InvalidOperationException("sign-out failed");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class TestEndpointRouteBuilder(IServiceProvider serviceProvider) : IEndpointRouteBuilder {
        public IApplicationBuilder CreateApplicationBuilder() => new ApplicationBuilder(ServiceProvider);

        public ICollection<EndpointDataSource> DataSources { get; } = [];

        public IServiceProvider ServiceProvider { get; } = serviceProvider;
    }
}
