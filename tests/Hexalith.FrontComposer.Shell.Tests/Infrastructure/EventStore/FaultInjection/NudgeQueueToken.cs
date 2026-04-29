namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore.FaultInjection;

/// <summary>
/// Opaque handle returned by <see cref="FaultInjectingProjectionHubConnection.QueueNudge"/> and
/// <see cref="FaultInjectingProjectionHubConnection.DelayNextNudge"/>. Use it with
/// <c>ReleaseAsync</c>, <c>ReleaseInOrderAsync</c>, or <c>Discard</c> to deterministically flush
/// the queued publication. Tokens belong to the harness instance that created them; using a
/// token with a different instance throws <see cref="InvalidOperationException"/>.
/// </summary>
internal readonly record struct NudgeQueueToken {
    internal NudgeQueueToken(int instanceId, int sequence) {
        InstanceId = instanceId;
        Sequence = sequence;
    }

    internal int InstanceId { get; }

    internal int Sequence { get; }

    public override string ToString() => $"nudge#{Sequence}";
}
