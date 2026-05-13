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

public sealed record MarkReconciliationSweepAction(
    long Epoch,
    IReadOnlyList<string> ViewKeys,
    DateTimeOffset ExpiresAt,
    DateTimeOffset Now);

public sealed record ClearExpiredReconciliationSweepsAction(DateTimeOffset Now);

public sealed class ReconciliationSweepFeature : Feature<ReconciliationSweepState> {
    public override string GetName() => typeof(ReconciliationSweepState).FullName!;

    protected override ReconciliationSweepState GetInitialState() => new();
}

public static class ReconciliationSweepReducers {
    private const int MaxSweepMarkers = 512;

    [ReducerMethod]
    public static ReconciliationSweepState ReduceMark(ReconciliationSweepState state, MarkReconciliationSweepAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        // P17 — fail-closed on null ViewKeys; the action is only ever produced by the coordinator
        // but a misuse from a test/adopter must surface as ArgumentException, not NRE inside
        // .Distinct() under the Fluxor pipeline.
        ArgumentNullException.ThrowIfNull(action.ViewKeys);

        // P20 (Story 11.7 code review DN-1) — the incoming action carries the dispatcher's
        // observation of the wall clock. If the markers it requests are already expired (or
        // would be by the time the next ClearExpired sweep runs), they cannot produce any
        // user-visible state and must not consume cap slots that would then crowd out fresh
        // markers from later actions.
        if (action.ExpiresAt <= action.Now) {
            return state;
        }

        ImmutableDictionary<string, ReconciliationSweepMarker> next = state.MarkersByViewKey;
        bool mutated = false;
        foreach (string viewKey in action.ViewKeys.Distinct(StringComparer.Ordinal)) {
            if (string.IsNullOrWhiteSpace(viewKey)) {
                continue;
            }

            if (!next.ContainsKey(viewKey) && next.Count >= MaxSweepMarkers) {
                // P21 (Story 11.7 code review DN-1) — when the cap is saturated, evict the
                // marker with the earliest ExpiresAt and let this newer key take its slot.
                // Order-dependent first-in-wins would let a stale burst lock out every fresh
                // lane until the legacy markers expire; an LRU-by-expiry policy keeps the
                // most-recently-needed reconciliation state in memory.
                KeyValuePair<string, ReconciliationSweepMarker> oldest = next
                    .OrderBy(static kv => kv.Value.ExpiresAt)
                    .First();
                if (oldest.Value.ExpiresAt >= action.ExpiresAt) {
                    // The incoming marker would itself be the earliest-expiring; dropping
                    // it keeps the existing set intact.
                    continue;
                }

                next = next.Remove(oldest.Key);
                mutated = true;
            }

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
