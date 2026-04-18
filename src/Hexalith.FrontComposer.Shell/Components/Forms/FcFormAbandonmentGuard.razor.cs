using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Lifecycle;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Blazor component: awaited tasks must resume on the component's sync context, so ConfigureAwait(false) is the wrong choice here.
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

namespace Hexalith.FrontComposer.Shell.Components.Forms;

/// <summary>
/// Story 2-5 Decisions D6 / D8 / D9 / D10 / D13 / D19 / D24 / AC6 / ADR-025 — full-page form abandonment
/// protection. Wraps a generated <c>CommandRenderMode.FullPage</c> form's children and:
/// <list type="bullet">
///   <item><description>Anchors the abandonment timer on <see cref="EditContext.OnFieldChanged"/> first-fire (D10 — mount-without-edit never arms the guard).</description></item>
///   <item><description>Intercepts internal navigation via <see cref="NavigationLock"/> (ADR-025 — not <c>beforeunload</c>) when elapsed ≥ <see cref="FcShellOptions.FormAbandonmentThresholdSeconds"/>.</description></item>
///   <item><description>Suppresses the warning while <see cref="ILifecycleStateService.GetState"/> reports <see cref="CommandLifecycleState.Submitting"/> or when the cascading <c>WrapperInitiatedNavigation</c> flag is set (D13 — Submitting-only suppression; Syncing FIRES).</description></item>
///   <item><description>Clears the leave-anyway bypass via <c>try/finally</c> so a failed navigation never leaks the flag (D24 / Red Team Attack-3).</description></item>
/// </list>
/// </summary>
public partial class FcFormAbandonmentGuard : ComponentBase, IDisposable {
    private EditContext? _subscribedEditContext;
    private DateTimeOffset? _firstEditAt;
    private bool _showingWarning;
    private string? _pendingTarget;
    private bool _isLeaving;
    private int _disposed;

