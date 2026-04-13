namespace Hexalith.FrontComposer.Shell.State.Theme;

using Fluxor;

/// <summary>
/// Fluxor feature registration for <see cref="FrontComposerThemeState"/>.
/// </summary>
public class FrontComposerThemeFeature : Feature<FrontComposerThemeState>
{
    /// <inheritdoc/>
    public override string GetName() => "FrontComposerTheme";

    /// <inheritdoc/>
    protected override FrontComposerThemeState GetInitialState()
        => new(ThemeValue.Light);
}
