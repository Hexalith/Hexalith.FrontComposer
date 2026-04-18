using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.FluentUI.AspNetCore.Components;

// Blazor component: awaited tasks must resume on the component's sync context, so ConfigureAwait(false) is the wrong choice here.
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

namespace Hexalith.FrontComposer.Shell.Components.Forms;

/// <summary>
/// Story 2-5 Decision D11 / D12 / D22 / AC3 — destructive confirmation dialog shown via
/// <see cref="IDialogService"/> before a destructive command's submit handler fires.
/// Cancel is auto-focused so <c>Enter</c> does the safe thing; <c>Escape</c> dispatches cancel
/// (D22 defense-in-depth); the destructive action button uses Fluent UI v5 error color slot.
/// </summary>
/// <remarks>
/// Opens via
/// <c>DialogService.ShowDialogAsync&lt;FcDestructiveConfirmationDialog&gt;(options =&gt; options.Parameters.Add(...))</c>.
/// The caller reads <c>DialogResult.Cancelled</c> to decide whether to invoke the submit delegate.
/// Copy is framework-controlled plain text (UX-DR57 + D14 XSS invariant — never <c>MarkupString</c>).
/// </remarks>
public partial class FcDestructiveConfirmationDialog : ComponentBase {
    /// <summary>
    /// Gets or sets the dialog instance cascaded by <see cref="IDialogService"/>. Null when the
    /// component is rendered standalone (tests).
    /// </summary>
    [CascadingParameter]
    public IDialogInstance? Dialog { get; set; }

    /// <summary>Gets or sets the dialog title (D12 — falls back to <c>{DisplayLabel}?</c> at the call site when null).</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Gets or sets the dialog body (D12 — falls back to localized "This action cannot be undone." at the call site when null).</summary>
    [Parameter]
    public string? Body { get; set; }

    /// <summary>Gets or sets the destructive action button label (D12 — the command's domain-language <c>DisplayLabel</c>).</summary>
    [Parameter]
    public string? DestructiveLabel { get; set; }

    /// <summary>
    /// Gets or sets the confirm callback invoked when the user clicks the destructive button.
    /// Optional in tests; in production the caller relies on <see cref="IDialogService"/>'s result.
    /// </summary>
    [Parameter]
    public EventCallback OnConfirm { get; set; }

    /// <summary>
    /// Gets or sets the cancel callback invoked when the user clicks Cancel or presses <c>Escape</c>.
    /// Optional in tests; in production the caller relies on <see cref="IDialogService"/>'s result.
    /// </summary>
    [Parameter]
    public EventCallback OnCancel { get; set; }

    // Review 2026-04-17 — Blazor UI handlers must stay on the sync context; removed ConfigureAwait(false)
    // throughout. Dialog.CloseAsync / CancelAsync now run in a finally block so a throwing OnConfirm /
    // OnCancel callback cannot leave the IDialogInstance open indefinitely (edge finding ECH-11).
    private async Task HandleKeyDownAsync(KeyboardEventArgs e) {
        // D22 — Escape dispatches cancel (safest keyboard path).
        if (e.Key == "Escape") {
            await InvokeCancelAsync();
        }
    }

    private Task OnCancelAsync() => InvokeCancelAsync();

    private async Task OnConfirmAsync() {
        try {
            if (OnConfirm.HasDelegate) {
                await OnConfirm.InvokeAsync();
            }
        }
        finally {
            if (Dialog is not null) {
                await Dialog.CloseAsync();
            }
        }
    }

    private async Task InvokeCancelAsync() {
        try {
            if (OnCancel.HasDelegate) {
                await OnCancel.InvokeAsync();
            }
        }
        finally {
            if (Dialog is not null) {
                await Dialog.CancelAsync();
            }
        }
    }
}
