using Microsoft.Extensions.Logging;

namespace Counter.Web;

/// <summary>
/// Provides source-generated logging for the sample-only fake authentication path.
/// </summary>
internal static partial class CounterFakeAuthLogging {
    /// <summary>
    /// Logs that the Counter sample is running with fake authentication enabled.
    /// </summary>
    /// <param name="logger">The logger that records the critical warning.</param>
    [LoggerMessage(
        EventId = 1,
        EventName = "FakeAuthenticationEnabled",
        Level = LogLevel.Critical,
        Message = "Counter sample is running with FAKE authentication (Hexalith:FrontComposer:FakeAuth:Enabled=true). All requests share a single shared identity. Do not deploy with this flag set.")]
    internal static partial void FakeAuthenticationEnabled(ILogger logger);
}
