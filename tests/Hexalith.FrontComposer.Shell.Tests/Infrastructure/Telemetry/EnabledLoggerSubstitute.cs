using Microsoft.Extensions.Logging;

using NSubstitute;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;

/// <summary>
/// Creates general-purpose logger substitutes that expose generated logger calls at every level.
/// </summary>
internal static class EnabledLoggerSubstitute
{
    /// <summary>
    /// Creates an <see cref="ILogger{TCategoryName}"/> whose <see cref="ILogger.IsEnabled"/> result is always true.
    /// </summary>
    /// <typeparam name="TCategoryName">The logger category type.</typeparam>
    /// <returns>An all-level-enabled logger substitute.</returns>
    public static ILogger<TCategoryName> Create<TCategoryName>()
    {
        ILogger<TCategoryName> logger = Substitute.For<ILogger<TCategoryName>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        return logger;
    }
}
