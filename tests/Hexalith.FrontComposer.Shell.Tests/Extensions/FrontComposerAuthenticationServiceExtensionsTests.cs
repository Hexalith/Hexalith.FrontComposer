using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Auth;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

using Xunit;

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
        _ = services.AddHexalithEventStore(options => {
            options.BaseAddress = new Uri("https://eventstore.test");
        });
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
}
