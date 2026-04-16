using System.Collections.Immutable;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Fluxor feature registration for <see cref="DataGridNavigationState"/> (Story 2-2 ADR-015).
/// </summary>
public sealed class DataGridNavigationFeature : Feature<DataGridNavigationState> {
    /// <inheritdoc/>
    public override string GetName() => "Hexalith.FrontComposer.Shell.State.DataGridNavigationState";

    /// <inheritdoc/>
    protected override DataGridNavigationState GetInitialState()
        => new(ImmutableDictionary<string, GridViewSnapshot>.Empty);
}
