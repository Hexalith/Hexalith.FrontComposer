
using Fluxor;

namespace Hexalith.FrontComposer.Shell.State.Theme;
/// <summary>
/// Fluxor feature registration for <see cref="FrontComposerThemeState"/> (Story 3-6 D19).
/// </summary>
public class FrontComposerThemeFeature : Feature<FrontComposerThemeState> {
    /// <inheritdoc/>
    public override string GetName() => "FrontComposerTheme";

    /// <inheritdoc/>
    protected override FrontComposerThemeState GetInitialState()
        => new(ThemeValue.Light, ThemeHydrationState.Idle);
}