    /// <summary>Gets or sets the form children wrapped by the guard.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the edit context exposed by the generated form (Story 2-5 Task 5.3 —
    /// <c>OnEditContextReady</c>). Required for D10 first-edit anchoring; when null the guard is inert.
    /// </summary>
    [Parameter]
    public EditContext? EditContext { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID bound to the form's lifecycle state so D13 can query
    /// <see cref="ILifecycleStateService.GetState"/> for Submitting suppression.
    /// </summary>
    [Parameter]
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a cascading flag indicating the wrapper initiated navigation itself
    /// (Story 2-5 D13 — e.g., <c>FcLifecycleWrapper</c>'s "Start over" page reload). When set,
    /// the guard yields without prompting.
    /// </summary>
    [CascadingParameter(Name = "WrapperInitiatedNavigation")]
    public bool WrapperInitiatedNavigation { get; set; }

    [Inject]
    private ILifecycleStateService LifecycleService { get; set; } = default!;

    [Inject]
    private IOptionsMonitor<FcShellOptions> ShellOptions { get; set; } = default!;

    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    [Inject]
    private ILogger<FcFormAbandonmentGuard> Logger { get; set; } = default!;

    [Inject]
    private TimeProvider Time { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        if (!ReferenceEquals(EditContext, _subscribedEditContext)) {
            UnsubscribeFromEditContext();
            if (EditContext is not null) {
                _subscribedEditContext = EditContext;
                _subscribedEditContext.OnFieldChanged += OnFirstEdit;
            }
        }
    }

    // D9 — "Stay on form" auto-focus is wired via FluentButton's AutoFocus="true" attribute;
    // the button renders every time the warning becomes visible, re-applying focus.

    private void OnFirstEdit(object? sender, FieldChangedEventArgs e) {
        // D10 — capture the moment of the first edit, then detach so later edits don't re-anchor.
        if (_firstEditAt is not null || _disposed != 0) {
            return;
        }

        _firstEditAt = Time.GetUtcNow();
        UnsubscribeFromEditContext();
    }

    private async Task HandleNavigationChangingAsync(LocationChangingContext context) {
        // D24 — `_isLeaving` is consumed on first re-entry: we set it in OnLeaveClickedAsync
        // before calling NavigateTo, and clear it here once the resulting LocationChanging
        // event has arrived. This works whether the navigation pipeline runs synchronously
        // (WebAssembly) or asynchronously (Server circuits) — review 2026-04-17 P4.
        if (_isLeaving) {
            _isLeaving = false;
            return;
        }

        if (_disposed != 0) {
            return;
        }

        // D13 — wrapper-initiated navigation (Start over) bypasses the guard.
        if (WrapperInitiatedNavigation) {
            Logger.LogInformation(
                "{Diag} Abandonment guard yielded for wrapper-initiated navigation. Target={Target}",
                FcDiagnosticIds.HFC2103_AbandonmentDuringSubmitting,
                context.TargetLocation);
            return;
        }

        // D10 — no edit, no protection.
        if (_firstEditAt is null) {
            return;
        }

        FcShellOptions opts = ShellOptions.CurrentValue;
        double elapsedSeconds = (Time.GetUtcNow() - _firstEditAt.Value).TotalSeconds;
        if (elapsedSeconds < opts.FormAbandonmentThresholdSeconds) {
            return;
        }

        // D13 revised — suppression applies ONLY to Submitting; Syncing (with or without ActionPrompt) FIRES.
        if (!string.IsNullOrEmpty(CorrelationId)) {
            CommandLifecycleState current = LifecycleService.GetState(CorrelationId);
            if (current == CommandLifecycleState.Submitting) {
                Logger.LogInformation(
                    "{Diag} Abandonment guard suppressed while lifecycle state is Submitting. CorrelationId={Cid}",
                    FcDiagnosticIds.HFC2103_AbandonmentDuringSubmitting,
                    RedactForLog(CorrelationId));
                return;
            }
        }

        _pendingTarget = context.TargetLocation;
        _showingWarning = true;
        context.PreventNavigation();
        // Review 2026-04-17 P5 — NavigationLock callbacks can run on a background thread in
        // Blazor Server; marshal back via InvokeAsync so StateHasChanged hits the render context.
        await InvokeAsync(StateHasChanged);
    }

    private Task OnStayClickedAsync() {
        _showingWarning = false;
        _pendingTarget = null;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task OnLeaveClickedAsync() {
        // Review 2026-04-17 P4/P9 — `_isLeaving` is a bypass flag *consumed* by the next
        // HandleNavigationChangingAsync entry (so the flag survives until the nav event lands,
        // which may be asynchronous under Blazor Server). On exception we still clear it so a
        // failed NavigateTo never leaks the bypass to an unrelated later nav (D24 / Red Team #3).
        string? target = _pendingTarget;
        _showingWarning = false;
        _pendingTarget = null;

        if (string.IsNullOrEmpty(target)) {
            StateHasChanged();
            return Task.CompletedTask;
        }

        _isLeaving = true;
        StateHasChanged();
        try {
            Nav.NavigateTo(target);
        }
        catch {
            _isLeaving = false;
            throw;
        }

        return Task.CompletedTask;
    }

    private async Task HandleBarKeyDownAsync(KeyboardEventArgs e) {
        // D9 — Escape on the warning bar triggers "Stay" (preserve work by default).
        // Review 2026-04-17 — no ConfigureAwait(false): continuations must stay on Blazor's sync context.
        if (e.Key == "Escape") {
            await OnStayClickedAsync();
        }
    }

    private void UnsubscribeFromEditContext() {
        if (_subscribedEditContext is not null) {
            _subscribedEditContext.OnFieldChanged -= OnFirstEdit;
            _subscribedEditContext = null;
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) {
            return;
        }

        UnsubscribeFromEditContext();
        GC.SuppressFinalize(this);
    }

    // Review 2026-04-17 — "hash" is a misnomer: this is a prefix-redaction helper, not a cryptographic hash.
    // Renamed from HashForLog so the name matches the behavior (first 8 chars + ellipsis).
    private static string RedactForLog(string correlationId)
        => correlationId.Length <= 8 ? correlationId : string.Concat(correlationId.AsSpan(0, 8), "…");
}
