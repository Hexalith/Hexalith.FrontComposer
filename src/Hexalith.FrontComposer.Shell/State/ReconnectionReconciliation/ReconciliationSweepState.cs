using System.Collections.Immutable;

using Fluxor;

namespace Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

/// <summary>Lane-level sweep markers for a single reconnect epoch.</summary>
public sealed record ReconciliationSweepState {
    public ImmutableDictionary<string, ReconciliationSweepMarker> MarkersByViewKey { get; init; }
        = ImmutableDictionary<string, ReconciliationSweepMarker>.Empty;
}

/// <summary>Transient marker for a changed visible lane.</summary>
public sealed record ReconciliationSweepMarker(long Epoch, DateTimeOffset ExpiresAt);

public sealed record MarkReconciliationSweepAction(long Epoch, IReadOnlyList<string> ViewKeys, DateTimeOffset ExpiresAt);

public sealed record ClearExpiredReconciliationSweepsAction(DateTimeOffset Now);

public sealed class ReconciliationSweepFeature : Feature<ReconciliationSweepState> {
    public override string GetName() => typeof(ReconciliationSweepState).FullName!;

    protected override ReconciliationSweepState GetInitialState() => new();
}

public static class ReconciliationSweepReducers {
    [ReducerMethod]
    public static ReconciliationSweepState ReduceMark(ReconciliationSweepState state, MarkReconciliationSweepAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        ImmutableDictionary<string, ReconciliationSweepMarker> next = state.MarkersByViewKey;
        foreach (string viewKey in action.ViewKeys.Distinct(StringComparer.Ordinal)) {
            if (string.IsNullOrWhiteSpace(viewKey)) {
                continue;
            }

            next = next.SetItem(viewKey, new ReconciliationSweepMarker(action.Epoch, action.ExpiresAt));
        }

        return ReferenceEquals(next, state.MarkersByViewKey) ? state : state with { MarkersByViewKey = next };
    }

    [ReducerMethod]
    public static ReconciliationSweepState ReduceClearExpired(ReconciliationSweepState state, ClearExpiredReconciliationSweepsAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        ImmutableDictionary<string, ReconciliationSweepMarker> next = state.MarkersByViewKey;
        foreach (KeyValuePair<string, ReconciliationSweepMarker> marker in state.MarkersByViewKey) {
            if (marker.Value.ExpiresAt <= action.Now) {
                next = next.Remove(marker.Key);
            }
        }

        return ReferenceEquals(next, state.MarkersByViewKey) ? state : state with { MarkersByViewKey = next };
    }
}
