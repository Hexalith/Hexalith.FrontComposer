using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Components.DataGrid;

/// <summary>
/// Story 4-4 T1.2 / D8 / D11 / AC3 — auto-dismissing <c>FluentMessageBar Intent="Info"</c> surfaced
/// when <c>LoadedPageState.LastElapsedMsByKey[ViewKey]</c> exceeds
/// <c>FcShellOptions.SlowQueryThresholdMs</c>. Auto-dismisses after 5 s via the injected
/// <see cref="TimeProvider"/> (<c>FakeTimeProvider.Advance(5_000)</c> in tests); dismiss state is
/// scoped to this component instance (not persisted across navigations).
/// </summary>
public partial class FcSlowQueryNotice : ComponentBase, IDisposable {
    private static readonly TimeSpan AutoDismissDelay = TimeSpan.FromSeconds(5);

    private bool _visible;
    private bool _dismissed;
    private long _lastObservedElapsedMs;
    private ITimer? _dismissTimer;

    /// <summary>Stable per-view key (<c>{boundedContext}:{projectionTypeFqn}</c>).</summary>
    [Parameter]
    [EditorRequired]
    public string ViewKey { get; set; } = string.Empty;

    [Inject]
    private IState<LoadedPageState> LoadedPage { get; set; } = default!;

    [Inject]
    private IOptionsMonitor<FcShellOptions> ShellOptions { get; set; } = default!;

    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    [Inject]
    private TimeProvider TimeProvider { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized() {
        LoadedPage.StateChanged += OnStateChanged;
        ReconcileVisibility();
    }

    /// <inheritdoc />
    protected override void OnParametersSet() => ReconcileVisibility();

    private void OnStateChanged(object? sender, EventArgs e) {
        ReconcileVisibility();
        InvokeAsync(StateHasChanged);
    }

    private void ReconcileVisibility() {
        if (_dismissed) {
            _visible = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(ViewKey)) {
            _visible = false;
            return;
        }

        if (!LoadedPage.Value.LastElapsedMsByKey.TryGetValue(ViewKey, out long elapsedMs)) {
            _visible = false;
            return;
        }

        bool overThreshold = elapsedMs > ShellOptions.CurrentValue.SlowQueryThresholdMs;
        if (overThreshold && elapsedMs != _lastObservedElapsedMs) {
            _visible = true;
            _lastObservedElapsedMs = elapsedMs;
            RestartAutoDismissTimer();
        }
        else if (!overThreshold) {
            _visible = false;
        }
    }

    private void RestartAutoDismissTimer() {
        _dismissTimer?.Dispose();
        _dismissTimer = TimeProvider.CreateTimer(
            callback: static state => ((FcSlowQueryNotice)state!).AutoDismiss(),
            state: this,
            dueTime: AutoDismissDelay,
            period: Timeout.InfiniteTimeSpan);
    }

    private void AutoDismiss() {
        _dismissed = true;
        _visible = false;
        InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    public void Dispose() {
        LoadedPage.StateChanged -= OnStateChanged;
        _dismissTimer?.Dispose();
    }
}
