using Fluxor;

namespace Hexalith.FrontComposer.Shell.State.ExpandedRow;

/// <summary>
/// Story 4-5 D2 — Fluxor feature registration for <see cref="ExpandedRowState"/>. Initial
/// state is an empty dictionary; entries materialise on the first
/// <see cref="Contracts.Rendering.ExpandRowAction"/> dispatch per view-key.
/// </summary>
public sealed class ExpandedRowFeature : Feature<ExpandedRowState> {
    /// <inheritdoc/>
    public override string GetName() => typeof(ExpandedRowState).FullName!;

    /// <inheritdoc/>
    protected override ExpandedRowState GetInitialState() => new();
}
