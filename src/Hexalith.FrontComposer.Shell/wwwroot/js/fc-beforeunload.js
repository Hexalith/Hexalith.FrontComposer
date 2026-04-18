// Story 3-1 Task 3.1 (D17) — ES module registering the window.beforeunload handler that
// invokes IStorageService.FlushAsync via a DotNetObjectReference. Import once per circuit
// from FrontComposerShell.OnAfterRenderAsync(firstRender: true); unregister on Dispose so
// the DotNetObjectReference does not leak across circuit teardowns.

const FLUSH_BUDGET_MS = 200;

/**
 * Attach the beforeunload flush handler and return a subscription handle the caller can
 * pass back to {@link unregister} on Dispose.
 * @param {any} dotnetRef - DotNetObjectReference exposing an invocable FlushAsync method.
 * @returns {{ unsubscribe: () => void }}
 */
export function register(dotnetRef) {
    if (!dotnetRef) {
        throw new Error('fc-beforeunload.register: dotnetRef is required.');
    }

    const handler = () => {
        // Race the flush against a short budget — browsers typically cut background promises
        // shortly after beforeunload returns, so we yield control quickly rather than block
        // the unload ceremony.
        const flush = dotnetRef.invokeMethodAsync('FlushAsync');
        const timeout = new Promise(resolve => setTimeout(resolve, FLUSH_BUDGET_MS));
        return Promise.race([flush, timeout]);
    };

    window.addEventListener('beforeunload', handler, { capture: true });

    return {
        unsubscribe() {
            window.removeEventListener('beforeunload', handler, { capture: true });
        },
    };
}

/**
 * Remove the beforeunload flush handler registered via {@link register}.
 * @param {{ unsubscribe: () => void } | null | undefined} subscription
 */
export function unregister(subscription) {
    subscription?.unsubscribe();
}
