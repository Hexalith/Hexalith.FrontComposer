using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Components.Forms;

/// <summary>
/// Renders one complete, linked, focus-only validation summary in declared field order.
/// </summary>
public partial class FcValidationSummary : ComponentBase, IDisposable {
    private const string FocusModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js";
    private readonly List<(string Message, string? InputId, string? Label)> _entries = [];
    private EditContext? _subscribedEditContext;
    private bool _focusPending;
    private bool _visible;

    // A summary link cancels its native fragment navigation only after scripted focus has been
    // proven to work; until then, or after a scripted failure, the native href remains the fallback.
    private bool _scriptFocusAvailable;
    private int _disposed;
    private FcValidationSummaryKind _kind;

    /// <summary>Gets or sets the edit context whose messages are summarized.</summary>
    [Parameter]
    [EditorRequired]
    public EditContext? EditContext { get; set; }

    /// <summary>Gets or sets fields in their declared DOM order.</summary>
    [Parameter]
    public IReadOnlyList<FcValidationFieldDescriptor> Fields { get; set; } = [];

    /// <summary>Gets or sets support-safe form-level errors that have no field target.</summary>
    [Parameter]
    public IReadOnlyList<string> FormLevelErrors { get; set; } = [];

    /// <summary>Gets or sets the stable summary DOM identifier.</summary>
    [Parameter]
    [EditorRequired]
    public string SummaryId { get; set; } = string.Empty;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    private string TitleId => $"{SummaryId}-title";

    private string DescriptionId => $"{SummaryId}-description";

    private string ValidationKindAttribute => _kind == FcValidationSummaryKind.MappedServerRejection
        ? "mapped-server-rejection"
        : "client-validation";

    private string Title => Resolve(
        _kind == FcValidationSummaryKind.MappedServerRejection
            ? "MappedRejectionSummaryTitle"
            : "ValidationSummaryTitle",
        _kind == FcValidationSummaryKind.MappedServerRejection
            ? "Command rejected"
            : "Validation failed");

    private string Description {
        get {
            bool singular = _entries.Count == 1;
            string template = Resolve(
                _kind == FcValidationSummaryKind.MappedServerRejection
                    ? singular ? "MappedRejectionSummaryMessageSingle" : "MappedRejectionSummaryMessage"
                    : singular ? "ValidationSummaryMessageSingle" : "ValidationSummaryMessage",
                _kind == FcValidationSummaryKind.MappedServerRejection
                    ? singular
                        ? "The command was rejected. Correct the mapped error before submitting again. One error."
                        : "The command was rejected. Correct the mapped errors before submitting again. {0} errors."
                    : singular
                        ? "Correct the error before submitting. One error."
                        : "Correct the errors before submitting. {0} errors.");
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, template, _entries.Count);
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet() {
        if (!ReferenceEquals(EditContext, _subscribedEditContext)) {
            Unsubscribe();
            _subscribedEditContext = EditContext;
            if (_subscribedEditContext is not null) {
                _subscribedEditContext.OnValidationStateChanged += OnValidationStateChanged;
            }
        }

        if (_visible) {
            RebuildEntries();
        }
    }

    /// <summary>Shows the complete current summary and schedules its single focus move.</summary>
    /// <param name="kind">The validation outcome kind.</param>
    /// <returns>A task that completes after the summary is rebuilt and rendered on the renderer dispatcher.</returns>
    /// <remarks>
    /// Callers run after <c>ConfigureAwait(false)</c> continuations, so every state change is
    /// marshalled onto the renderer dispatcher and cannot race a Fluxor-triggered render (BH2-07).
    /// </remarks>
    public Task ShowAndFocusAsync(FcValidationSummaryKind kind)
        => InvokeAsync(() => {
            _kind = kind;
            _visible = true;
            _focusPending = true;
            RebuildEntries();
            StateHasChanged();
        });

    /// <summary>Clears the visible summary without changing validation messages.</summary>
    /// <returns>A task that completes after the summary is hidden on the renderer dispatcher.</returns>
    public Task HideAsync()
        => InvokeAsync(() => {
            if (_visible || _entries.Count != 0) {
                _visible = false;
                _focusPending = false;
                _entries.Clear();
                StateHasChanged();
            }
        });

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (!_focusPending || _disposed != 0) {
            return;
        }

        _focusPending = false;
        IJSObjectReference? module = null;
        try {
            module = await JS.InvokeAsync<IJSObjectReference>("import", FocusModulePath).ConfigureAwait(true);
            await module.InvokeVoidAsync("focusValidationOutcome", SummaryId).ConfigureAwait(true);
            if (!_scriptFocusAvailable && _disposed == 0) {
                _scriptFocusAvailable = true;
                StateHasChanged();
            }
        }
        catch (JSDisconnectedException) {
            // Circuit teardown after validation is safe to ignore.
        }
        catch (JSException) {
            // Focus enhancement is unavailable; the complete linked summary remains rendered.
        }
        catch (InvalidOperationException) {
            // Prerender has no interactive JS runtime.
        }
        finally {
            if (module is not null) {
                await DisposeModuleAsync(module).ConfigureAwait(true);
            }
        }
    }

