// Story 4-4 T1.5 / D4 re-revised — setTimeout+timestamp throttle (~30 Hz cap) with
// guaranteed trailing-edge dispatch, scroll-to-offset restore, and per-instance dispose.
//
// The JS side caps raw @onscroll → HandleScrollAsync dispatch rate so Fluxor subscribers
// don't re-compute 60×/s during active scroll bursts. The 150 ms semantic debounce lives
// in ScrollPersistenceEffect on the .NET side.

const THROTTLE_INTERVAL_MS = 33; // ≈ 30 Hz
const perInstance = new Map();
const rowMetrics = new Map();

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
    const metrics = rowMetrics.get(key);
    if (metrics) { metrics.resize.disconnect(); metrics.mutations.disconnect(); metrics.focus.abort(); cancelAnimationFrame(metrics.frame); rowMetrics.delete(key); }
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

// Cells retain their complete value inside the labelled two-dimensional scrollport.
// Text spacing or stacked content may increase their natural extent. Apply one measured height to every virtual row and
// remount Virtualize with that same ItemSize, rather than silently drifting its spacer geometry.
export function observeRowMetrics(viewKey, instanceId, dotnetRef, defaultHeight) {
    const container = findContainer(viewKey, instanceId);
    if (!container) return;
    const previous = rowMetrics.get(instanceId);
    if (previous?.container === container && previous.defaultHeight === defaultHeight) { previous.schedule(); return; }
    if (previous) { previous.resize.disconnect(); previous.mutations.disconnect(); previous.focus.abort(); cancelAnimationFrame(previous.frame); }
    const state = { container, defaultHeight, frame: 0, height: null, pendingFocus: null, focus: new AbortController() };
    const recoverFocus = () => {
        const prior = state.pendingFocus;
        if (!prior || prior.element.isConnected) return;
        state.pendingFocus = null;
        if (!state.focus.signal.aborted && container.isConnected && prior.path === location.href
            && (!document.activeElement || document.activeElement === document.body)) container.focus({ preventScroll: true });
    };
    document.addEventListener('focusin', event => {
        if (state.pendingFocus && event.target !== state.pendingFocus.element) state.pendingFocus = null;
    }, { capture: true, signal: state.focus.signal });
    for (const event of ['pointerdown', 'keydown']) document.addEventListener(event, () => { state.pendingFocus = null; }, { capture: true, signal: state.focus.signal });
    container.addEventListener('focusin', event => {
        if (event.target instanceof HTMLElement) {
            event.target.scrollIntoView({ block: 'nearest', inline: 'nearest', behavior: 'instant' });
        }
    }, { signal: state.focus.signal });
    const measure = () => {
        state.frame = 0;
        if (!container.isConnected) return;
        const cells = container.querySelectorAll('tbody tr.fluent-data-grid-row > td');
        // Virtualize remounts briefly contain no materialized rows. Keep the last measurement
        // through that interval instead of resetting to the floor and remounting forever.
        if (cells.length === 0) return;
        recoverFocus();
        let height = defaultHeight;
        // Read the complete natural row, including stacked blocks and line breaks. Remove only
        // imposed row/cell heights synchronously; restore their exact styles before painting.
        const nodes = [...container.querySelectorAll('tbody tr.fluent-data-grid-row'), ...cells];
        const saved = new Map(nodes.map(node => [node, node.getAttribute('style')]));
        state.mutations.takeRecords(); // The current measurement includes all preceding external changes.
        try {
            for (const node of nodes) {
                node.style.setProperty('height', 'auto', 'important');
                node.style.setProperty('min-height', '0', 'important');
            }
            for (const row of container.querySelectorAll('tbody tr.fluent-data-grid-row')) height = Math.max(height, Math.ceil(row.getBoundingClientRect().height));
        } finally {
            for (const [node, style] of saved) {
                if (style === null) node.removeAttribute('style'); else node.setAttribute('style', style);
            }
            // Drain only this synchronous probe's style records. Unexpected external records still
            // schedule a follow-up; the observer otherwise remains connected throughout the probe.
            if (state.mutations.takeRecords().some(record => record.type !== 'attributes' || record.attributeName !== 'style' || !saved.has(record.target))) state.schedule();
        }
        if (container.style.getPropertyValue('--fc-grid-row-height') !== `${height}px`) container.style.setProperty('--fc-grid-row-height', `${height}px`);
        if (height !== state.height) {
            state.height = height;
            const active = document.activeElement;
            if (active instanceof HTMLElement && active !== container && container.contains(active)) state.pendingFocus = { element: active, path: location.href };
            const ownedFocus = state.pendingFocus;
            dotnetRef.invokeMethodAsync('HandleRowMetricsAsync', instanceId, height, defaultHeight).catch(() => {}).finally(() => requestAnimationFrame(() => {
                if (state.pendingFocus === ownedFocus) { recoverFocus(); state.pendingFocus = null; }
            }));
        }
    };
    state.schedule = () => { if (!state.frame) state.frame = requestAnimationFrame(measure); };
    state.resize = new ResizeObserver(state.schedule);
    state.resize.observe(container);
    state.mutations = new MutationObserver(state.schedule);
    // Styles may be supplied on an ancestor or by a user stylesheet, including shadow-root rules.
    state.mutations.observe(document.documentElement, { childList: true, subtree: true, attributes: true, attributeFilter: ['style', 'class'] });
    rowMetrics.set(instanceId, state);
    state.schedule();
}
