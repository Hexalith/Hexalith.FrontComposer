using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Services.Announcements;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Components.Lifecycle;

/// <summary>
/// Story 2-4 progressive-visibility wrapper rendered around generated command forms.
/// Subscribes to <see cref="ILifecycleStateService"/> and escalates visual feedback via a
/// <see cref="LifecycleThresholdTimer"/> at the configured <see cref="FcShellOptions"/> thresholds.
/// </summary>
public partial class FcLifecycleWrapper : ComponentBase, IAsyncDisposable, IDisposable {
    private const int DegradedAfterAcceptanceMs = 10_000;
    private const string FocusModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js";

    // Story 13.3 VR-03 — the Shell's never-throwing clipboard helper; it returns a structured outcome string.
    private const string ClipboardModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-devmode-clipboard.js";
    private string _boundCorrelationId = string.Empty;
    private IDisposable? _subscription;
    private IDisposable? _optionsChangeRegistration;
    private IDisposable? _projectionConnectionRegistration;
    private LifecycleThresholdTimer? _timer;
    private ITimer? _dismissTimer;
    private ITimer? _deadlineTimer;
    private ITimer? _degradedTimer;
    private DateTimeOffset? _acceptanceAt;
    private LifecycleUiState _state = LifecycleUiState.Idle;
    private ProjectionConnectionSnapshot _projectionConnectionSnapshot = new(
        ProjectionConnectionStatus.Connected,
        DateTimeOffset.MinValue,
        ReconnectAttempt: 0,
        LastFailureCategory: null);
    private int _disposed;
    private ElementReference _wrapperElement;

    // Story 13.3 VR-03 — outcome of the last "Copy support reference" activation for the current
    // rejection: null before any attempt, true when copied, false when the browser refused the copy.
    private bool? _supportReferenceCopied;

    // Story 13.3 VR-02 / AA10-07 — the operator cancelled the current field-mapped rejection's card.
    private bool _mappedRejectionDismissed;

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

    /// <summary>Gets or sets the stable command id used by browser E2E selectors.</summary>
    [Parameter]
    public string? CommandId { get; set; }

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
    /// Gets or sets optional typed rejection metadata. Plain text only; rendered with normal Blazor encoding.
    /// </summary>
    [Parameter]
    public CommandRejectionDetails? RejectionDetails { get; set; }

    /// <summary>
    /// Gets or sets whether the rejection owns a focused mapped validation summary. When true,
    /// the lifecycle region remains visible but suppresses its independent live announcement.
    /// </summary>
    [Parameter]
    public bool MappedRejection { get; set; }

    /// <summary>Gets or sets the localized command label used by the active lifecycle heading.</summary>
    [Parameter]
    public string? DisplayLabel { get; set; }

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
    private IProjectionConnectionState ProjectionConnectionState { get; set; } = default!;

    [Inject]
    private IOptionsMonitor<FcShellOptions> ShellOptions { get; set; } = default!;

    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    [Inject]
    private ILogger<FcLifecycleWrapper> Logger { get; set; } = default!;

    [Inject]
    private TimeProvider Time { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    [Inject]
    private ISurfaceAnnouncementCoordinator Announcements { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

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
            isDisconnected: () => ProjectionConnectionState.Current.IsDisconnected);
        _timer.OnPhaseChanged += OnPhaseChangedFromTimer;
        _optionsChangeRegistration = ShellOptions.OnChange((newOpts, _) => _timer?.UpdateThresholds(
                newOpts.SyncPulseThresholdMs,
                newOpts.StillSyncingThresholdMs,
                newOpts.TimeoutActionThresholdMs));

        _subscription = LifecycleService.Subscribe(correlationId, OnTransitionFromService);
        _projectionConnectionRegistration = ProjectionConnectionState.Subscribe(OnProjectionConnectionChanged);
    }

    private void UnbindCurrent() {
        IDisposable? sub = Interlocked.Exchange(ref _subscription, null);
        sub?.Dispose();

        IDisposable? changeReg = Interlocked.Exchange(ref _optionsChangeRegistration, null);
        changeReg?.Dispose();

        IDisposable? connectionReg = Interlocked.Exchange(ref _projectionConnectionRegistration, null);
        connectionReg?.Dispose();

        LifecycleThresholdTimer? timer = Interlocked.Exchange(ref _timer, null);
        if (timer is not null) {
            timer.OnPhaseChanged -= OnPhaseChangedFromTimer;
            timer.Dispose();
        }

        ITimer? dismiss = Interlocked.Exchange(ref _dismissTimer, null);
        dismiss?.Dispose();
        ITimer? deadline = Interlocked.Exchange(ref _deadlineTimer, null);
        deadline?.Dispose();
        CancelDegraded();
        Announcements.Cancel(AnnouncementSurface, _boundCorrelationId);

        _boundCorrelationId = string.Empty;
        _acceptanceAt = null;
        _state = LifecycleUiState.Idle;
    }

