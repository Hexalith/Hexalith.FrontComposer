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

    // Review 2026-04-17 P3 — cascaded as WrapperInitiatedNavigation so FcFormAbandonmentGuard
    // can bypass its warning when the wrapper itself triggers a Start-over navigation.
    private bool _wrapperInitiatedNavigation;

    /// <summary>Gets or sets the correlation identifier this wrapper tracks. Story 2-4 D1 — string, not Guid.</summary>
    [Parameter]
    [EditorRequired]
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>Gets or sets the wrapped content (typically a generated <c>EditForm</c>).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the optional domain-specific rejection copy. Story 2-5 populates; null → generic fallback (D22 XSS: rendered as plain text, never MarkupString).</summary>
    [Parameter]
    public string? RejectionMessage { get; set; }

    /// <summary>
    /// Story 2-5 D4 / D17 — optional domain-language rejection title (e.g., "Approval failed").
    /// Null → falls back to Story 2-4's localized "Submission rejected". Plain text only per D14.
    /// </summary>
    [Parameter]
    public string? RejectionTitle { get; set; }

    /// <summary>
    /// Story 2-5 D3 / D7 / D17 — optional adopter-supplied idempotent Info copy override.
    /// Null → framework default "This was already confirmed — no action needed." (AC2 front-loaded
    /// reassurance — safe under both cross-user and self-reconnect replay contexts). Plain text only per D14.
    /// </summary>
    [Parameter]
    public string? IdempotentInfoMessage { get; set; }

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
                RedactForLog(transition.CorrelationId));
            return;
        }

        if (transition.IdempotencyResolved) {
            Logger.LogInformation(
                "{Diag} — idempotency-resolved transition observed for CorrelationId={Cid}",
                FcDiagnosticIds.HFC2101_IdempotencyResolvedObserved,
                RedactForLog(transition.CorrelationId));
        }

        _ = InvokeAsync(() => {
            ApplyTransition(transition);
            StateHasChanged();
        });
    }

    private void ApplyTransition(CommandLifecycleTransition transition) {
        LifecycleTimerPhase phase = _timer?.CurrentPhase ?? LifecycleTimerPhase.NoPulse;
        LifecycleUiState next = LifecycleUiState.From(transition, phase, RejectionMessage, RejectionTitle);

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
                if (next.IsIdempotent) {
                    // Story 2-5 D3 / AC2 — idempotent outcome schedules Info-bar dismiss at the
                    // IdempotentInfoToastDurationMs threshold, not ConfirmedToastDurationMs.
                    int durationMs = ShellOptions.CurrentValue.IdempotentInfoToastDurationMs;
                    DateTimeOffset dismissAt = transition.LastTransitionAt.AddMilliseconds(durationMs);
                    ScheduleIdempotentDismiss(transition.LastTransitionAt, durationMs);
                    next = next with {
                        TimerPhase = LifecycleTimerPhase.Terminal,
                        IdempotentDismissAt = dismissAt,
                    };

                    Logger.LogInformation(
                        "{Diag} FcLifecycleWrapper rendered idempotent Info bar. CorrelationId={Cid}",
                        FcDiagnosticIds.HFC2104_IdempotentInfoBarRendered,
                        RedactForLog(transition.CorrelationId));
                }
                else {
                    int confirmedMs = ShellOptions.CurrentValue.ConfirmedToastDurationMs;
                    DateTimeOffset confirmedDismissAt = transition.LastTransitionAt.AddMilliseconds(confirmedMs);
                    ScheduleConfirmedDismiss(transition.LastTransitionAt, confirmedMs);
                    next = next with {
                        TimerPhase = LifecycleTimerPhase.Terminal,
                        ConfirmedDismissAt = confirmedDismissAt,
                    };
                }

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

    private void ScheduleConfirmedDismiss(DateTimeOffset transitionLastAtUtc, int durationMs) {
        CancelDismissTimer();
        TimeSpan due = ComputeDueTimeFromTransitionAnchor(transitionLastAtUtc, durationMs);
        _dismissTimer = Time.CreateTimer(
            _ => {
                if (_disposed != 0) {
                    return;
                }
                _ = InvokeAsync(() => {
                    // Review 2026-04-17 P10 — guard mirrors ScheduleIdempotentDismiss: only dismiss
                    // when we are STILL in the non-idempotent Confirmed branch. Rapid transitions
                    // that flip between idempotent/non-idempotent otherwise risk the wrong timer
                    // dismissing the wrong bar.
                    if (_state.Current == CommandLifecycleState.Confirmed && !_state.IsIdempotent) {
                        _state = LifecycleUiState.Idle with { LastTransitionAt = _state.LastTransitionAt };
                        StateHasChanged();
                    }
                });
            },
            state: null,
            dueTime: due,
            period: Timeout.InfiniteTimeSpan);
    }

    private void ScheduleIdempotentDismiss(DateTimeOffset transitionLastAtUtc, int durationMs) {
        CancelDismissTimer();
        TimeSpan due = ComputeDueTimeFromTransitionAnchor(transitionLastAtUtc, durationMs);
        _dismissTimer = Time.CreateTimer(
            _ => {
                if (_disposed != 0) {
                    return;
                }
                _ = InvokeAsync(() => {
                    if (_state.Current == CommandLifecycleState.Confirmed && _state.IsIdempotent) {
                        _state = LifecycleUiState.Idle with { LastTransitionAt = _state.LastTransitionAt };
                        StateHasChanged();
                    }
                });
            },
            state: null,
            dueTime: due,
            period: Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// AC2 — timer fires at <paramref name="transitionLastAtUtc"/> + duration, not at wall-clock
    /// handler time, so dismiss aligns with the lifecycle anchor when the UI thread is delayed.
    /// </summary>
    private TimeSpan ComputeDueTimeFromTransitionAnchor(DateTimeOffset transitionLastAtUtc, int durationMs) {
        DateTimeOffset fireAt = transitionLastAtUtc.AddMilliseconds(durationMs);
        TimeSpan due = fireAt - Time.GetUtcNow();
        return due <= TimeSpan.Zero ? TimeSpan.Zero : due;
    }

    private void CancelDismissTimer() {
        ITimer? toDispose = Interlocked.Exchange(ref _dismissTimer, null);
        toDispose?.Dispose();
    }

    private void OnStartOverClicked() {
        // ADR-022 — page reload is the minimum-viable recovery.
        // Review 2026-04-17 P3 — flag the wrapper-initiated nav so FcFormAbandonmentGuard's
        // CascadingParameter bypass fires. forceLoad:true bypasses NavigationLock anyway (full
        // document reload), but flipping the flag first makes the defense correct under any
        // future non-forceLoad Start-over variant.
        _wrapperInitiatedNavigation = true;
        try {
            Nav.NavigateTo(Nav.Uri, forceLoad: true);
        }
        catch {
            _wrapperInitiatedNavigation = false;
            throw;
        }
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

    // Review 2026-04-17 — "hash" is a misnomer: this is a prefix-redaction helper, not a cryptographic hash.
    // Renamed from HashForLog so the name matches the behavior (first 8 chars + ellipsis).
    private static string RedactForLog(string correlationId)
        => correlationId.Length <= 8 ? correlationId : string.Concat(correlationId.AsSpan(0, 8), "…");
}
