using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

public sealed class SignalRProjectionHubConnectionFactoryTests
{
    [Fact]
    public async Task Create_ConfiguresProductionRetryTokenAndInitialPhase()
    {
        HubConnectionBuilder? observedBuilder = null;
        Func<Task<string?>>? observedTokenProvider = null;
        SignalRProjectionHubConnectionFactory sut = new(
            logger: null,
            (builder, tokenProvider) =>
            {
                observedBuilder = builder;
                observedTokenProvider = tokenProvider;
            });

        IProjectionHubConnection connection = sut.Create(
            new Uri("https://eventstore.test/hubs/projection-changes"),
            _ => ValueTask.FromResult<string?>("captured-token"));
        await using (connection.ConfigureAwait(false))
        {
            observedBuilder.ShouldNotBeNull();
            observedBuilder.Services.ShouldContain(descriptor =>
                descriptor.ServiceType == typeof(IRetryPolicy)
                && descriptor.ImplementationInstance is ProjectionHubRetryPolicy);
            observedTokenProvider.ShouldNotBeNull();
            (await observedTokenProvider()).ShouldBe("captured-token");

            // Assert what WithUrl actually installed, not the wrapper handed to the observer:
            // emptying the configure callback must not leave this test green.
            (await ResolveAccessTokenAsync(observedBuilder)).ShouldBe("captured-token");
            connection.Phase.ShouldBe(ProjectionHubConnectionPhase.Disconnected);
        }
    }

    [Theory]
    [InlineData(HubConnectionState.Disconnected, (int)ProjectionHubConnectionPhase.Disconnected)]
    [InlineData(HubConnectionState.Connecting, (int)ProjectionHubConnectionPhase.Connecting)]
    [InlineData(HubConnectionState.Connected, (int)ProjectionHubConnectionPhase.Connected)]
    [InlineData(HubConnectionState.Reconnecting, (int)ProjectionHubConnectionPhase.Reconnecting)]
    public void MapConnectionPhase_MapsEverySignalRPhase(
        HubConnectionState source,
        int expected)
        => ((int)SignalRProjectionHubConnectionFactory.MapConnectionPhase(source)).ShouldBe(expected);

    [Theory]
    [InlineData(true, null, "JoinGroup")]
    [InlineData(true, "   ", "JoinGroup")]
    [InlineData(false, "   ", "LeaveGroup")]
    [InlineData(true, "conversation", "JoinGroupScoped")]
    [InlineData(false, null, "LeaveGroup")]
    [InlineData(false, "conversation", "LeaveGroupScoped")]
    public void SelectGroupMethod_MapsScopedAndUnscopedAdapterCalls(
        bool join,
        string? scope,
        string expected)
        => SignalRProjectionHubConnectionFactory.SelectGroupMethod(join, scope).ShouldBe(expected);

    [Fact]
    public void ProjectionHubWireContract_UsesEventStoreHubMethodNames()
    {
        ProjectionHubWireContract.ProjectionChanged.ShouldBe("ProjectionChanged");
        ProjectionHubWireContract.ProjectionChangedDetail.ShouldBe("ProjectionChangedDetail");
        ProjectionHubWireContract.JoinGroup.ShouldBe("JoinGroup");
        ProjectionHubWireContract.JoinGroupScoped.ShouldBe("JoinGroupScoped");
        ProjectionHubWireContract.LeaveGroup.ShouldBe("LeaveGroup");
        ProjectionHubWireContract.LeaveGroupScoped.ShouldBe("LeaveGroupScoped");
    }

    [Fact]
    public void ProjectionHubRetryPolicy_NeverStopsRetrying_AndCapsDelay()
    {
        ProjectionHubRetryPolicy sut = new(_ => 0);

        foreach (long retryCount in new[] { 0L, 1L, 2L, 3L, 10L, 100L })
        {
            TimeSpan? delay = sut.NextRetryDelay(new RetryContext
            {
                PreviousRetryCount = retryCount,
                ElapsedTime = TimeSpan.FromDays(30),
                RetryReason = new IOException("transport"),
            });

            delay.ShouldNotBeNull();
            (delay.Value <= TimeSpan.FromMilliseconds(ProjectionHubRetryPolicy.MaxDelayMilliseconds)).ShouldBeTrue();
        }
    }

    [Fact]
    public void ProjectionHubRetryPolicy_AppliesJitter()
    {
        RetryContext context = new()
        {
            PreviousRetryCount = 3,
            ElapsedTime = TimeSpan.FromSeconds(10),
            RetryReason = new IOException("transport"),
        };
        TimeSpan low = new ProjectionHubRetryPolicy(_ => 0).NextRetryDelay(context)!.Value;
        TimeSpan high = new ProjectionHubRetryPolicy(maxExclusive => maxExclusive - 1).NextRetryDelay(context)!.Value;

        high.ShouldBeGreaterThan(low);
    }

    [Fact]
    public async Task Create_WithoutAccessTokenProvider_InstallsNoAccessTokenProvider()
    {
        HubConnectionBuilder? observedBuilder = null;
        SignalRProjectionHubConnectionFactory sut = new(
            logger: null,
            (builder, _) => observedBuilder = builder);

        IProjectionHubConnection connection = sut.Create(
            new Uri("https://eventstore.test/hubs/projection-changes"),
            accessTokenProvider: null);
        await using (connection.ConfigureAwait(false))
        {
            observedBuilder.ShouldNotBeNull();
            HttpConnectionOptions options = ResolveHttpConnectionOptions(observedBuilder);
            options.AccessTokenProvider.ShouldBeNull();
        }
    }

    private static async Task<string?> ResolveAccessTokenAsync(HubConnectionBuilder builder)
    {
        HttpConnectionOptions options = ResolveHttpConnectionOptions(builder);
        options.AccessTokenProvider.ShouldNotBeNull();
        return await options.AccessTokenProvider().ConfigureAwait(false);
    }

    private static HttpConnectionOptions ResolveHttpConnectionOptions(HubConnectionBuilder builder)
    {
        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<HttpConnectionOptions>>().Value;
    }
}
