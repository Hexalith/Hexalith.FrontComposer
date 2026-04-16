namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Story 2-2 Decision D37 — enforces the "at most one Inline popover open at a time per circuit"
/// invariant. When a renderer opens its popover, the registry asks any currently-open popover to close.
/// Registered as <c>Scoped</c> so each Blazor circuit gets its own instance.
/// </summary>
public sealed class InlinePopoverRegistry {
    private readonly object _gate = new();
    private IInlinePopover? _currentlyOpen;

    /// <summary>
    /// Records <paramref name="popover"/> as the newly-open popover after closing any previously
    /// open one.
    /// </summary>
    public async Task OpenAsync(IInlinePopover popover) {
        if (popover is null) {
            throw new ArgumentNullException(nameof(popover));
        }

        IInlinePopover? previous;
        lock (_gate) {
            previous = _currentlyOpen;
            _currentlyOpen = popover;
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
                // Telemetry hook deferred to the Shell wrapper that injects ILogger (Group C).
            }
        }
    }

    /// <summary>Marks <paramref name="popover"/> as no longer the currently-open popover.</summary>
    public void Released(IInlinePopover popover) {
        lock (_gate) {
            if (ReferenceEquals(_currentlyOpen, popover)) {
                _currentlyOpen = null;
            }
        }
    }
}
