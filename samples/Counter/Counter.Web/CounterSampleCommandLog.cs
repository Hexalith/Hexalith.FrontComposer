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

    /// <summary>Records the native time specimen payload at the command service boundary.</summary>
    /// <param name="logger">The sample command service logger.</param>
    /// <param name="requiredTime">The invariant round-trip representation of the required time.</param>
    /// <param name="optionalTime">The invariant round-trip representation of the optional time, or null.</param>
    [LoggerMessage(
        EventId = 9802,
        Level = LogLevel.Information,
        SkipEnabledCheck = true,
        Message = "Native time specimen dispatched. RequiredTime={RequiredTime} OptionalTime={OptionalTime}.")]
    public static partial void NativeTimeSpecimenDispatched(
        ILogger logger,
        string requiredTime,
        string optionalTime);
}
