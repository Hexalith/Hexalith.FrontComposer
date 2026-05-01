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
        string? token = await eventStore.AccessTokenProvider!(TestContext.Current.CancellationToken);
        token.ShouldBe("token");
    }
}
