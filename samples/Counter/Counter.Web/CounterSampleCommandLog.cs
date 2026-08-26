using Microsoft.Extensions.Logging;

namespace Counter.Web;

internal static partial class CounterSampleCommandLog
{
    [LoggerMessage(
        EventId = 9801,
        Level = LogLevel.Information,
        Message = "Epic 9 exact-target sample command reached terminal state. CommandType={CommandType} Result={Result}.")]
    public static partial void ExactTargetCommandConfirmed(
        ILogger logger,
        string commandType,
        string result);
}
