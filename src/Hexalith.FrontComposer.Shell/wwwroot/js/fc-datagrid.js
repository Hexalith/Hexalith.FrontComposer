// Story 4-4 T1.5 / D4 re-revised — setTimeout+timestamp throttle (~30 Hz cap) with
// guaranteed trailing-edge dispatch, scroll-to-offset restore, and per-viewKey dispose.
//
// The JS side caps raw @onscroll → HandleScrollAsync dispatch rate so Fluxor subscribers
// don't re-compute 60×/s during active scroll bursts. The 150 ms semantic debounce lives
// in ScrollPersistenceEffect on the .NET side.

const THROTTLE_INTERVAL_MS = 33; // ≈ 30 Hz
const perViewKey = new Map();

export function captureScrollThrottled(viewKey, scrollTop, dotnetRef) {
    if (!viewKey) {
        return;
    }

    const actualScrollTop = readScrollTop(viewKey, scrollTop);
    const now = performance.now();
    let entry = perViewKey.get(viewKey);
    if (!entry) {
        entry = { lastTs: 0, timeoutId: null, pendingScrollTop: 0, dotnetRef };
        perViewKey.set(viewKey, entry);
    }

    // Always update pending to the latest value; store the dotnetRef so trailing-edge
    // dispatch uses the freshest reference in case the component re-mounts mid-scroll.
    entry.pendingScrollTop = actualScrollTop;
    entry.dotnetRef = dotnetRef;

    const elapsed = now - entry.lastTs;

    if (elapsed >= THROTTLE_INTERVAL_MS && entry.timeoutId === null) {
        // Fire immediately.
        entry.lastTs = now;
        invokeHandleScroll(entry.dotnetRef, viewKey, actualScrollTop);
        return;
    }

    if (entry.timeoutId !== null) {
        // A trailing-edge timeout is already pending; it will pick up pendingScrollTop.
        return;
    }

    const remaining = Math.max(0, THROTTLE_INTERVAL_MS - elapsed);
    entry.timeoutId = setTimeout(() => {
        const current = perViewKey.get(viewKey);
        if (!current) {
            return;
        }
        current.lastTs = performance.now();
        current.timeoutId = null;
        invokeHandleScroll(current.dotnetRef, viewKey, current.pendingScrollTop);
    }, remaining);
}

export function scrollToOffset(viewKey, scrollTop) {
    if (!viewKey) {
        return;
    }

    // Fluent DataGrid internally renders a scrollable div; the outer container carries
    // data-fc-datagrid. Try the scroll container first; if the Fluent internal selector
    // shifted (minor-version drift) fall back to the container itself.
    const container = findContainer(viewKey);
    if (!container) {
        console.warn(
            `fc-datagrid: container selector missed for viewKey=${viewKey}. ` +
                'Verify [data-fc-datagrid] attribute presence.',
        );
        return;
    }

    const scroller = findScroller(container);
    if (!scroller) {
        console.warn(
            `fc-datagrid: scroll container selector missed for viewKey=${viewKey}. ` +
                'Fluent UI version may have changed; verify .fluent-data-grid-scroll-container selector.',
        );
        return;
    }

    scroller.scrollTop = scrollTop;
}

export function disposeViewKey(viewKey) {
    if (!viewKey) {
        return;
    }

    const entry = perViewKey.get(viewKey);
    if (!entry) {
        return;
    }

    if (entry.timeoutId !== null) {
        clearTimeout(entry.timeoutId);
    }
    perViewKey.delete(viewKey);
}

function invokeHandleScroll(dotnetRef, viewKey, scrollTop) {
    if (!dotnetRef) {
        return;
    }

    try {
        dotnetRef.invokeMethodAsync('HandleScrollAsync', viewKey, scrollTop).catch(() => {});
    } catch (err) {
        // Circuit tearing down or component disposed — ignore.
    }
}

function readScrollTop(viewKey, fallback) {
    const container = findContainer(viewKey);
    if (!container) {
        return fallback;
    }

    return findScroller(container)?.scrollTop ?? fallback;
}

function findContainer(viewKey) {
    return document.querySelector(`[data-fc-datagrid="${escapeSelector(viewKey)}"]`);
}

function findScroller(container) {
    return container.querySelector('.fluent-data-grid-scroll-container') || container;
}

function escapeSelector(value) {
    if (typeof CSS !== 'undefined' && typeof CSS.escape === 'function') {
        return CSS.escape(value);
    }
    return String(value).replace(/["\\]/g, '\\$&');
}
