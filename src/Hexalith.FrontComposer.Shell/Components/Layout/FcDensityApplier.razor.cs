using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.Density;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Headless component that mirrors <see cref="FrontComposerDensityState.EffectiveDensity"/> onto
/// the <c>&lt;body data-fc-density&gt;</c> attribute via the <c>fc-density.js</c> module
/// (Story 3-3 D10; ADR-041). Imports the module on first render and invokes <c>setDensity</c> on
/// every selection change. Structure mirrors Story 3-1's <c>FcSystemThemeWatcher</c>.
/// </summary>
public partial class FcDensityApplier : ComponentBase, IAsyncDisposable
{
    private const string ModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-density.js";

    private IJSObjectReference? _module;
    private DensityLevel? _lastApplied;
    private bool _disposed;

    /// <summary>Injected JS runtime for module import + invocation.</summary>
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// Projection over <see cref="FrontComposerDensityState"/> yielding
    /// <see cref="FrontComposerDensityState.EffectiveDensity"/>. Re-renders on change.
    /// </summary>
    [Inject] private IStateSelection<FrontComposerDensityState, DensityLevel> EffectiveSelection { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        EffectiveSelection.Select(state => state.EffectiveDensity);
        EffectiveSelection.SelectedValueChanged += OnSelectedValueChanged;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (JSException)
        {
            // Non-fatal — density styling reverts to CSS defaults when the module fails to load.
            return;
        }

        await InvokeSetDensityAsync(EffectiveSelection.Value).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        EffectiveSelection.SelectedValueChanged -= OnSelectedValueChanged;

        if (_module is not null)
        {
            try { await _module.DisposeAsync().ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            catch (JSDisconnectedException) { }
            catch (JSException) { }
        }

        GC.SuppressFinalize(this);
    }

    private void OnSelectedValueChanged(object? sender, DensityLevel value)
    {
        // Fire-and-forget — StateSelection events fire on the renderer thread; do not await JS here.
        _ = InvokeSetDensityAsync(value);
    }

    private async Task InvokeSetDensityAsync(DensityLevel level)
    {
        if (_module is null || _disposed)
        {
            return;
        }

        if (_lastApplied == level)
        {
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("setDensity", level.ToString()).ConfigureAwait(false);
            _lastApplied = level;
        }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
    }
}
