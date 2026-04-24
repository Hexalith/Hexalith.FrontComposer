using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Hexalith.FrontComposer.Shell.Components.DataGrid;

/// <summary>
/// Story 4-3 T1.1 / D1 / D4 / AC1 — per-column filter input with a 300 ms TimeProvider-anchored
/// debounce. On debounce-complete dispatches
/// <see cref="ColumnFilterChangedAction"/>; whitespace dispatches with <c>FilterValue = null</c>
/// (clears the filter). Owns a per-instance <see cref="CancellationTokenSource"/> that is
/// cancelled on dispose.
/// </summary>
public partial class FcColumnFilterCell : ComponentBase, IDisposable {
    private static readonly TimeSpan DebounceInterval = TimeSpan.FromMilliseconds(300);

    private string _value = string.Empty;
    private string _lastAppliedInitialValue = string.Empty;
    private bool _hasAppliedInitialValue;
    private string _placeholder = string.Empty;
    private string _ariaLabel = string.Empty;
    private CancellationTokenSource? _pending;

    /// <summary>Stable per-view key used by the filter action.</summary>
    [Parameter]
    [EditorRequired]
    public string ViewKey { get; set; } = string.Empty;

    /// <summary>Declared property name of the filtered column.</summary>
    [Parameter]
    [EditorRequired]
    public string ColumnKey { get; set; } = string.Empty;

    /// <summary>Humanised column header, used in placeholder / aria-label templates.</summary>
    [Parameter]
    public string? ColumnHeader { get; set; }

    /// <summary>Initial filter value supplied from the hydrated snapshot.</summary>
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

        string header = string.IsNullOrWhiteSpace(ColumnHeader) ? ColumnKey : ColumnHeader!;
        _placeholder = Localizer["ColumnFilterPlaceholderTemplate", header].Value;
        _ariaLabel = Localizer["ColumnFilterAriaLabelTemplate", header].Value;
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
        Dispatcher.Dispatch(new ColumnFilterChangedAction(ViewKey, ColumnKey, payload));
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
