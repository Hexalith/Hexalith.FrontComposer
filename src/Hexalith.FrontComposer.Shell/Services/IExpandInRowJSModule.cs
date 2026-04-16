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
    private readonly object _importGate = new();
    private Task<IJSObjectReference>? _moduleTask;

    public ExpandInRowJSModule(IJSRuntime js) {
        _js = js ?? throw new ArgumentNullException(nameof(js));
    }

    /// <inheritdoc/>
    public async Task InitializeAsync(ElementReference element) {
        Task<IJSObjectReference> importTask = GetOrStartImport();

        IJSObjectReference module;
        try {
            module = await importTask.ConfigureAwait(false);
        }
        catch (InvalidOperationException) {
            // Prerender: JSInterop not yet available. Ignored per Decision D25.
            ClearFaultedImport(importTask);
            return;
        }
        catch (JSDisconnectedException) {
            // Circuit disconnected during init — module is lost with the circuit; benign.
            ClearFaultedImport(importTask);
            return;
        }
        catch (JSException) {
            // Module import path failed (404, module SyntaxError). Clear the cache so a
            // subsequent call can retry instead of permanently disabling expand-in-row.
            ClearFaultedImport(importTask);
            return;
        }
        catch (OperationCanceledException) {
            ClearFaultedImport(importTask);
            return;
        }

        try {
            await module.InvokeVoidAsync("initializeExpandInRow", element).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) {
            // Circuit tore down between import and invoke; benign.
        }
        catch (JSException) {
            // JS-side initialization failed (e.g. detached element). Story 2-2 AC3 treats
            // scroll stabilization as best-effort; swallow to avoid faulting the host component.
        }
        catch (OperationCanceledException) {
            // Ignored.
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() {
        Task<IJSObjectReference>? snapshot;
        lock (_importGate) {
            snapshot = _moduleTask;
            _moduleTask = null;
        }

        if (snapshot is null) {
            return;
        }

        IJSObjectReference module;
        try {
            module = await snapshot.ConfigureAwait(false);
        }
        catch (JSDisconnectedException) {
            return;
        }
        catch (JSException) {
            return;
        }
        catch (OperationCanceledException) {
            return;
        }

        try {
            await module.DisposeAsync().ConfigureAwait(false);
        }
        catch (JSDisconnectedException) {
            // Circuit tore down before module dispose — benign.
        }
        catch (JSException) {
            // JS-side dispose failed; best-effort only.
        }
    }

    private Task<IJSObjectReference> GetOrStartImport() {
        lock (_importGate) {
            _moduleTask ??= _js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
            return _moduleTask;
        }
    }

    private void ClearFaultedImport(Task<IJSObjectReference> faulted) {
        lock (_importGate) {
            if (ReferenceEquals(_moduleTask, faulted)) {
                _moduleTask = null;
            }
        }
    }
}
