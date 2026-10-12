using Hexalith.FrontComposer.Shell.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
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
    private int _focusAttempt;
    private int? _focusedGeneration;
    private ScopeBoundaryService? _scope;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized() {
        _scope = Services.GetService<ScopeBoundaryService>();
        if (_scope is not null) {
            _scope.Changed += OnScopeChanged;
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (_disposed || _focusRequested) {
            return;
        }

        ScopeBoundaryService? scope = _scope;
        int? generation = scope?.Generation;
        if (scope is not null && generation is int current && !ScopeBlockedFocusOwnership.For(scope).TryOwn(current, this)) {
            _focusRequested = true;
            _focusedGeneration = current;
            return;
        }

        int attempt = ++_focusAttempt;
        try {
            await _heading.FocusAsync(preventScroll: true).ConfigureAwait(true);
            if (!_disposed && _focusAttempt == attempt) {
                _focusRequested = true;
                _focusedGeneration = generation;
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
        if (_scope is not null) {
            _scope.Changed -= OnScopeChanged;
        }

        GC.SuppressFinalize(this);
    }

    private void OnScopeChanged(object? sender, EventArgs e) {
        _ = InvokeAsync(() => {
            if (_disposed || _scope is null || _focusedGeneration == _scope.Generation) {
                return;
            }

            _focusAttempt++;
            _focusRequested = false;
            StateHasChanged();
        });
    }
}
