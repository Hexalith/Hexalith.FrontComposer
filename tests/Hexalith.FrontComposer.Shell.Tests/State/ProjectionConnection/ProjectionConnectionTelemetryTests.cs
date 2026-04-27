using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.State.ProjectionConnection;

[Trait("Category", "Governance")]
public sealed class ProjectionConnectionTelemetryTests {
    [Fact]
    public void ReconnectingLogs_AreRateLimited_AndNextVisibleLogCarriesSuppressionCount() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 04, 26, 12, 0, 0, TimeSpan.Zero));
        CapturingLogger<ProjectionConnectionStateService> logger = new();
        ProjectionConnectionStateService sut = new(time, logger);

        sut.Apply(new ProjectionConnectionTransition(
            ProjectionConnectionStatus.Reconnecting,
            FailureCategory: "TimeoutException",
            ReconnectAttempt: 1));
        sut.Apply(new ProjectionConnectionTransition(
            ProjectionConnectionStatus.Reconnecting,
            FailureCategory: "TimeoutException",
            ReconnectAttempt: 2));
        sut.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Connected));

        logger.Messages.Count(message => message.Contains("projection connection state changed", StringComparison.OrdinalIgnoreCase))
            .ShouldBe(2);
        logger.Messages.Last().ShouldContain("SuppressedCount=1");
    }

    private sealed class CapturingLogger<T> : ILogger<T> {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
    }
}
