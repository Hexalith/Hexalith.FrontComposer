using System.Collections.Immutable;

using Fluxor;

namespace Hexalith.FrontComposer.Shell.State.Navigation;

/// <summary>
/// Fluxor feature registration for <see cref="FrontComposerNavigationState"/> (Story 3-2 D3).
/// Default tier is <see cref="ViewportTier.Desktop"/> per UX spec §170 ("new users start expanded").
/// </summary>
public sealed class FrontComposerNavigationFeature : Feature<FrontComposerNavigationState>
{
    /// <inheritdoc/>
    public override string GetName() => "FrontComposerNavigation";

    /// <inheritdoc/>
    protected override FrontComposerNavigationState GetInitialState()
        => new(
            SidebarCollapsed: false,
            CollapsedGroups: ImmutableDictionary<string, bool>.Empty.WithComparers(StringComparer.Ordinal),
            CurrentViewport: ViewportTier.Desktop);
}
