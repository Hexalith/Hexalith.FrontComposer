using Fluxor;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Fluxor feature registration for <see cref="LoadedPageState"/> (Story 4-4 D2 / D3 / D10 / D16).
/// Initial state is entirely empty; entries materialise on first
/// <see cref="Contracts.Rendering.LoadPageAction"/> dispatch per view key.
/// </summary>
public sealed class LoadedPageFeature : Feature<LoadedPageState> {
    /// <inheritdoc/>
    public override string GetName() => typeof(LoadedPageState).FullName!;

    /// <inheritdoc/>
    protected override LoadedPageState GetInitialState() => new();
}
