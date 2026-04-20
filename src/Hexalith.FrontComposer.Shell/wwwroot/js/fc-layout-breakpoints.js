// Story 3-2 Task 4.1 (D5 / D6 / ADR-036) — ES module composing three matchMedia queries into
// a single ViewportTier integer. Mirrors fc-prefers-color-scheme.js shape (subscribe returns
// { unsubscribe }, one-shot emission on subscribe). Dedupes composed-tier no-ops per D6.

const QUERIES = {
    desktop: '(min-width: 1366px)',
    compact: '(min-width: 1024px)',
    tablet: '(min-width: 768px)',
};

function computeTier(desktop, compact, tablet) {
    if (desktop) return 3; // Desktop
    if (compact) return 2; // CompactDesktop
    if (tablet) return 1;  // Tablet
    return 0;              // Phone
}

/**
 * Subscribe to viewport-tier changes derived from three matchMedia queries.
 * @param {any} dotnetRef - DotNetObjectReference exposing OnViewportTierChangedAsync(int).
 * @returns {{ unsubscribe: () => void }}
 */
export function subscribe(dotnetRef) {
    if (!dotnetRef) {
        throw new Error('fc-layout-breakpoints.subscribe: dotnetRef is required.');
    }

    const mqDesktop = window.matchMedia(QUERIES.desktop);
    const mqCompact = window.matchMedia(QUERIES.compact);
    const mqTablet = window.matchMedia(QUERIES.tablet);

    let lastTier = computeTier(mqDesktop.matches, mqCompact.matches, mqTablet.matches);
    // Emit the initial value once so C# does not need a separate bootstrap round-trip.
    // .catch swallows the rejection Blazor throws when the circuit tears down mid-call —
    // mirrors the defensive C# side that swallows JSDisconnectedException / OperationCanceledException.
    dotnetRef.invokeMethodAsync('OnViewportTierChangedAsync', lastTier).catch(() => {});

    const handler = () => {
        const tier = computeTier(mqDesktop.matches, mqCompact.matches, mqTablet.matches);
        if (tier === lastTier) {
            return; // D6 — dedupe composed-tier no-ops across the three queries.
        }
        lastTier = tier;
        dotnetRef.invokeMethodAsync('OnViewportTierChangedAsync', tier).catch(() => {});
    };

    mqDesktop.addEventListener('change', handler);
    mqCompact.addEventListener('change', handler);
    mqTablet.addEventListener('change', handler);

    return {
        unsubscribe() {
            mqDesktop.removeEventListener('change', handler);
            mqCompact.removeEventListener('change', handler);
            mqTablet.removeEventListener('change', handler);
        },
    };
}

/**
 * Remove the subscription registered via {@link subscribe}.
 * @param {{ unsubscribe: () => void } | null | undefined} subscription
 */
export function unsubscribe(subscription) {
    subscription?.unsubscribe();
}