    private void OnTransitionFromService(CommandLifecycleTransition transition) {
        if (_disposed != 0) {
            return;
        }

        // HFC2100 race guard — wrapper received a transition for a CorrelationId it isn't bound to.
        if (!string.Equals(transition.CorrelationId, _boundCorrelationId, StringComparison.Ordinal)) {
            FrontComposerHotPathLog.LifecycleUnexpectedCorrelation(
                Logger,
                FcDiagnosticIds.HFC2100_UnknownCorrelationId,
                transition.CorrelationId);
            return;
        }

        if (transition.IdempotencyResolved) {
            FrontComposerHotPathLog.LifecycleIdempotencyResolved(
                Logger,
                FcDiagnosticIds.HFC2101_IdempotencyResolvedObserved,
                transition.CorrelationId);
        }

        string boundCorrelationId = _boundCorrelationId;
        _ = InvokeAsync(() => {
            if (_disposed != 0 || !string.Equals(boundCorrelationId, _boundCorrelationId, StringComparison.Ordinal)
                || !string.Equals(transition.CorrelationId, _boundCorrelationId, StringComparison.Ordinal)) {
                return;
            }
            ApplyTransition(transition);
            StateHasChanged();
        });
    }

    private void ApplyTransition(CommandLifecycleTransition transition) {
        LifecycleTimerPhase phase = _timer?.CurrentPhase ?? LifecycleTimerPhase.NoPulse;
        var next = LifecycleUiState.From(transition, phase, RejectionMessage, RejectionTitle, RejectionDetails);

        switch (transition.NewState) {
            case CommandLifecycleState.Acknowledged:
                _acceptanceAt ??= transition.LastTransitionAt;
                _timer?.Reset(_acceptanceAt.Value);
                _timer?.Start();
                ScheduleDeadline(_acceptanceAt.Value);
                ScheduleDegraded(_acceptanceAt.Value);
                phase = _timer?.CurrentPhase ?? LifecycleTimerPhase.NoPulse;
                next = next with { TimerPhase = phase };
                CancelDismissTimer();
                break;

            case CommandLifecycleState.Syncing:
            case CommandLifecycleState.Degraded:
                // A wrapper mounted after acknowledgement receives the original acceptance
                // anchor in the replay transition. Status polls never reset the budget.
                if (_acceptanceAt is null) {
                    _acceptanceAt = transition.LastTransitionAt;
                    _timer?.Reset(_acceptanceAt.Value);
                    _timer?.Start();
                    ScheduleDeadline(_acceptanceAt.Value);
                    ScheduleDegraded(_acceptanceAt.Value);
                }
                phase = _timer?.CurrentPhase ?? LifecycleTimerPhase.NoPulse;
                next = next with { TimerPhase = phase };
                CancelDismissTimer();
                break;

            case CommandLifecycleState.Confirmed:
            case CommandLifecycleState.IdempotentConfirmed:
                _timer?.EnterTerminal();
                CancelDeadline();
                CancelDegraded();
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

                    FrontComposerHotPathLog.LifecycleIdempotentInfoBarRendered(
                        Logger,
                        FcDiagnosticIds.HFC2104_IdempotentInfoBarRendered,
                        transition.CorrelationId);
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
            case CommandLifecycleState.NeedsReview:
            case CommandLifecycleState.Warning:
            case CommandLifecycleState.DegradedExhausted:
                _timer?.EnterTerminal();
                CancelDeadline();
                CancelDegraded();
                CancelDismissTimer();
                _supportReferenceCopied = null;
                _mappedRejectionDismissed = false;
                next = next with { TimerPhase = LifecycleTimerPhase.Terminal };
                break;

            case CommandLifecycleState.Idle:
                _timer?.EnterTerminal();
                CancelDeadline();
                CancelDegraded();
                CancelDismissTimer();
                _acceptanceAt = null;
                next = LifecycleUiState.Idle with { LastTransitionAt = transition.LastTransitionAt };
                break;

            case CommandLifecycleState.Submitting:
            default:
                CancelDismissTimer();
                break;
        }

        _state = next;
        AnnounceLifecycle(transition);
    }

