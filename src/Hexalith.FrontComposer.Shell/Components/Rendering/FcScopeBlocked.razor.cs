using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Components.Rendering;

/// <summary>
/// Story 13.1 — support-safe blocking region the Shell renders in place of home, projection, and
/// badge content while no validated tenant and user scope is current. It carries only a localized
/// heading and message, never tenant, user, or diagnostic values.
/// </summary>
public partial class FcScopeBlocked : IDisposable {
    private ElementReference _heading;
    private bool _focusRequested;
    private bool _disposed;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (_disposed || _focusRequested) {
            return;
        }

        try {
            await _heading.FocusAsync(preventScroll: true).ConfigureAwait(true);
            if (!_disposed) {
                _focusRequested = true;
            }
        }
        catch (JSDisconnectedException) {
        }
        catch (JSException) {
        }
        catch (InvalidOperationException) {
        }
        catch (OperationCanceledException) {
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
