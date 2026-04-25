using System.Collections.Immutable;

namespace Hexalith.FrontComposer.Shell.State.ExpandedRow;

/// <summary>
/// Story 4-5 D2 / D3 — Fluxor feature state tracking the single currently-expanded row per
/// view-key (single-expand invariant per UX-DR17 enforced at the reducer per D4). The state
/// is EPHEMERAL — no <c>Persist</c> attribute, no <c>DataGridNavigationEffects</c> participation,
/// no LocalStorage hydration on reload. Cross-session restore would be the same disorienting
/// anti-pattern as Story 4-4 D5's scroll-position cross-session clamp.
/// </summary>
/// <remarks>
/// <para>
/// <b>D22 ephemeral key contract.</b> The <see cref="ExpandedByViewKey"/> dictionary is keyed
/// by the per-component-instance ephemeral form
/// <c>{boundedContext}:{projectionTypeFqn}:{ComponentInstanceId}</c> — distinct from the
/// persisted view-key consumed by Story 3-6 / 4-3 / 4-4. The reducer treats the suffix as
/// opaque; only equality matters for dictionary access.
/// </para>
/// <para>
/// <b>Disposal contract.</b> Each generated view dispatches <see cref="Contracts.Rendering.CollapseRowAction"/>
/// from its <c>DisposeAsync</c> body so navigation away leaves no stale entry — see Story 4-4
/// D3 <c>ClearPendingPagesAction</c> precedent. The reducer is idempotent, so the dispatch is
/// unconditional (no state read during teardown).
/// </para>
/// </remarks>
public sealed record ExpandedRowState {
    /// <summary>
    /// Gets the dictionary of (ephemeral view-key) → (single expanded entry). Empty by default;
    /// entries materialise on the first <see cref="Contracts.Rendering.ExpandRowAction"/> dispatch
    /// per view-key and disappear on <see cref="Contracts.Rendering.CollapseRowAction"/> or view
    /// <c>DisposeAsync</c>.
    /// </summary>
    public ImmutableDictionary<string, ExpandedRowEntry> ExpandedByViewKey { get; init; }
        = ImmutableDictionary<string, ExpandedRowEntry>.Empty;

    /// <summary>
    /// Returns the entry for the given view-key, or <see langword="null"/> when none is present.
    /// Convenience accessor used by generated views to drive <c>_expandedItemKey</c> /
    /// <c>_expandedItem</c> on every render.
    /// </summary>
    /// <param name="viewKey">The ephemeral view-key (D22).</param>
    public ExpandedRowEntry? GetEntry(string viewKey)
        => ExpandedByViewKey.TryGetValue(viewKey, out ExpandedRowEntry entry) ? entry : null;
}
