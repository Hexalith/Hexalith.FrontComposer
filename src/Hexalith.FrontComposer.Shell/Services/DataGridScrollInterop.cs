using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Story 4-4 D4 / T1.4 — JS interop bridge for scroll capture + restore. The JS-side
/// <c>captureScrollThrottled</c> helper caps raw <c>@onscroll</c> dispatch at ~30 Hz via a
/// <c>setTimeout</c>+timestamp mechanism (per D4 re-revised), invoking <c>HandleScrollAsync</c>
/// via <c>DotNetObjectReference</c> to reach the reducer. The 150 ms semantic debounce lives in
/// <c>ScrollPersistenceEffect</c>.
/// </summary>
public sealed class DataGridScrollInterop : IAsyncDisposable {
    private const string ModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-datagrid.js";

    private readonly IJSRuntime _jsRuntime;
    private readonly object _importGate = new();
    private Task<IJSObjectReference>? _moduleTask;

    /// <summary>Initializes a new instance of the <see cref="DataGridScrollInterop"/> class.</summary>
    /// <param name="jsRuntime">Runtime used to import <c>fc-datagrid.js</c>.</param>
    public DataGridScrollInterop(IJSRuntime jsRuntime) {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Invokes the JS-side throttle — consumed by the generated view's <c>@onscroll</c> handler
    /// (one call per scroll event; the JS module caps dispatch at ≤ 30 Hz with guaranteed
    /// trailing-edge delivery).
    /// </summary>
    /// <param name="viewKey">Per-view key attached to the outer <c>data-fc-datagrid</c> container.</param>
    /// <param name="scrollTop">Current scroll offset.</param>
    /// <param name="dotnetRef">Reference the JS module invokes back into via <c>HandleScrollAsync</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async ValueTask CaptureScrollAsync(
        string viewKey,
        double scrollTop,
        DotNetObjectReference<object> dotnetRef,
        CancellationToken cancellationToken = default) {
        IJSObjectReference? module = await TryGetModuleAsync(cancellationToken).ConfigureAwait(false);
        if (module is null) {
            return;
        }

        try {
            await module.InvokeVoidAsync("captureScrollThrottled", cancellationToken, viewKey, scrollTop, dotnetRef).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) {
            // Circuit tearing down — ignore.
        }
        catch (JSException) {
            // Log-at-warn is Epic 5 observability; silently tolerate here.
        }
        catch (OperationCanceledException) {
            // Expected during disposal.
        }
    }

    /// <summary>
    /// Applies the hydrated <see cref="Contracts.Rendering.GridViewSnapshot.ScrollTop"/> value
    /// to the DataGrid's scroll container after the first render tick (within-session only;
    /// cross-session hydration has already clamped to 0 per Story 4-4 D5).
    /// </summary>
    /// <param name="viewKey">Per-view key attached to the outer <c>data-fc-datagrid</c> container.</param>
    /// <param name="scrollTop">Offset to apply to <c>.scrollTop</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async ValueTask ScrollToOffsetAsync(
        string viewKey,
        double scrollTop,
        CancellationToken cancellationToken = default) {
        IJSObjectReference? module = await TryGetModuleAsync(cancellationToken).ConfigureAwait(false);
        if (module is null) {
            return;
        }

        try {
            await module.InvokeVoidAsync("scrollToOffset", cancellationToken, viewKey, scrollTop).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) {
        }
        catch (JSException) {
        }
        catch (OperationCanceledException) {
        }
    }

    /// <summary>
    /// Clears any pending trailing-edge timer for <paramref name="viewKey"/> and removes the
    /// per-viewKey Map entry — called from the generated view's <c>IAsyncDisposable</c> chain to
    /// prevent cross-nav leaks.
    /// </summary>
    /// <param name="viewKey">Per-view key being disposed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async ValueTask DisposeViewKeyAsync(string viewKey, CancellationToken cancellationToken = default) {
        IJSObjectReference? module = await TryGetModuleAsync(cancellationToken).ConfigureAwait(false);
        if (module is null) {
            return;
        }

        try {
            await module.InvokeVoidAsync("disposeViewKey", cancellationToken, viewKey).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) {
        }
        catch (JSException) {
        }
        catch (OperationCanceledException) {
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        if (_moduleTask is null) {
            return;
        }

        try {
            IJSObjectReference module = await _moduleTask.ConfigureAwait(false);
            await module.DisposeAsync().ConfigureAwait(false);
        }
        catch (JSDisconnectedException) {
        }
        catch (JSException) {
        }
        catch (OperationCanceledException) {
        }
    }

    private Task<IJSObjectReference> GetModuleAsync(CancellationToken cancellationToken) {
        if (_moduleTask is not null) {
            return _moduleTask;
        }

        lock (_importGate) {
            return _moduleTask ??= _jsRuntime
                .InvokeAsync<IJSObjectReference>("import", cancellationToken, ModulePath)
                .AsTask();
        }
    }

    private async ValueTask<IJSObjectReference?> TryGetModuleAsync(CancellationToken cancellationToken) {
        try {
            return await GetModuleAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) {
            ResetFaultedImport();
            return null;
        }
        catch (JSException) {
            ResetFaultedImport();
            return null;
        }
        catch (OperationCanceledException) {
            ResetFaultedImport();
            return null;
        }
    }

    private void ResetFaultedImport() {
        lock (_importGate) {
            if (_moduleTask is not null && (_moduleTask.IsFaulted || _moduleTask.IsCanceled)) {
                _moduleTask = null;
            }
        }
    }
}
