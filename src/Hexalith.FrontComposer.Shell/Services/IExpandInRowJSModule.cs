using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Scoped service caching the <c>fc-expandinrow.js</c> module reference (Story 2-2 Decision D25).
/// Prevents per-component module re-import and guards against prerender-time JSRuntime unavailability.
/// </summary>
public interface IExpandInRowJSModule {
    /// <summary>
    /// Initializes the expand-in-row scroll stabilization for <paramref name="element"/>.
    /// Safe to call during prerender — the module import is skipped when JSInterop is unavailable.
    /// </summary>
    Task InitializeAsync(ElementReference element);
}

/// <inheritdoc/>
public sealed class ExpandInRowJSModule : IExpandInRowJSModule, IAsyncDisposable {
    private const string ModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-expandinrow.js";

    private readonly IJSRuntime _js;
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    public ExpandInRowJSModule(IJSRuntime js) {
        _js = js ?? throw new ArgumentNullException(nameof(js));
        _moduleTask = new Lazy<Task<IJSObjectReference>>(() => _js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask());
    }

    /// <inheritdoc/>
    public async Task InitializeAsync(ElementReference element) {
        try {
            IJSObjectReference module = await _moduleTask.Value.ConfigureAwait(false);
            await module.InvokeVoidAsync("initializeExpandInRow", element).ConfigureAwait(false);
        }
        catch (InvalidOperationException) {
            // Prerender: JSInterop not yet available. Ignored per Decision D25.
        }
        catch (JSDisconnectedException) {
            // Circuit disconnected during init — module is lost with the circuit; benign.
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() {
        if (_moduleTask.IsValueCreated) {
            try {
                IJSObjectReference module = await _moduleTask.Value.ConfigureAwait(false);
                await module.DisposeAsync().ConfigureAwait(false);
            }
            catch {
                // Disposal is best-effort; circuit teardown may have already torn the JS host down.
            }
        }
    }
}
