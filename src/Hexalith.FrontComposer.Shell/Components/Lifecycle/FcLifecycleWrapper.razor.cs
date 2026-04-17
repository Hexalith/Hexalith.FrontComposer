using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Lifecycle;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Components.Lifecycle;

/// <summary>
/// Story 2-4 progressive-visibility wrapper rendered around generated command forms.
/// Subscribes to <see cref="ILifecycleStateService"/> and escalates visual feedback via a
/// <see cref="LifecycleThresholdTimer"/> at the configured <see cref="FcShellOptions"/> thresholds.
/// </summary>
public partial class FcLifecycleWrapper : ComponentBase, IAsyncDisposable, IDisposable {
    private string _boundCorrelationId = string.Empty;
    private IDisposable? _subscription;
    private IDisposable? _optionsChangeRegistration;
    private LifecycleThresholdTimer? _timer;
    private ITimer? _dismissTimer;
    private LifecycleUiState _state = LifecycleUiState.Idle;
    private int _disposed;

    /// <summary>Gets or sets the correlation identifier this wrapper tracks. Story 2-4 D1 — string, not Guid.</summary>
    [Parameter]
    [EditorRequired]
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>Gets or sets the wrapped content (typically a generated <c>EditForm</c>).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the optional domain-specific rejection copy. Story 2-5 will populate; null → generic fallback (D22 XSS: rendered as plain text, never MarkupString).</summary>
    [Parameter]
    public string? RejectionMessage { get; set; }

    [Inject]
    private ILifecycleStateService LifecycleService { get; set; } = default!;

    [Inject]
    private IOptionsMonitor<FcShellOptions> ShellOptions { get; set; } = default!;

    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    [Inject]
    private ILogger<FcLifecycleWrapper> Logger { get; set; } = default!;

