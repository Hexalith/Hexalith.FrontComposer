using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;

/// <summary>
/// Verifies the all-level-enabled logger substitute factory.
/// </summary>
public sealed class EnabledLoggerSubstituteTests
{
    /// <summary>
    /// Verifies that current and future log levels are enabled.
    /// </summary>
    [Fact]
    public void Create_EnablesEveryCurrentAndFutureLogLevel()
    {
        ILogger<EnabledLoggerSubstituteTests> logger = EnabledLoggerSubstitute.Create<EnabledLoggerSubstituteTests>();
        LogLevel[] levels = [.. Enum.GetValues<LogLevel>(), (LogLevel)int.MaxValue];

        levels.ShouldAllBe(level => logger.IsEnabled(level));
    }
}
