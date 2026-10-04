using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

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
    private const string FocusModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js";
    private EditContext? _subscribedEditContext;
    private DateTimeOffset? _firstEditAt;
    private bool _showingWarning;
    private string? _pendingTarget;
    private bool _isLeaving;
    private int _disposed;
    private int _editedOriginCaptureSequence;

    // The field whose editor is the current captured origin. A capture round trip runs only when the
    // edited field changes, so typing in one field does not import the focus module per keystroke.
    private string? _capturedOriginFieldName;
    private ElementReference _guardRoot;
    private bool _stayFocusPending;
    private readonly string _instanceId = Guid.NewGuid().ToString("N");

    private string _warningTitleId => $"fc-form-abandonment-title-{_instanceId}";

    private string _warningDescriptionId => $"fc-form-abandonment-description-{_instanceId}";

    private string _stayButtonId => $"fc-form-abandonment-stay-{_instanceId}";

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

    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        if (!ReferenceEquals(EditContext, _subscribedEditContext)) {
            UnsubscribeFromEditContext();
            // Story 13.3 BH3-14 — a new EditContext belongs to a new form model, so the next edit
            // must capture its origin again even when it names the same field.
            _capturedOriginFieldName = null;
            if (EditContext is not null) {
                _subscribedEditContext = EditContext;
                _subscribedEditContext.OnFieldChanged += OnFieldEdited;
            }
        }
    }

    // D9 — "Stay on form" is focused when the warning appears: FluentButton's AutoFocus="true"
    // requests it and OnAfterRenderAsync moves focus there by id (FM-11), because AutoFocus alone
    // does not re-apply when the warning re-renders in an already interactive page.

    private void OnFieldEdited(object? sender, FieldChangedEventArgs e) {
        if (_disposed != 0) {
            return;
        }

        // Keep the timer anchored to the first edit while refreshing the deterministic focus
        // origin to the most recently edited control.
        _firstEditAt ??= Time.GetUtcNow();

        // Story 13.3 E4-08 — a split numeric binding also reports its private text buffer on the form
        // component ("_XString") before the model field ("X"). Only model fields name an editor, so a
        // foreign-model identifier must not re-capture the origin on every keystroke.
        if (!ReferenceEquals(e.FieldIdentifier.Model, _subscribedEditContext?.Model)) {
            return;
        }

        string fieldName = e.FieldIdentifier.FieldName;
        if (string.Equals(fieldName, _capturedOriginFieldName, StringComparison.Ordinal)) {
            return;
        }

        _capturedOriginFieldName = fieldName;
        int sequence = Interlocked.Increment(ref _editedOriginCaptureSequence);
        _ = InvokeAsync(() => CaptureEditedOriginAsync(fieldName, sequence));
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
            FrontComposerDiagnosticLog.AbandonmentGuardWrapperNavigationYielded(
                Logger,
                FcDiagnosticIds.HFC2103_AbandonmentDuringSubmitting,
                context.TargetLocation);
            return;
        }

        // D10 — no edit, no protection.
        if (_firstEditAt is null) {
            return;
        }

        if (EditContext is not null && !EditContext.IsModified()) {
            ResetFirstEditAnchor();
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
                FrontComposerDiagnosticLog.AbandonmentGuardSuppressedWhileSubmitting(
                    Logger,
                    FcDiagnosticIds.HFC2103_AbandonmentDuringSubmitting,
                    CorrelationId);
                return;
            }
        }

        _pendingTarget = context.TargetLocation;
        _showingWarning = true;
        _stayFocusPending = true;
        // A shell activation waiting for this route must settle as cancelled, so an open command
        // palette closes and this warning stays reachable.
        ServiceProvider.GetService<NavigationFailureNotifier>()?.CancelAttempt(Nav.ToAbsoluteUri(context.TargetLocation).AbsoluteUri);
        context.PreventNavigation();
        // Review 2026-04-17 P5 — NavigationLock callbacks can run on a background thread in
        // Blazor Server; marshal back via InvokeAsync so StateHasChanged hits the render context.
        await InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (!_stayFocusPending || _disposed != 0) {
            return;
        }

        _stayFocusPending = false;
        try {
            await using IJSObjectReference module = await JS.InvokeAsync<IJSObjectReference>("import", FocusModulePath);
            await module.InvokeVoidAsync("focusAbandonmentStay", _stayButtonId);
        }
        catch (JSDisconnectedException) {
        }
        catch (JSException) {
        }
        catch (InvalidOperationException) {
        }
    }

    private async Task OnStayClickedAsync() {
        _showingWarning = false;
        _pendingTarget = null;
        await InvokeAsync(StateHasChanged);
        await RestoreEditedOriginAsync();
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

    private async Task CaptureEditedOriginAsync(string fieldName, int sequence) {
        bool captured = false;
        try {
            await using IJSObjectReference module = await JS.InvokeAsync<IJSObjectReference>("import", FocusModulePath);
            captured = await module.InvokeAsync<bool>("captureEditedOrigin", _guardRoot, fieldName, sequence);
        }
        catch (JSDisconnectedException) {
        }
        catch (JSException) {
        }
        catch (InvalidOperationException) {
        }

        // A capture that did not bind (editor not rendered yet, or interop unavailable) must not
        // suppress the next edit of the same field from trying again.
        if (!captured
            && sequence == Volatile.Read(ref _editedOriginCaptureSequence)
            && string.Equals(fieldName, _capturedOriginFieldName, StringComparison.Ordinal)) {
            _capturedOriginFieldName = null;
        }
    }

    private async Task RestoreEditedOriginAsync() {
        try {
            await using IJSObjectReference module = await JS.InvokeAsync<IJSObjectReference>("import", FocusModulePath);
            await module.InvokeVoidAsync("restoreEditedOrigin", _guardRoot);
        }
        catch (JSDisconnectedException) {
        }
        catch (JSException) {
        }
        catch (InvalidOperationException) {
        }
    }

    private void UnsubscribeFromEditContext() {
        _subscribedEditContext?.OnFieldChanged -= OnFieldEdited;
        _subscribedEditContext = null;
    }

    private void ResetFirstEditAnchor() {
        _firstEditAt = null;
        _showingWarning = false;
        _pendingTarget = null;

        if (_disposed != 0 || EditContext is null) {
            return;
        }

        if (!ReferenceEquals(_subscribedEditContext, EditContext)) {
            UnsubscribeFromEditContext();
            _subscribedEditContext = EditContext;
            _subscribedEditContext.OnFieldChanged += OnFieldEdited;
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
}