    [Inject]
    private TimeProvider Time { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized() {
        // D14 — synchronous subscribe in OnInitialized so the replay callback lands before first render.
        if (!string.IsNullOrEmpty(CorrelationId)) {
            BindToCorrelationId(CorrelationId);
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        // D15 — if CorrelationId changes between renders, dispose old subscription and re-bind.
        if (!string.Equals(CorrelationId, _boundCorrelationId, StringComparison.Ordinal)) {
            UnbindCurrent();
            if (!string.IsNullOrEmpty(CorrelationId)) {
                BindToCorrelationId(CorrelationId);
            }
        }
    }

    private void BindToCorrelationId(string correlationId) {
        _boundCorrelationId = correlationId;
        _state = LifecycleUiState.Idle;

        FcShellOptions opts = ShellOptions.CurrentValue;
        _timer = new LifecycleThresholdTimer(
            Time,
            opts.SyncPulseThresholdMs,
            opts.StillSyncingThresholdMs,
            opts.TimeoutActionThresholdMs,
            isDisconnected: null);
        _timer.OnPhaseChanged += OnPhaseChangedFromTimer;
        _optionsChangeRegistration = ShellOptions.OnChange((newOpts, _) => {
            _timer?.UpdateThresholds(
                newOpts.SyncPulseThresholdMs,
                newOpts.StillSyncingThresholdMs,
                newOpts.TimeoutActionThresholdMs);
        });

        _subscription = LifecycleService.Subscribe(correlationId, OnTransitionFromService);
    }

    private void UnbindCurrent() {
        IDisposable? sub = Interlocked.Exchange(ref _subscription, null);
        sub?.Dispose();

        IDisposable? changeReg = Interlocked.Exchange(ref _optionsChangeRegistration, null);
        changeReg?.Dispose();

        LifecycleThresholdTimer? timer = Interlocked.Exchange(ref _timer, null);
        if (timer is not null) {
            timer.OnPhaseChanged -= OnPhaseChangedFromTimer;
            timer.Dispose();
        }

        ITimer? dismiss = Interlocked.Exchange(ref _dismissTimer, null);
        dismiss?.Dispose();

        _boundCorrelationId = string.Empty;
        _state = LifecycleUiState.Idle;
    }

    private void OnTransitionFromService(CommandLifecycleTransition transition) {
        if (_disposed != 0) {
            return;
        }

        // HFC2100 race guard — wrapper received a transition for a CorrelationId it isn't bound to.
        if (!string.Equals(transition.CorrelationId, _boundCorrelationId, StringComparison.Ordinal)) {
            Logger.LogWarning(
                "{Diag} — FcLifecycleWrapper received transition for unexpected CorrelationId={Cid}",
                FcDiagnosticIds.HFC2100_UnknownCorrelationId,
                HashForLog(transition.CorrelationId));
            return;
        }

        if (transition.IdempotencyResolved) {
            Logger.LogInformation(
                "{Diag} — idempotency-resolved transition observed for CorrelationId={Cid}",
                FcDiagnosticIds.HFC2101_IdempotencyResolvedObserved,
                HashForLog(transition.CorrelationId));
        }

        _ = InvokeAsync(() => {
            ApplyTransition(transition);
            StateHasChanged();
        });
    }

    private void ApplyTransition(CommandLifecycleTransition transition) {
        LifecycleTimerPhase phase = _timer?.CurrentPhase ?? LifecycleTimerPhase.NoPulse;
        LifecycleUiState next = LifecycleUiState.From(transition, phase, RejectionMessage);

        switch (transition.NewState) {
            case CommandLifecycleState.Acknowledged:
            case CommandLifecycleState.Syncing:
                _timer?.Reset(transition.LastTransitionAt);
                _timer?.Start();
                phase = _timer?.CurrentPhase ?? LifecycleTimerPhase.NoPulse;
                next = next with { TimerPhase = phase };
                CancelDismissTimer();
                break;

            case CommandLifecycleState.Confirmed:
                _timer?.EnterTerminal();
                ScheduleConfirmedDismiss();
                next = next with {
                    TimerPhase = LifecycleTimerPhase.Terminal,
                    ConfirmedDismissAt = Time.GetUtcNow().AddMilliseconds(ShellOptions.CurrentValue.ConfirmedToastDurationMs),
                };
                break;

            case CommandLifecycleState.Rejected:
                _timer?.EnterTerminal();
                CancelDismissTimer();
                next = next with { TimerPhase = LifecycleTimerPhase.Terminal };
                break;

            case CommandLifecycleState.Idle:
                _timer?.EnterTerminal();
                CancelDismissTimer();
                next = LifecycleUiState.Idle with { LastTransitionAt = transition.LastTransitionAt };
                break;

            case CommandLifecycleState.Submitting:
            default:
                CancelDismissTimer();
                break;
        }

        _state = next;
    }

    private void OnPhaseChangedFromTimer(LifecycleTimerPhase phase) {
        if (_disposed != 0) {
            return;
        }

        // HFC2102 — timer callback runs on the thread pool; render updates must go through InvokeAsync.
        Logger.LogDebug(
            "{Diag} Threshold timer phase update received off the UI thread; marshaling render work via InvokeAsync.",
            FcDiagnosticIds.HFC2102_ThresholdTimerOffUiThread);

        _ = InvokeAsync(() => {
            // Ignore tick-driven changes once we've reached a terminal display state.
            if (_state.Current is CommandLifecycleState.Confirmed or CommandLifecycleState.Rejected) {
                return;
            }
            _state = _state with { TimerPhase = phase };
            StateHasChanged();
        });
    }

    private void ScheduleConfirmedDismiss() {
        CancelDismissTimer();
        int durationMs = ShellOptions.CurrentValue.ConfirmedToastDurationMs;
        _dismissTimer = Time.CreateTimer(
            _ => {
                if (_disposed != 0) {
                    return;
                }
                _ = InvokeAsync(() => {
                    if (_state.Current == CommandLifecycleState.Confirmed) {
                        _state = LifecycleUiState.Idle with { LastTransitionAt = _state.LastTransitionAt };
                        StateHasChanged();
                    }
                });
            },
            state: null,
            dueTime: TimeSpan.FromMilliseconds(durationMs),
            period: Timeout.InfiniteTimeSpan);
    }

    private void CancelDismissTimer() {
        ITimer? toDispose = Interlocked.Exchange(ref _dismissTimer, null);
        toDispose?.Dispose();
    }

    private void OnStartOverClicked() {
        // ADR-022 — page reload is the minimum-viable recovery.
        Nav.NavigateTo(Nav.Uri, forceLoad: true);
    }

    /// <inheritdoc />
    public void Dispose() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) {
            return;
        }
        UnbindCurrent();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private static string HashForLog(string correlationId)
        => correlationId.Length <= 8 ? correlationId : string.Concat(correlationId.AsSpan(0, 8), "…");
}
