// Story 2-2 Task 7 — expand-in-row scroll stabilization for CompactInline command renderers.
// Contract: `initializeExpandInRow(elementRef)` scrolls the element into view and, when the
// user has NOT requested reduced motion, applies a follow-up requestAnimationFrame measurement
// to correct for late-arriving layout shifts. Module-scoped import (./_content/Hexalith.FrontComposer.Shell/js/fc-expandinrow.js)
// avoids global namespace pollution (Decision D11). Module is loaded lazily via
// `IExpandInRowJSModule` (Decision D25) and disposed on circuit teardown.

/** Focus + scroll-nearest for inline popover close (Story 2-2 — replaces host `eval` interop). */
export function focusTriggerElementById(elementId) {
    if (typeof document === 'undefined' || !elementId) {
        return;
    }
    const el = document.getElementById(elementId);
    if (el) {
        el.scrollIntoView({ block: 'nearest' });
        el.focus();
    }
}

// Follow-up scroll is only applied when the element is this many pixels past a viewport edge —
// avoids fighting a smooth scrollIntoView that's still in flight.
const SCROLL_CORRECTION_THRESHOLD_PX = 4;

export function initializeExpandInRow(elementRef) {
    if (!(elementRef instanceof Element) || !elementRef.isConnected) {
        return;
    }

    const reduceMotion = typeof window !== 'undefined'
        && typeof window.matchMedia === 'function'
        && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    elementRef.scrollIntoView({
        block: 'nearest',
        behavior: reduceMotion ? 'auto' : 'smooth',
    });

    if (reduceMotion || typeof window === 'undefined') {
        return;
    }

    requestAnimationFrame(() => {
        if (!elementRef.isConnected) {
            return;
        }
        const rect = elementRef.getBoundingClientRect();
        const viewportHeight = window.innerHeight || document.documentElement.clientHeight;
        let delta = 0;
        if (rect.top < -SCROLL_CORRECTION_THRESHOLD_PX) {
            delta = rect.top;
        }
        else if (rect.bottom > viewportHeight + SCROLL_CORRECTION_THRESHOLD_PX) {
            delta = rect.bottom - viewportHeight;
        }
        if (delta !== 0) {
            window.scrollBy({ top: delta, behavior: 'smooth' });
        }
    });
}
