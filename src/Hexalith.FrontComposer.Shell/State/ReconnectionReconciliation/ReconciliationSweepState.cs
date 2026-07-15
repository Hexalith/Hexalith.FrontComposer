using System.Collections.Immutable;

namespace Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

/// <summary>Lane-level sweep markers for a single reconnect epoch.</summary>
public sealed record ReconciliationSweepState {
    public ImmutableDictionary<string, ReconciliationSweepMarker> MarkersByViewKey { get; init; }
        = ImmutableDictionary<string, ReconciliationSweepMarker>.Empty;
}
