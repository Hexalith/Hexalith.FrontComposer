namespace Hexalith.FrontComposer.Shell.State.ExpandedRow;

/// <summary>
/// Story 4-5 D2 / D3 — single entry inside <see cref="ExpandedRowState.ExpandedByViewKey"/>
/// describing the currently-expanded row for one view-key. Single-entry-per-viewKey invariant
/// is enforced at the reducer level (D4); the view never holds more than one expansion per
/// view-key.
/// </summary>
/// <remarks>
/// <para>
/// <b>D5 boxing seam.</b> <see cref="ItemKey"/> is typed <see cref="object"/> because Fluxor
/// feature states cannot be open-generic. Value-type keys (e.g. <see cref="System.Guid"/>,
/// <see cref="int"/>) box on the dispatch path; the v1 boxing surface is documented under
/// Epic 9-4 AOT analyzer scope.
/// </para>
/// <para>
/// <b>D2 ephemeral lifecycle.</b> <see cref="ExpandedAt"/> is diagnostic-only — it is NOT used
/// for an auto-collapse timeout in v1 (no scheduled effect prunes the dictionary). The field
/// surfaces in test assertions and future Epic 5 reconciliation effects that may want to
/// inspect "how long has this been expanded?" telemetry.
/// </para>
/// </remarks>
public readonly record struct ExpandedRowEntry(object ItemKey, DateTimeOffset ExpandedAt);
