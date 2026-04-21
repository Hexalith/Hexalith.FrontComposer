using System.Collections.Immutable;

using Fluxor;

namespace Hexalith.FrontComposer.Shell.State.CommandPalette;

/// <summary>
/// Fluxor feature registration for <see cref="FrontComposerCommandPaletteState"/> (Story 3-4 D6).
/// Initial state: closed, empty query, empty results + recent buffer, selection 0, idle load.
/// </summary>
public class FrontComposerCommandPaletteFeature : Feature<FrontComposerCommandPaletteState> {
    /// <inheritdoc />
    public override string GetName() => "FrontComposerCommandPalette";

    /// <inheritdoc />
    protected override FrontComposerCommandPaletteState GetInitialState()
        => new(
            IsOpen: false,
            Query: string.Empty,
            Results: ImmutableArray<PaletteResult>.Empty,
            RecentRouteUrls: ImmutableArray<string>.Empty,
            SelectedIndex: 0,
            LoadState: PaletteLoadState.Idle);
}
