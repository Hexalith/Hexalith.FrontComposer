// Story 3-1 Task 3.2 (D23) — ES module subscribing to the prefers-color-scheme media query
// and reporting changes (and the initial value) to FcSystemThemeWatcher via a
// DotNetObjectReference. The initial emission at subscription time lets Fluxor state == System
// respond to the OS preference on first page load without requiring a change event.

const QUERY = '(prefers-color-scheme: dark)';

/**
 * Subscribe to OS dark/light preference changes.
 * @param {any} dotnetRef - DotNetObjectReference exposing OnSystemThemeChangedAsync(bool).
 * @returns {{ unsubscribe: () => void }}
 */
export function subscribe(dotnetRef) {
    if (!dotnetRef) {
        throw new Error('fc-prefers-color-scheme.subscribe: dotnetRef is required.');
    }

    const mediaQuery = window.matchMedia(QUERY);

    const handler = event => {
        dotnetRef.invokeMethodAsync('OnSystemThemeChangedAsync', event.matches);
    };

    // Emit the current value once so callers do not need a separate bootstrap round-trip.
    dotnetRef.invokeMethodAsync('OnSystemThemeChangedAsync', mediaQuery.matches);

    mediaQuery.addEventListener('change', handler);

    return {
        unsubscribe() {
            mediaQuery.removeEventListener('change', handler);
        },
    };
}

/**
 * Remove the prefers-color-scheme subscription registered via {@link subscribe}.
 * @param {{ unsubscribe: () => void } | null | undefined} subscription
 */
export function unsubscribe(subscription) {
    subscription?.unsubscribe();
}