    private void OnPhaseChangedFromTimer(LifecycleTimerPhase phase) {
        if (_disposed != 0) {
            return;
        }

        // HFC2102 — timer callback runs on the thread pool; render updates must go through InvokeAsync.
        // Capture the binding before the queue so a rebind cannot apply this tick to the replacement command.
        string boundCorrelationId = _boundCorrelationId;
        FrontComposerHotPathLog.LifecycleTimerPhaseMarshaled(
            Logger,
            FcDiagnosticIds.HFC2102_ThresholdTimerOffUiThread);

        _ = InvokeAsync(() => {
            if (_disposed != 0 || !string.Equals(boundCorrelationId, _boundCorrelationId, StringComparison.Ordinal)) {
                return;
            }

            // Ignore tick-driven changes once we've reached a terminal display state.
            if (_state.TimerPhase == LifecycleTimerPhase.Terminal) {
                return;
            }
            _state = _state with { TimerPhase = phase };
            StateHasChanged();
        });
    }

    private string AnnouncementSurface => "lifecycle:" + _boundCorrelationId;

    private void AnnounceLifecycle(CommandLifecycleTransition transition) {
        string? key = transition.NewState switch {
            CommandLifecycleState.Submitting => "Am10Submitting",
            CommandLifecycleState.Acknowledged => "Am11Acknowledged",
            CommandLifecycleState.Syncing => "Am12Syncing",
            CommandLifecycleState.Confirmed or CommandLifecycleState.IdempotentConfirmed => "Am13Confirmed",
            CommandLifecycleState.Rejected when !MappedRejection => "Am14Rejected",
            CommandLifecycleState.NeedsReview => "Am15NeedsReview",
            CommandLifecycleState.Warning => "Am16Warning",
            CommandLifecycleState.Degraded => "Am17Degraded",
            CommandLifecycleState.DegradedExhausted => "Am24Exhausted",
            _ => null,
        };
        bool terminal = _state.TimerPhase == LifecycleTimerPhase.Terminal;
        if (key is null) {
            if (terminal) {
                Announcements.Cancel(AnnouncementSurface, _boundCorrelationId);
            }
            return;
        }

        string message = transition.NewState is CommandLifecycleState.IdempotentConfirmed
            || (transition.NewState is CommandLifecycleState.Confirmed && _state.IsIdempotent)
            ? IdempotentInfoMessage ?? Localizer[key].Value
            : Localizer[key].Value;
        Announcements.Announce(
            AnnouncementSurface,
            _boundCorrelationId,
            transition.NewState.ToString(),
            message,
            terminal,
            immediate: transition.NewState == CommandLifecycleState.Degraded);
    }

    private void ScheduleDeadline(DateTimeOffset acceptedAt) {
        CancelDeadline();
        TimeSpan due = acceptedAt.AddMilliseconds(ShellOptions.CurrentValue.MaxPendingCommandPollingDurationMs) - Time.GetUtcNow();
        string correlationId = _boundCorrelationId;
        _deadlineTimer = Time.CreateTimer(_ => _ = InvokeAsync(() => {
            if (_disposed == 0 && string.Equals(_boundCorrelationId, correlationId, StringComparison.Ordinal)
                && _state.Current is CommandLifecycleState.Acknowledged or CommandLifecycleState.Syncing or CommandLifecycleState.Degraded) {
                bool resolved = false;
                if (_state.MessageId is { Length: > 0 } messageId
                    && Services.GetService(typeof(IPendingCommandStateService)) is IPendingCommandStateService pending) {
                    PendingCommandResolutionStatus status = pending.ResolveTerminal(new PendingCommandTerminalObservation(
                        messageId, PendingCommandTerminalOutcome.DegradedExhausted)).Status;
                    resolved = status is PendingCommandResolutionStatus.Resolved or PendingCommandResolutionStatus.DuplicateIgnored;
                }
                if (!resolved) {
                    LifecycleService.Transition(correlationId, CommandLifecycleState.DegradedExhausted, _state.MessageId);
                }
            }
        }), null, due > TimeSpan.Zero ? due : TimeSpan.Zero, Timeout.InfiniteTimeSpan);
    }

    private void ScheduleDegraded(DateTimeOffset acceptedAt) {
        CancelDegraded();
        TimeSpan due = acceptedAt.AddMilliseconds(DegradedAfterAcceptanceMs) - Time.GetUtcNow();
        string correlationId = _boundCorrelationId;
        _degradedTimer = Time.CreateTimer(_ => _ = InvokeAsync(() => {
            if (_disposed == 0 && string.Equals(_boundCorrelationId, correlationId, StringComparison.Ordinal)
                && _state.Current is CommandLifecycleState.Acknowledged or CommandLifecycleState.Syncing) {
                LifecycleService.Transition(correlationId, CommandLifecycleState.Degraded, _state.MessageId);
            }
        }), null, due > TimeSpan.Zero ? due : TimeSpan.Zero, Timeout.InfiniteTimeSpan);
    }

