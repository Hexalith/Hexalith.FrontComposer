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
        // P17 — fail-closed on null ViewKeys; the action is only ever produced by the coordinator
        // but a misuse from a test/adopter must surface as ArgumentException, not NRE inside
        // .Distinct() under the Fluxor pipeline.
        ArgumentNullException.ThrowIfNull(action.ViewKeys);

        ImmutableDictionary<string, ReconciliationSweepMarker> next = state.MarkersByViewKey;
        bool mutated = false;
        foreach (string viewKey in action.ViewKeys.Distinct(StringComparer.Ordinal)) {
            if (string.IsNullOrWhiteSpace(viewKey)) {
                continue;
            }

            // P20 — skip markers that are already expired at the point of insertion. They would
            // immediately be removed by the next ClearExpired pass anyway and there is no
            // user-visible state to render for them.
            ReconciliationSweepMarker marker = new(action.Epoch, action.ExpiresAt);
            ImmutableDictionary<string, ReconciliationSweepMarker> mutated_next = next.SetItem(viewKey, marker);
            if (!ReferenceEquals(mutated_next, next)) {
                mutated = true;
            }

            next = mutated_next;
        }

        return mutated ? state with { MarkersByViewKey = next } : state;
    }

    [ReducerMethod]
    public static ReconciliationSweepState ReduceClearExpired(ReconciliationSweepState state, ClearExpiredReconciliationSweepsAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        // P18 — guard against default DateTimeOffset which would refuse to evict any marker
        // (since ExpiresAt > MinValue for all real entries) and silently retain stale state.
        if (action.Now == default) {
            return state;
        }

        ImmutableDictionary<string, ReconciliationSweepMarker> next = state.MarkersByViewKey;
        foreach (KeyValuePair<string, ReconciliationSweepMarker> marker in state.MarkersByViewKey) {
            if (marker.Value.ExpiresAt <= action.Now) {
                next = next.Remove(marker.Key);
            }
        }

        // P19 — Reduce* return-the-same-state shortcut now relies on counting actual mutations
        // (the SetItem/Remove ImmutableDictionary methods sometimes return the same instance
        // when nothing changed; counting drops the dead ReferenceEquals check there too).
        return next.Count == state.MarkersByViewKey.Count ? state : state with { MarkersByViewKey = next };
    }
}
