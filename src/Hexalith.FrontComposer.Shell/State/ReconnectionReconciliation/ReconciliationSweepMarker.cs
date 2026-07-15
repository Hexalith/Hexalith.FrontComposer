namespace Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

/// <summary>Transient marker for a changed visible lane.</summary>
public sealed record ReconciliationSweepMarker(long Epoch, DateTimeOffset ExpiresAt);
