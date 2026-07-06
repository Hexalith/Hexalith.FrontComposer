using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

using Microsoft.AspNetCore.SignalR.Client;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

public sealed class SignalRProjectionHubConnectionFactoryTests
{
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
