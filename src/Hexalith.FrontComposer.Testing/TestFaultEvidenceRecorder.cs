using System.Collections.Concurrent;

namespace Hexalith.FrontComposer.Testing;

/// <summary>Records deterministic reconnection evidence without claiming to alter fake behavior.</summary>
public sealed class TestFaultEvidenceRecorder {
    private readonly ConcurrentQueue<FaultEvidence> _evidence = new();
    private readonly FrontComposerTestOptions _options;

    internal TestFaultEvidenceRecorder(FrontComposerTestOptions options) => _options = options;

    /// <summary>Gets captured fault-scenario evidence.</summary>
    public IReadOnlyList<FaultEvidence> Evidence => [.. _evidence];

    /// <summary>Records a drop scenario.</summary>
    public FaultEvidence RecordDrop(string correlationId) => Capture("drop", correlationId);

    /// <summary>Records a delay scenario.</summary>
    public FaultEvidence RecordDelay(string correlationId) => Capture("delay", correlationId);

    /// <summary>Records a partial-delivery scenario.</summary>
    public FaultEvidence RecordPartialDelivery(string correlationId) => Capture("partial-delivery", correlationId);

    /// <summary>Records a reorder scenario.</summary>
    public FaultEvidence RecordReorder(string correlationId) => Capture("reorder", correlationId);

    /// <summary>Records a reconnect-nudge scenario.</summary>
    public FaultEvidence RecordReconnectNudge(string correlationId) => Capture("reconnect-nudge", correlationId);

    /// <summary>Clears captured evidence.</summary>
    public void Reset() {
        while (_evidence.TryDequeue(out _)) {
        }
    }

    private FaultEvidence Capture(string mode, string correlationId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        FaultEvidence evidence = new(mode, "<tenant>", "<user>",
            RedactedEvidenceFormatter.FormatText(correlationId, _options), _options.TimeProvider.GetUtcNow());
        _evidence.Enqueue(evidence);
        while (_evidence.Count > _options.MaxEvidenceRecords && _evidence.TryDequeue(out _)) {
        }

        return evidence;
    }
}