    private async Task FocusFieldAsync(string inputId) {
        IJSObjectReference? module = null;
        try {
            module = await JS.InvokeAsync<IJSObjectReference>("import", FocusModulePath).ConfigureAwait(true);
            await module.InvokeVoidAsync("focusValidationTarget", SummaryId, inputId).ConfigureAwait(true);
        }
        catch (JSDisconnectedException) {
        }
        catch (JSException) {
            // Scripted focus failed: let the next activation use the native fragment href.
            RestoreNativeLinkFallback();
        }
        catch (InvalidOperationException) {
            RestoreNativeLinkFallback();
        }
        finally {
            if (module is not null) {
                await DisposeModuleAsync(module).ConfigureAwait(true);
            }
        }
    }

    private void RestoreNativeLinkFallback() {
        if (_scriptFocusAvailable && _disposed == 0) {
            _scriptFocusAvailable = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Builds a same-page fragment link: Blazor's base href would otherwise resolve a bare
    /// <c>#id</c> against the application root and navigate away from the form.
    /// </summary>
    private string FragmentHref(string inputId) {
        string fragment = "#" + Uri.EscapeDataString(inputId);
        try {
            string relative = Navigation.ToBaseRelativePath(Navigation.Uri);
            int existingFragment = relative.IndexOf('#', StringComparison.Ordinal);
            return (existingFragment >= 0 ? relative[..existingFragment] : relative) + fragment;
        }
        catch (InvalidOperationException) {
            return fragment;
        }
        catch (ArgumentException) {
            return fragment;
        }
    }

    /// <summary>
    /// Story 13.3 BH3-17 — a link names its field once: default DataAnnotations messages already
    /// contain the label, so the "Label: " prefix is added only when the message lacks it.
    /// BH4-03 — the label must appear as a whole word: "Id" inside "Invalid" does not name the field.
    /// </summary>
    private static string LinkText(string message, string? label)
        => string.IsNullOrWhiteSpace(label) || ContainsWholeWord(message, label)
            ? message
            : $"{label}: {message}";

    private static bool ContainsWholeWord(string message, string label) {
        System.Globalization.CompareInfo compare = System.Globalization.CultureInfo.CurrentCulture.CompareInfo;
        int offset = 0;
        while (offset < message.Length) {
            int index = compare.IndexOf(
                message.AsSpan(offset),
                label.AsSpan(),
                System.Globalization.CompareOptions.IgnoreCase,
                out int matchLength);
            if (index < 0) {
                return false;
            }

            int start = offset + index;
            int end = start + matchLength;
            if ((start == 0 || !char.IsLetterOrDigit(message[start - 1]))
                && (end >= message.Length || !char.IsLetterOrDigit(message[end]))) {
                return true;
            }

            offset = start + 1;
        }

        return false;
    }

    private static async ValueTask DisposeModuleAsync(IJSObjectReference module) {
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

    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e) {
        if (!_visible || _disposed != 0) {
            return;
        }

        _ = InvokeAsync(() => {
            RebuildEntries();
            StateHasChanged();
        });
    }

    private void RebuildEntries() {
        _entries.Clear();
        if (EditContext is null) {
            return;
        }

        List<string> remaining = [.. EditContext.GetValidationMessages()];
        foreach (FcValidationFieldDescriptor field in Fields) {
            FieldIdentifier identifier = new(EditContext.Model, field.FieldName);
            foreach (string message in EditContext.GetValidationMessages(identifier)) {
                _entries.Add((message, field.InputId, field.Label));
                _ = remaining.Remove(message);
            }
        }

        foreach (string message in remaining) {
            _entries.Add((message, null, null));
        }

        foreach (string message in FormLevelErrors) {
            if (!_entries.Any(entry => entry.InputId is null && string.Equals(entry.Message, message, StringComparison.Ordinal))) {
                _entries.Add((message, null, null));
            }
        }
    }

    private string Resolve(string key, string fallback) {
        LocalizedString value = Localizer[key];
        return value.ResourceNotFound || string.IsNullOrWhiteSpace(value.Value) ? fallback : value.Value;
    }

    private void Unsubscribe() {
        if (_subscribedEditContext is not null) {
            _subscribedEditContext.OnValidationStateChanged -= OnValidationStateChanged;
            _subscribedEditContext = null;
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) {
            return;
        }

        Unsubscribe();
        GC.SuppressFinalize(this);
    }
}
