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
        // Behavioural assertion: the second visible log MUST be the Connected resolution and
        // MUST carry the count of suppressed Reconnecting events from the prior window.
        string connectedLog = logger.Messages.Last(m => m.Contains("Status=Connected", StringComparison.Ordinal));
        connectedLog.ShouldContain("SuppressedCount=1");
    }

    [Fact]
    public void ReconnectingLogs_AfterWindowExpiry_EmitFreshBucketAndCarryPriorSuppression() {
        // F38 — advance FakeTimeProvider past the 30-second window so the next Reconnecting
        // arrives in a fresh bucket and emits a visible log carrying the prior window's count.
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

        time.Advance(TimeSpan.FromSeconds(31));

        sut.Apply(new ProjectionConnectionTransition(
            ProjectionConnectionStatus.Reconnecting,
            FailureCategory: "TimeoutException",
            ReconnectAttempt: 3));

        // Two visible Reconnecting logs total: the first when the bucket opened, and the third
        // when the window expired and a new bucket opened. The third visible log MUST carry
        // the prior bucket's SuppressedCount=1 (the second Reconnecting was rate-limited).
        List<string> reconnectingLogs = [.. logger.Messages.Where(m => m.Contains("Status=Reconnecting", StringComparison.Ordinal))];
        reconnectingLogs.Count.ShouldBe(2);
        reconnectingLogs[1].ShouldContain("SuppressedCount=1");
    }

    [Fact]
    public async Task DisposeAsync_FlushesPendingSuppressionCount() {
        // F07 — circuit teardown without an intervening Connected/Disconnected transition must
        // not silently swallow a window of suppressed counts. DisposeAsync emits the residual.
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
        sut.Apply(new ProjectionConnectionTransition(
            ProjectionConnectionStatus.Reconnecting,
            FailureCategory: "TimeoutException",
            ReconnectAttempt: 3));

        await sut.DisposeAsync();

        string finalLog = logger.Messages.Last();
        // Two suppressed transitions accumulated (attempt 2 and 3); flush emits SuppressedCount=2.
        finalLog.ShouldContain("SuppressedCount=2");
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
