using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Story 2-2 Decision D37 — enforces the "at most one Inline popover open at a time per circuit"
/// invariant. When a renderer opens its popover, the registry asks any currently-open popover to close.
/// Registered as <c>Scoped</c> so each Blazor circuit gets its own instance.
/// </summary>
public sealed class InlinePopoverRegistry {
    private readonly object _gate = new();
    // Blazor owns the renderer lifetime, including removal while a popover is open.
    private WeakReference<IInlinePopover>? _currentlyOpen;

    /// <summary>
    /// Records <paramref name="popover"/> as the newly-open popover after closing any previously
    /// open one.
    /// </summary>
    public async Task OpenAsync(IInlinePopover popover) {
        ArgumentNullException.ThrowIfNull(popover);

        IInlinePopover? previous = null;
        lock (_gate) {
            _ = _currentlyOpen?.TryGetTarget(out previous);
            _currentlyOpen = new WeakReference<IInlinePopover>(popover);
        }

        if (previous is not null && !ReferenceEquals(previous, popover)) {
            try {
                await previous.ClosePopoverAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception) {
                // Best-effort close — never block the new popover from opening on a stale handle.
                // Sanitized observability for stale-close failures is tracked separately.
            }
        }
    }

    /// <summary>Dismisses the current reconnectable popover, if any, for circuit recovery.</summary>
    /// <returns>Whether an open popover was dismissed.</returns>
    internal async Task<bool> DismissOnReconnectAsync() {
        WeakReference<IInlinePopover>? registration;
        IReconnectableInlinePopover popover;
        lock (_gate) {
            registration = _currentlyOpen;
            if (registration?.TryGetTarget(out IInlinePopover? current) != true
                || current is not IReconnectableInlinePopover reconnectable) {
                return false;
            }

            popover = reconnectable;
        }

        bool dismissed = await popover.DismissOnReconnectAsync().ConfigureAwait(false);
        lock (_gate) {
            if (ReferenceEquals(_currentlyOpen, registration)) {
                _currentlyOpen = null;
            }
        }

        return dismissed;
    }

    /// <summary>Marks <paramref name="popover"/> as no longer the currently-open popover.</summary>
    public void Released(IInlinePopover popover) {
        lock (_gate) {
            if (_currentlyOpen?.TryGetTarget(out IInlinePopover? current) == true && ReferenceEquals(current, popover)) {
                _currentlyOpen = null;
            }
        }
    }
}
