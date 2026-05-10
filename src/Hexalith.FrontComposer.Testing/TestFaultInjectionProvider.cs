using System.Collections.Concurrent;

namespace Hexalith.FrontComposer.Testing;

/// <summary>
/// Deterministic fault provider for reconnection tests that should not open a live SignalR hub.
/// </summary>
public sealed class TestFaultInjectionProvider
{
    private readonly ConcurrentQueue<FaultInjectionEvidence> _evidence = new();
    private readonly FrontComposerTestOptions _options;

    internal TestFaultInjectionProvider(FrontComposerTestOptions options) => _options = options;

    /// <summary>Gets captured fault simulation evidence.</summary>
    public IReadOnlyList<FaultInjectionEvidence> Evidence => [.. _evidence];

    /// <summary>Records a deterministic drop fault.</summary>
    public FaultInjectionEvidence Drop(string correlationId) => Capture("drop", correlationId);

    /// <summary>Records a deterministic delay fault.</summary>
    public FaultInjectionEvidence Delay(string correlationId) => Capture("delay", correlationId);

    /// <summary>Records a deterministic partial-delivery fault.</summary>
    public FaultInjectionEvidence PartialDelivery(string correlationId) => Capture("partial-delivery", correlationId);

    /// <summary>Records a deterministic reorder fault.</summary>
    public FaultInjectionEvidence Reorder(string correlationId) => Capture("reorder", correlationId);

    /// <summary>Records a deterministic reconnect nudge.</summary>
    public FaultInjectionEvidence ReconnectNudge(string correlationId) => Capture("reconnect-nudge", correlationId);

    /// <summary>Clears captured fault evidence.</summary>
    public void Reset()
    {
        while (_evidence.TryDequeue(out _))
        {
        }
    }

    private FaultInjectionEvidence Capture(string mode, string correlationId)
    {
        FaultInjectionEvidence evidence = new(
            mode,
            _options.TestTenantId,
            _options.TestUserId,
            correlationId,
            _options.TimeProvider.GetUtcNow());
        _evidence.Enqueue(evidence);
        while (_evidence.Count > _options.MaxEvidenceRecords && _evidence.TryDequeue(out _))
        {
        }

        return evidence;
    }
}
