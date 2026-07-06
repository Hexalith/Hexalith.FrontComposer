using System.Security.Cryptography;

using Microsoft.AspNetCore.SignalR.Client;

namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

internal sealed class ProjectionHubRetryPolicy(Func<int, int>? jitterProvider = null) : IRetryPolicy
{
    internal const int MaxDelayMilliseconds = 30_000;

    private readonly Func<int, int> _jitterProvider = jitterProvider ?? RandomNumberGenerator.GetInt32;

    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        ArgumentNullException.ThrowIfNull(retryContext);

        int baseDelayMilliseconds = CalculateBaseDelayMilliseconds(retryContext.PreviousRetryCount);
        int jitterWindowMilliseconds = Math.Clamp(baseDelayMilliseconds / 3, 100, 1_000);
        int jitterMilliseconds = _jitterProvider(jitterWindowMilliseconds + 1);
        if (jitterMilliseconds < 0 || jitterMilliseconds > jitterWindowMilliseconds)
        {
            jitterMilliseconds = 0;
        }

        int floorMilliseconds = Math.Max(0, baseDelayMilliseconds - (jitterWindowMilliseconds / 2));
        return TimeSpan.FromMilliseconds(Math.Min(MaxDelayMilliseconds, floorMilliseconds + jitterMilliseconds));
    }

    private static int CalculateBaseDelayMilliseconds(long previousRetryCount)
    {
        long boundedRetryCount = Math.Clamp(previousRetryCount, 0, 10);
        double exponentialMilliseconds = 500 * Math.Pow(2, boundedRetryCount);
        return (int)Math.Min(MaxDelayMilliseconds, exponentialMilliseconds);
    }
}
