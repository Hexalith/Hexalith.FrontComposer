using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Components.Rendering;

/// <summary>
/// Story 13.1 — support-safe blocking region the Shell renders in place of home, projection, and
/// badge content while no validated tenant and user scope is current. It carries only a localized
/// heading and message, never tenant, user, or diagnostic values.
/// </summary>
public partial class FcScopeBlocked {
    private ElementReference _heading;
    private bool _focusRequested;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (_focusRequested) {
            return;
        }

        try {
            await _heading.FocusAsync(preventScroll: true).ConfigureAwait(true);
            _focusRequested = true;
        }
        catch (JSDisconnectedException) {
        }
        catch (JSException) {
        }
        catch (InvalidOperationException) {
        }
    }
}
