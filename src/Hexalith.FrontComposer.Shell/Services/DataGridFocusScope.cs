using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Story 4-3 T6.1 / D10 / AC1 — JS-interop guard used by the <c>/</c> shortcut handler to
/// scope-gate its activation. Returns <see langword="true"/> when <c>document.activeElement</c>
/// is inside a <c>[data-fc-datagrid]</c> container and <see langword="false"/> otherwise; the
/// shortcut is then transparent in non-DataGrid contexts.
/// </summary>
public sealed class DataGridFocusScope : IAsyncDisposable {
    private const string ModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-keyboard.js";

    private readonly IJSRuntime _jsRuntime;
    private readonly object _importGate = new();
    private Task<IJSObjectReference>? _moduleTask;

    /// <summary>Initializes a new instance of the <see cref="DataGridFocusScope"/> class.</summary>
    /// <param name="jsRuntime">The runtime used to invoke the <c>fc-keyboard.js</c> module.</param>
    public DataGridFocusScope(IJSRuntime jsRuntime) {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        _jsRuntime = jsRuntime;
    }

    /// <summary>True when focus is inside any DataGrid container.</summary>
    public async ValueTask<bool> IsFocusWithinDataGridAsync(CancellationToken cancellationToken = default) {
        IJSObjectReference? module = await GetModuleAsync(cancellationToken).ConfigureAwait(false);
        if (module is null) {
            return false;
        }

        try {
            return await module
                .InvokeAsync<bool>("isFocusWithinDataGrid", cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JSDisconnectedException) {
            return false;
        }
        catch (JSException) {
            return false;
        }
        catch (OperationCanceledException) {
            return false;
        }
    }

    /// <summary>Returns the <c>viewKey</c> of the enclosing DataGrid container, or <see langword="null"/>.</summary>
    public async ValueTask<string?> GetActiveViewKeyAsync(CancellationToken cancellationToken = default) {
        IJSObjectReference? module = await GetModuleAsync(cancellationToken).ConfigureAwait(false);
        if (module is null) {
            return null;
        }

        try {
            return await module
                .InvokeAsync<string?>("activeDataGridViewKey", cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JSDisconnectedException) {
            return null;
        }
        catch (JSException) {
            return null;
        }
        catch (OperationCanceledException) {
            return null;
        }
    }

    /// <summary>Focuses the first column filter input inside the matching DataGrid container.</summary>
    public async ValueTask<bool> FocusFirstColumnFilterAsync(string viewKey, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(viewKey)) {
            return false;
        }

        IJSObjectReference? module = await GetModuleAsync(cancellationToken).ConfigureAwait(false);
        if (module is null) {
            return false;
        }

        try {
            return await module
                .InvokeAsync<bool>("focusFirstColumnFilter", cancellationToken, viewKey)
                .ConfigureAwait(false);
        }
        catch (JSDisconnectedException) {
            return false;
        }
        catch (JSException) {
            return false;
        }
        catch (OperationCanceledException) {
            return false;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        Task<IJSObjectReference>? snapshot;
        lock (_importGate) {
            snapshot = _moduleTask;
            _moduleTask = null;
        }

        if (snapshot is null) {
            return;
        }

        try {
            IJSObjectReference module = await snapshot.ConfigureAwait(false);
            await module.DisposeAsync().ConfigureAwait(false);
        }
        catch (InvalidOperationException) {
            // JS interop unavailable during prerender; nothing to dispose.
        }
        catch (JSDisconnectedException) {
            // Circuit already torn down.
        }
        catch (JSException) {
            // Best-effort disposal.
        }
        catch (OperationCanceledException) {
            // Best-effort disposal.
        }
    }

    private async ValueTask<IJSObjectReference?> GetModuleAsync(CancellationToken cancellationToken) {
        Task<IJSObjectReference> importTask = GetOrStartImport();

        try {
            return await importTask.ConfigureAwait(false);
        }
        catch (InvalidOperationException) {
            ClearFaultedImport(importTask);
            return null;
        }
        catch (JSDisconnectedException) {
            ClearFaultedImport(importTask);
            return null;
        }
        catch (JSException) {
            ClearFaultedImport(importTask);
            return null;
        }
        catch (OperationCanceledException) {
            ClearFaultedImport(importTask);
            return null;
        }
    }

    private Task<IJSObjectReference> GetOrStartImport() {
        lock (_importGate) {
            _moduleTask ??= _jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
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
