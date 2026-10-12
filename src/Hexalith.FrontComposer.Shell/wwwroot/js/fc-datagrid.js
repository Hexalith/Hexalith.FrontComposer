// Story 4-4 T1.5 / D4 re-revised — setTimeout+timestamp throttle (~30 Hz cap) with
// guaranteed trailing-edge dispatch, scroll-to-offset restore, and per-instance dispose.
//
// The JS side caps raw @onscroll → HandleScrollAsync dispatch rate so Fluxor subscribers
// don't re-compute 60×/s during active scroll bursts. The 150 ms semantic debounce lives
// in ScrollPersistenceEffect on the .NET side.

const THROTTLE_INTERVAL_MS = 33; // ≈ 30 Hz
const perInstance = new Map();

export function captureScrollThrottled(viewKey, scrollTop, dotnetRef, generation = 0, instanceId = '') {
    if (!viewKey) {
        return;
    }

    const key = instanceId || viewKey;
    const actualScrollTop = readScrollTop(viewKey, scrollTop, instanceId);
    const now = performance.now();
    let entry = perInstance.get(key);
    if (entry && entry.generation > generation) {
        return;
    }
    if (entry && entry.generation < generation) {
        if (entry.timeoutId !== null) { clearTimeout(entry.timeoutId); }
        entry = null;
    }
    if (!entry) {
        entry = { lastTs: 0, timeoutId: null, pendingScrollTop: 0, dotnetRef, generation };
        perInstance.set(key, entry);
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
        const current = perInstance.get(key);
        if (current !== entry) {
            return;
        }
        current.lastTs = performance.now();
        current.timeoutId = null;
        invokeHandleScroll(current.dotnetRef, viewKey, current.pendingScrollTop);
    }, remaining);
}

export function scrollToOffset(viewKey, scrollTop, instanceId = '') {
    if (!viewKey) {
        return;
    }

    // Fluent DataGrid internally renders a scrollable div; the outer container carries
    // data-fc-datagrid. Try the scroll container first; if the Fluent internal selector
    // shifted (minor-version drift) fall back to the container itself.
    const container = findContainer(viewKey, instanceId);
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

export function disposeViewKey(viewKey, generation, instanceId = '') {
    if (!viewKey) {
        return;
    }

    const key = instanceId || viewKey;
    const entry = perInstance.get(key);
    if (!entry) {
        return;
    }

    if (generation !== undefined && entry.generation !== undefined && entry.generation > generation) {
        return;
    }

    if (entry.timeoutId !== null) {
        clearTimeout(entry.timeoutId);
    }
    perInstance.delete(key);
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

function readScrollTop(viewKey, fallback, instanceId) {
    const container = findContainer(viewKey, instanceId);
    if (!container) {
        return fallback;
    }

    return findScroller(container)?.scrollTop ?? fallback;
}

function findContainer(viewKey, instanceId) {
    const selector = `[data-fc-datagrid="${escapeSelector(viewKey)}"]`;
    if (instanceId) {
        return [...document.querySelectorAll(selector)].find(container =>
            container.getAttribute('data-fc-datagrid-instance') === instanceId);
    }
    return document.querySelector(selector);
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
