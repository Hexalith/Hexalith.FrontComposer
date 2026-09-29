using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Components.Forms;

/// <summary>
/// Story 13.3 VR-05 / FM-09 / AM-20 — presents a blocked later submit through exactly one polite,
/// atomic status node that stays mounted for the lifetime of the command surface.
/// </summary>
/// <remarks>
/// A blocked attempt keeps focus on the attempted control, never dispatches, and adds no
/// validation error. When the attempt came from inside the owning form (for example Enter in a
/// field), focus stays where it is; otherwise it returns to <see cref="AttemptedControlId"/>. Each
/// attempt clears the status text, waits <see cref="ReannounceDelay"/>, and sets the exact AM-20 copy
/// again, so every blocked attempt — including an identical repeat — is announced once. Attempts
/// are serialized, so a rapid repeat still gets its own clear-and-set. "View active command" is
/// offered only when a rendered active lifecycle heading exists at the time the attempt is
/// announced. The action is not withdrawn automatically when that heading later disappears: it is
/// withdrawn, with focus returned to the attempted control, only when the operator activates it
/// and no rendered active lifecycle heading can take focus.
/// </remarks>
public partial class FcCommandBlockedOutcome : ComponentBase, IDisposable {
    /// <summary>The pause between clearing and re-setting the status text for one attempt.</summary>
    public static readonly TimeSpan ReannounceDelay = TimeSpan.FromMilliseconds(100);

    private const string FocusModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js";
    private string _message = string.Empty;
    private bool _blocked;
    private bool _viewActionAvailable;
    private int _generation;
    private int _disposed;
    private Task _presentation = Task.CompletedTask;

    /// <summary>Gets or sets the DOM id of the control whose activation was blocked.</summary>
    [Parameter]
    [EditorRequired]
    public string AttemptedControlId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the outcome renders inline, beside a trigger in the
    /// text flow (a zero-field inline command), instead of as its own row under a form.
    /// </summary>
    [Parameter]
    public bool Inline { get; set; }

    /// <summary>Gets a value indicating whether a blocked outcome is currently presented.</summary>
    public bool IsBlocked => _blocked;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    [Inject]
    private TimeProvider Time { get; set; } = default!;

    private string BlockedMessage => Resolve("CommandBlockedMessage", "This command did not run. Another command is already in progress.");

    private string ViewActiveCommandLabel => Resolve("ViewActiveCommand", "View active command");

    private string BlockedAttribute => _blocked ? "true" : "false";

    /// <summary>
    /// Presents one blocked attempt: keeps focus on the attempted control and announces AM-20 once.
    /// </summary>
    /// <returns>A task that completes when this attempt's announcement has been set.</returns>
    public Task PresentAsync()
        => InvokeAsync(() => {
            if (_disposed != 0) {
                return Task.CompletedTask;
            }

            _presentation = PresentAfterAsync(_presentation, _generation);
            return _presentation;
        });

    /// <summary>Withdraws the blocked outcome, for example when a later attempt is admitted.</summary>
    /// <returns>A task that completes after the outcome is cleared on the renderer dispatcher.</returns>
    public Task ClearAsync()
        => InvokeAsync(() => {
            _generation++;
            if (_blocked || _message.Length != 0) {
                _blocked = false;
                _viewActionAvailable = false;
                _message = string.Empty;
                StateHasChanged();
            }
        });

    /// <inheritdoc />
    public void Dispose() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) {
            return;
        }

        _generation++;
        GC.SuppressFinalize(this);
    }

    private async Task PresentAfterAsync(Task previous, int generation) {
        // Serialize attempts so each one gets its own clear-then-set cycle.
        await previous.ConfigureAwait(true);
        if (!IsCurrent(generation)) {
            return;
        }

        // Clear first: re-setting identical text into a live region is not announced again.
        _blocked = true;
        _viewActionAvailable = false;
        _message = string.Empty;
        StateHasChanged();
        await FocusAttemptedControlAsync(force: false).ConfigureAwait(true);

        await Task.Delay(ReannounceDelay, Time).ConfigureAwait(true);
        if (!IsCurrent(generation)) {
            return;
        }

        bool viewActionAvailable = await InvokeFocusModuleAsync("hasActiveLifecycle").ConfigureAwait(true);
        if (!IsCurrent(generation)) {
            return;
        }

        _message = BlockedMessage;
        _viewActionAvailable = viewActionAvailable;
        StateHasChanged();
    }

    private async Task FocusActiveCommandAsync() {
        if (await InvokeFocusModuleAsync("focusActiveLifecycle").ConfigureAwait(true)) {
            return;
        }

        // The active lifecycle is gone or not rendered: withdraw the action and keep the operator on
        // the attempted control rather than on a removed button.
        _viewActionAvailable = false;
        StateHasChanged();
        await FocusAttemptedControlAsync(force: true).ConfigureAwait(true);
    }

    private async Task FocusAttemptedControlAsync(bool force) {
        if (string.IsNullOrWhiteSpace(AttemptedControlId)) {
            return;
        }

        _ = await InvokeFocusModuleAsync("focusAttemptedControl", AttemptedControlId, force).ConfigureAwait(true);
    }

    private async Task<bool> InvokeFocusModuleAsync(string identifier, params object?[] args) {
        IJSObjectReference? module = null;
        try {
            module = await JS.InvokeAsync<IJSObjectReference>("import", FocusModulePath).ConfigureAwait(true);
            return await module.InvokeAsync<bool>(identifier, args).ConfigureAwait(true);
        }
        catch (JSDisconnectedException) {
            return false;
        }
        catch (JSException) {
            return false;
        }
        catch (InvalidOperationException) {
            // Prerender has no interactive JS runtime.
            return false;
        }
        catch (TaskCanceledException) {
            // A stalled interop call must not fault the serialized presentation chain.
            return false;
        }
        finally {
            if (module is not null) {
                await DisposeModuleAsync(module).ConfigureAwait(true);
            }
        }
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
        catch (TaskCanceledException) {
            // A stalled dispose must not fault the serialized presentation chain either.
        }
    }

    private bool IsCurrent(int generation) => _disposed == 0 && generation == _generation;

    private string Resolve(string key, string fallback) {
        LocalizedString value = Localizer[key];
        return value.ResourceNotFound || string.IsNullOrWhiteSpace(value.Value) ? fallback : value.Value;
    }
}
