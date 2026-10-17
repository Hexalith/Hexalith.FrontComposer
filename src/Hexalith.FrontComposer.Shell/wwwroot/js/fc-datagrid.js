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
    if (metrics) { metrics.dispose(); rowMetrics.delete(key); }
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
    if (previous?.container === container && previous.defaultHeight === defaultHeight) { previous.dotnetRef = dotnetRef; previous.retries = 0; previous.attempts = 0; previous.schedule(); return; }
    if (previous) previous.dispose();
    const state = { container, defaultHeight, dotnetRef, frame: 0, recoveryFrame: 0, retryTimer: 0, retries: 0, attempts: 0, acknowledged: null, desired: null, inFlight: false, pendingFocus: null, focus: new AbortController() };
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
        if (!container.isConnected || state.focus.signal.aborted) return;
        observeContent();
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
        // Every grid observer stays connected. Preserve earlier external records,
        // then discard this synchronous probe's temporary row/cell style records
        // from all observers so grids cannot repeatedly wake each other.
        for (const metrics of rowMetrics.values()) metrics.process(metrics.mutations.takeRecords());
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
            for (const metrics of rowMetrics.values()) metrics.process(metrics.mutations.takeRecords().filter(record => record.type !== 'attributes' || record.attributeName !== 'style' || !saved.has(record.target)));
        }
        if (container.style.getPropertyValue('--fc-grid-row-height') !== `${height}px`) container.style.setProperty('--fc-grid-row-height', `${height}px`);
        if (height !== state.desired) { state.desired = height; state.retries = 0; state.attempts = 0; clearTimeout(state.retryTimer); state.retryTimer = 0; }
        deliver();
    };
    const deliver = () => {
        if (state.focus.signal.aborted || !container.isConnected || state.inFlight || state.retryTimer || state.attempts >= 3 || state.desired === state.acknowledged || state.desired === null) return;
        const height = state.desired;
        state.inFlight = true;
        state.attempts++;
        const active = document.activeElement;
        if (active instanceof HTMLElement && active !== container && container.contains(active)) state.pendingFocus = { element: active, path: location.href };
        const ownedFocus = state.pendingFocus;
        Promise.resolve().then(() => state.dotnetRef.invokeMethodAsync('HandleRowMetricsAsync', instanceId, height, defaultHeight)).then(() => {
            if (state.focus.signal.aborted) return;
            state.acknowledged = height;
            state.retries = 0;
            state.recoveryFrame = requestAnimationFrame(() => {
                state.recoveryFrame = 0;
                if (!state.focus.signal.aborted && state.pendingFocus === ownedFocus) { recoverFocus(); state.pendingFocus = null; }
            });
        }, () => {
            if (!state.focus.signal.aborted && state.desired === height && state.retries < 2) {
                state.retryTimer = setTimeout(() => { state.retryTimer = 0; deliver(); }, 50 * ++state.retries);
            }
        }).finally(() => {
            state.inFlight = false;
            if (!state.focus.signal.aborted && state.desired !== height) state.schedule();
        });
    };
    state.schedule = () => { if (!state.focus.signal.aborted && !state.frame) state.frame = requestAnimationFrame(measure); };
    const composedParent = node => node.parentElement ?? (node.getRootNode() instanceof ShadowRoot ? node.getRootNode().host : null);
    const contains = (owner, node) => { for (let current = node; current; current = composedParent(current)) if (current === owner) return true; return false; };
    state.process = records => {
        if (records.some(record => contains(container, record.target) || contains(record.target, container)
            || (record.target instanceof Element ? record.target : record.target.parentElement)?.closest('style,link[rel="stylesheet"]')
            || [...record.addedNodes, ...record.removedNodes].some(node => node instanceof Element && (node.matches('style,link[rel="stylesheet"]') || node.querySelector('style,link[rel="stylesheet"]'))))) state.schedule();
    };
    state.resize = new ResizeObserver(state.schedule);
    state.mutations = new MutationObserver(state.process);
    const observed = new WeakSet();
    const observeContent = () => {
        const visit = root => {
            if (!observed.has(root)) { observed.add(root); state.mutations.observe(root, { childList: true, subtree: true, characterData: true, attributes: true, attributeFilter: ['style', 'class'] }); }
            for (const node of root.querySelectorAll('*')) {
                if (root !== document.documentElement && !observed.has(node)) { observed.add(node); state.resize.observe(node); }
                if (node.shadowRoot && (contains(container, node) || contains(node, container))) visit(node.shadowRoot);
            }
        };
        visit(document.documentElement);
        visit(container);
    };
    state.resize.observe(container);
    state.dispose = () => {
        state.focus.abort(); state.resize.disconnect(); state.mutations.disconnect();
        cancelAnimationFrame(state.frame); cancelAnimationFrame(state.recoveryFrame); clearTimeout(state.retryTimer); state.pendingFocus = null;
    };
    observeContent();
    rowMetrics.set(instanceId, state);
    state.schedule();
}