    private void CancelDegraded() {
        ITimer? timer = Interlocked.Exchange(ref _degradedTimer, null);
        timer?.Dispose();
    }

    private void CancelDeadline() {
        ITimer? timer = Interlocked.Exchange(ref _deadlineTimer, null);
        timer?.Dispose();
    }

    private void OnProjectionConnectionChanged(ProjectionConnectionSnapshot snapshot) {
        if (_disposed != 0) {
            return;
        }

        // P10 — capture the snapshot inside the InvokeAsync lambda so the renderer thread sees
        // a coherent value through the dispatcher's serialization barrier instead of a non-volatile
        // field write from the SignalR/state-service call chain.
        _ = InvokeAsync(() => {
            _projectionConnectionSnapshot = snapshot;
            if (_state.Current is CommandLifecycleState.Syncing) {
                LifecycleTimerPhase phase = _timer?.CurrentPhase ?? _state.TimerPhase;
                _state = _state with { TimerPhase = phase };
            }

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
                    if (_state.Current is CommandLifecycleState.IdempotentConfirmed
                        || (_state.Current == CommandLifecycleState.Confirmed && _state.IsIdempotent)) {
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

    /// <summary>
    /// Story 13.3 VR-02 / AA10-07 — "cancel" for a field-mapped rejection withdraws the mapped card. The
    /// entered values and the linked field errors stay, and focus moves to the form's first editable
    /// control so it never falls to the document body when the pressed button unmounts.
    /// </summary>
    private async Task DismissMappedRejectionAsync() {
        _mappedRejectionDismissed = true;
        StateHasChanged();
        await FocusEditableFormAsync().ConfigureAwait(true);
    }

    private async Task FocusEditableFormAsync() {
        IJSObjectReference? module = null;
        try {
            module = await JS.InvokeAsync<IJSObjectReference>("import", FocusModulePath).ConfigureAwait(true);
            await module.InvokeVoidAsync("focusFirstEditableWithin", _wrapperElement).ConfigureAwait(true);
        }
        catch (JSDisconnectedException) {
        }
        catch (JSException) {
        }
        catch (InvalidOperationException) {
        }
        finally {
            if (module is not null) {
                await DisposeFocusModuleAsync(module).ConfigureAwait(true);
            }
        }
    }

    /// <summary>
    /// Story 13.3 VR-03 — "Return" goes back to the previous page in the browser history and never
    /// resubmits. It behaves like the browser Back button: the abandonment guard applies to in-app
    /// history navigation on a guarded form, and a back to another document is not guarded.
    /// </summary>
    private async Task ReturnToPreviousPageAsync() {
        try {
            await JS.InvokeVoidAsync("history.back").ConfigureAwait(true);
        }
        catch (JSDisconnectedException) {
        }
        catch (JSException) {
        }
        catch (InvalidOperationException) {
        }
    }

    /// <summary>
    /// Story 13.3 VR-03 — copies the support-safe error and documentation codes that the rejection
    /// details already show. Nothing else (payload, backend metadata, identifiers) is copied.
    /// </summary>
    private async Task CopySupportReferenceAsync() {
        CommandRejectionDetails? details = RejectionDetails;
        if (details is null) {
            return;
        }

        string reference = string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            Localizer["RejectionSupportReferenceTemplate"],
            details.ErrorCode,
            details.DocsCode);
        IJSObjectReference? module = null;
        bool copied = false;
        try {
            module = await JS.InvokeAsync<IJSObjectReference>("import", ClipboardModulePath).ConfigureAwait(true);
            string outcome = await module.InvokeAsync<string>("copyToClipboard", reference).ConfigureAwait(true);
            copied = string.Equals(outcome, "Success", StringComparison.Ordinal);
        }
        catch (JSDisconnectedException) {
        }
        catch (JSException) {
        }
        catch (InvalidOperationException) {
        }
        finally {
            if (module is not null) {
                await DisposeFocusModuleAsync(module).ConfigureAwait(true);
            }
        }

        if (_disposed == 0) {
            _supportReferenceCopied = copied;
        }
    }

    private static async ValueTask DisposeFocusModuleAsync(IJSObjectReference module) {
        try {
            await module.DisposeAsync().ConfigureAwait(true);
        }
        catch (JSDisconnectedException) {
        }
        catch (JSException) {
        }
        catch (InvalidOperationException) {
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
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

}
