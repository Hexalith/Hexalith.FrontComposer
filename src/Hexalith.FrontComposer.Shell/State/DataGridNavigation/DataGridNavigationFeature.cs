using System.Collections.Immutable;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Fluxor feature registration for <see cref="DataGridNavigationState"/> (Story 2-2 ADR-015 /
/// Story 3-6 D19 / A7).
/// Seeds the state's <see cref="DataGridNavigationState.Cap"/> from
/// <see cref="FcShellOptions.DataGridNavCap"/> at first construction so reducers stay pure
/// (Group D code review W1 resolution).
/// </summary>
public sealed class DataGridNavigationFeature : Feature<DataGridNavigationState> {
    private readonly int _cap;

    public DataGridNavigationFeature() : this(null) {
    }

    public DataGridNavigationFeature(IOptions<FcShellOptions>? options) {
        int configured = options?.Value?.DataGridNavCap ?? 50;
        _cap = configured > 0 ? configured : 50;
    }

    /// <inheritdoc/>
    public override string GetName() => typeof(DataGridNavigationState).FullName!;

    /// <inheritdoc/>
    protected override DataGridNavigationState GetInitialState()
        => new(
            ViewStates: ImmutableDictionary<string, GridViewSnapshot>.Empty,
            Cap: _cap,
            HydrationState: DataGridNavigationHydrationState.Idle);
}
