using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

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
}
