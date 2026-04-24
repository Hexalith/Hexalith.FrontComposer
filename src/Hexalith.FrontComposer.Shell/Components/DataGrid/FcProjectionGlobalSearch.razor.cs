using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Hexalith.FrontComposer.Shell.Components.DataGrid;

/// <summary>
/// Story 4-3 T1.3 / D6 / AC4 — optional global search input. Rendered by the generated view
/// only when an <c>IProjectionSearchProvider&lt;T&gt;</c> is registered (nullable-DI). Wraps
/// <c>FluentSearch</c> with a 300 ms TimeProvider-anchored debounce that dispatches
/// <see cref="GlobalSearchChangedAction"/> on debounce-complete.
/// </summary>
public partial class FcProjectionGlobalSearch : ComponentBase, IDisposable {
    private static readonly TimeSpan DebounceInterval = TimeSpan.FromMilliseconds(300);

    private string _value = string.Empty;
    private string _lastAppliedInitialValue = string.Empty;
    private bool _hasAppliedInitialValue;
    private string _placeholder = string.Empty;
    private string _ariaLabel = string.Empty;
    private CancellationTokenSource? _pending;

    /// <summary>Stable per-view key.</summary>
    [Parameter]
    [EditorRequired]
    public string ViewKey { get; set; } = string.Empty;

    /// <summary>Initial query seeded from hydrated snapshot.</summary>
    [Parameter]
    public string? InitialValue { get; set; }

    [Inject]
    private IDispatcher Dispatcher { get; set; } = default!;

    [Inject]
    private TimeProvider Time { get; set; } = default!;

    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        string initialValue = InitialValue ?? string.Empty;
        if (!_hasAppliedInitialValue || !string.Equals(_lastAppliedInitialValue, initialValue, StringComparison.Ordinal)) {
            _value = initialValue;
            _lastAppliedInitialValue = initialValue;
            _hasAppliedInitialValue = true;
        }

        _placeholder = Localizer["GlobalSearchPlaceholder"].Value;
        _ariaLabel = Localizer["GlobalSearchAriaLabel"].Value;
    }

    private async Task OnValueChangedAsync(string? newValue) {
        _value = newValue ?? string.Empty;

        CancellationTokenSource? previous = Interlocked.Exchange(ref _pending, new CancellationTokenSource());
        previous?.Cancel();
        previous?.Dispose();

        CancellationTokenSource current = _pending!;
        CancellationToken token = current.Token;
        try {
            await Task.Delay(DebounceInterval, Time, token).ConfigureAwait(true);
        }
        catch (OperationCanceledException) {
            return;
        }

        if (token.IsCancellationRequested) {
            return;
        }

        string? payload = string.IsNullOrWhiteSpace(_value) ? null : _value;
        Dispatcher.Dispatch(new GlobalSearchChangedAction(ViewKey, payload));
    }

    /// <inheritdoc />
    public void Dispose() {
        CancellationTokenSource? cts = Interlocked.Exchange(ref _pending, null);
        try {
            cts?.Cancel();
        }
        catch (ObjectDisposedException) {
            // already disposed
        }
        finally {
            cts?.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
