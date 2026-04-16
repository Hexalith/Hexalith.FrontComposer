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

export function initializeExpandInRow(elementRef) {
    if (!elementRef) {
        return;
    }

    const reduceMotion = typeof window !== 'undefined'
        && typeof window.matchMedia === 'function'
        && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    elementRef.scrollIntoView({
        block: 'nearest',
        behavior: reduceMotion ? 'auto' : 'smooth',
    });

    if (!reduceMotion) {
        requestAnimationFrame(() => {
            const rect = elementRef.getBoundingClientRect();
            if (rect.top < 0) {
                window.scrollBy({ top: rect.top, behavior: 'smooth' });
            }
        });
    }
}

// Reserved for v2 multi-expand support; no-op today (Decision D11 forward-compat stub).
export function collapseExpandInRow(_elementRef) {
    // Intentionally empty — v1 is single-expand per DataGrid (Story 4.5 enforces).
}
