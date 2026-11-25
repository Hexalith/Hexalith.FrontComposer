using Hexalith.FrontComposer.Shell.State.ProjectionConnection;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

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

        logger.Entries.Count.ShouldBe(2);
        AssertTransition(logger.Entries[0], "Reconnecting", 1, "TimeoutException", 0);
        AssertTransition(logger.Entries[1], "Connected", 0, "none", 1);
    }

    [Fact]
    public void ReconnectingLogs_AfterWindowExpiry_EmitFreshBucketAndCarryPriorSuppression() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 04, 26, 12, 0, 0, TimeSpan.Zero));
        CapturingLogger<ProjectionConnectionStateService> logger = new();
        ProjectionConnectionStateService sut = new(time, logger);

        sut.Apply(new ProjectionConnectionTransition(
            ProjectionConnectionStatus.Reconnecting,
            FailureCategory: "TimeoutException",
            ReconnectAttempt: 1));
        time.Advance(TimeSpan.FromSeconds(20));
        sut.Apply(new ProjectionConnectionTransition(
            ProjectionConnectionStatus.Reconnecting,
            FailureCategory: "TimeoutException",
            ReconnectAttempt: 2));
        time.Advance(TimeSpan.FromSeconds(10));
        sut.Apply(new ProjectionConnectionTransition(
            ProjectionConnectionStatus.Reconnecting,
            FailureCategory: "TimeoutException",
            ReconnectAttempt: 3));

        logger.Entries.Count.ShouldBe(2);
        AssertTransition(logger.Entries[0], "Reconnecting", 1, "TimeoutException", 0);
        AssertTransition(logger.Entries[1], "Reconnecting", 3, "TimeoutException", 1);
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

        logger.Entries.Count.ShouldBe(2);
        AssertTransition(logger.Entries[0], "Reconnecting", 1, "TimeoutException", 0);
        AssertTransition(logger.Entries[1], "Reconnecting", 3, "TimeoutException", 2);
    }

    private static void AssertTransition(CapturedLogEntry entry, string status, int attempt, string failureCategory, int suppressedCount) {
        entry.EventId.Id.ShouldBe(5620);
        entry.EventId.Name.ShouldBe("ProjectionConnectionChanged");
        entry.Level.ShouldBe(LogLevel.Information);
        entry.State["Status"].ShouldBeOfType<string>().ShouldBe(status);
        entry.State["Attempt"].ShouldBeOfType<int>().ShouldBe(attempt);
        entry.State["FailureCategory"].ShouldBeOfType<string>().ShouldBe(failureCategory);
        entry.State["SuppressedCount"].ShouldBeOfType<int>().ShouldBe(suppressedCount);
    }
}
