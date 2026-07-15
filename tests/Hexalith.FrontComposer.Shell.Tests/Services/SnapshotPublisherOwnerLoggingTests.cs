using Hexalith.FrontComposer.Shell.State.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;
using Hexalith.FrontComposer.Shell.Tests.Services.Auth;

using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

/// <summary>Locks the distinct subscriber-fault logging contracts retained by the two snapshot owners.</summary>
public sealed class SnapshotPublisherOwnerLoggingTests {
    [Fact]
    public void ProjectionConnection_ThrowingSubscriber_RedactsExceptionAndContinuesFanOut() {
        CapturingLogger<ProjectionConnectionStateService> logger = new();
        ProjectionConnectionStateService sut = new(TimeProvider.System, logger);
        bool healthyCalled = false;
        using IDisposable _ = sut.Subscribe(
            static _ => throw new InvalidOperationException("secret-user@corp"),
            replay: false);
        using IDisposable __ = sut.Subscribe(_ => healthyCalled = true, replay: false);

        sut.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Reconnecting, "Timeout"));

        healthyCalled.ShouldBeTrue();
        CapturedLogEntry warning = logger.Entries.Single(entry => entry.Level == LogLevel.Warning);
        warning.Message.ShouldContain("Projection connection state subscriber threw");
        warning.Message.ShouldContain(nameof(InvalidOperationException));
        warning.Message.ShouldNotContain("secret-user@corp");
        warning.Exception.ShouldBeNull();
    }

    [Fact]
    public void Reconciliation_ThrowingSubscriber_RedactsExceptionAndContinuesFanOut() {
        CapturingLogger<ReconnectionReconciliationStateService> logger = new();
        ReconnectionReconciliationStateService sut = new(TimeProvider.System, logger);
        bool healthyCalled = false;
        using IDisposable _ = sut.Subscribe(
            static _ => throw new InvalidOperationException("reconciliation failure"),
            replay: false);
        using IDisposable __ = sut.Subscribe(_ => healthyCalled = true, replay: false);

        sut.Start(epoch: 1);

        healthyCalled.ShouldBeTrue();
        CapturedLogEntry warning = logger.Entries.Single(entry => entry.Level == LogLevel.Warning);
        warning.Message.ShouldContain("Reconnection reconciliation state subscriber threw");
        warning.Message.ShouldContain(nameof(InvalidOperationException));
        warning.Message.ShouldNotContain("reconciliation failure");
        warning.Exception.ShouldBeNull();
    }
}
