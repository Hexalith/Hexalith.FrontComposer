let lastFocusedRoute = null;
let lastFocusedHeading = null;
let routeHeadingObserver = null;
let routeFocusTimer = null;
let pendingRoutePath = null;
let pendingTabFocus = null;
let routeFocusGuardController = null;
const editedOriginCaptureSequences = new WeakMap();

export function prepareTabNavigation(route, tabId) {
    pendingTabFocus = { path: normalizePath(new URL(route, document.baseURI).pathname), tabId };
}

function normalizePath(path) {
    return path.replace(/\/+$/, '') || '/';
}

export function labelTabPanels(testId) {
    const roots = Array.from(document.querySelectorAll('[data-testid]'))
        .filter((root) => root.getAttribute('data-testid') === testId);
    for (const root of roots) {
        const panels = Array.from(root.querySelectorAll('[role="tabpanel"]'));
        for (const tab of root.querySelectorAll('fluent-tab[id][aria-controls]')) {
            const panelId = tab.getAttribute('aria-controls');
            const panel = panels.find((candidate) => candidate.id === panelId);
            if (panel && panel.getAttribute('aria-labelledby') !== tab.id) {
                panel.setAttribute('aria-labelledby', tab.id);
            }
        }
    }
}

export function captureOverlayOrigin(testId = null, preserveExisting = false) {
    if (document.activeElement?.closest('[role="dialog"], fluent-dialog')) return false;
    if (window.__fcOverlayOpenIntent) {
        return preserveExisting
            && window.__fcOverlayOrigin instanceof HTMLElement
            && window.__fcOverlayOrigin.isConnected;
    }
    const candidate = testId
        ? document.querySelector(`[data-testid="${testId}"]`)
        : document.activeElement;
    window.__fcOverlayOrigin = candidate instanceof HTMLElement && candidate.isConnected && !candidate.disabled
        ? candidate
        : null;
    window.__fcOverlayOpenIntent = { origin: window.__fcOverlayOrigin, moved: false, watchFocus: false };
    return true;
}

export function captureEditedOrigin(root = null, fieldName = null, sequence = 0) {
    if (!(root instanceof HTMLElement) || !fieldName) return false;
    const previousSequence = editedOriginCaptureSequences.get(root) ?? -1;
    if (sequence < previousSequence) return false;

    const fieldHost = root.querySelector(`[name="${cssEscape(fieldName)}"]`);
    const declaredEditor = fieldHost instanceof HTMLElement
        ? fieldHost.matches(fieldEditableSelector)
            ? fieldHost
            : fieldHost.querySelector(fieldEditableSelector)
        : null;
    if (!(declaredEditor instanceof HTMLElement)
        || !declaredEditor.isConnected
        || declaredEditor.hasAttribute('disabled')) return false;

    editedOriginCaptureSequences.set(root, sequence);
    window.__fcEditedOrigin = declaredEditor;
    window.__fcEditedFormRoot = root;
    return true;
}

export function restoreEditedOrigin() {
    const origin = window.__fcEditedOrigin;
    const capturedFormRoot = window.__fcEditedFormRoot;
    runAfterDismiss(() => {
        const formRoot = capturedFormRoot instanceof HTMLElement && capturedFormRoot.isConnected
            ? capturedFormRoot
            : null;
        const target = origin instanceof HTMLElement && origin.isConnected && !origin.disabled
            ? origin
            : formRoot?.querySelector('[data-fc-form-heading]')
                ?? document.querySelector('[data-fc-form-heading="true"]')
                ?? document.querySelector('#fc-main-content h1, main h1');
        focusTarget(target);
    });
}

export function labelDialog(testId, titleId, descriptionId) {
    const content = document.querySelector(`[data-testid="${cssEscape(testId)}"]`);
    const host = content?.closest('fluent-dialog');
    const dialog = host?.shadowRoot?.querySelector('dialog');
    if (!(dialog instanceof HTMLElement)) return false;

    const title = document.getElementById(titleId)?.textContent?.trim();
    const description = document.getElementById(descriptionId)?.textContent?.trim();
    const applyAccessibleIdentity = () => {
        if (!dialog.isConnected) return;
        dialog.setAttribute('aria-labelledby', titleId);
        if (title) dialog.setAttribute('aria-label', title);
        if (description) dialog.setAttribute('aria-description', description);
    };
    applyAccessibleIdentity();
    setTimeout(applyAccessibleIdentity, 150);
    return true;
}

export function focusValidationOutcome(summaryId) {
    runAfterDismiss(() => {
        const summary = document.getElementById(summaryId);
        if (focusTarget(summary)) return;
        const form = document.querySelector(`[data-fc-validation-summary-id="${cssEscape(summaryId)}"]`);
        const invalid = form?.querySelector('[aria-invalid="true"], [data-fc-validation-invalid="true"]')
            ?? Array.from(form?.querySelectorAll('[data-fc-validation-field]') ?? [])
                .find((candidate) => candidate instanceof HTMLElement
                    && document.getElementById(`${candidate.id}-error`)?.textContent?.trim());
        focusTarget(invalid);
    });
}

export function focusValidationTarget(summaryId, targetId) {
    const summary = document.getElementById(summaryId);
    const form = summary?.closest('[data-fc-command-form="true"]')
        ?? document.querySelector(`[data-fc-validation-summary-id="${cssEscape(summaryId)}"]`);
    const orderedTargets = Array.from(form?.querySelectorAll('[data-fc-validation-field]') ?? [])
        .filter((candidate) => candidate instanceof HTMLElement
            && candidate.isConnected
            && !candidate.hasAttribute('disabled'));
    const requested = document.getElementById(targetId);
    if (requested instanceof HTMLElement && orderedTargets.includes(requested) && focusTarget(requested)) {
        return true;
    }

    const linkedTargets = Array.from(summary?.querySelectorAll('[data-fc-validation-target]') ?? []);
    const requestedLinkIndex = linkedTargets.findIndex((candidate) => candidate.getAttribute('data-fc-validation-target') === targetId);
    if (requestedLinkIndex >= 0) {
        for (const link of linkedTargets.slice(requestedLinkIndex + 1)) {
            const nextId = link.getAttribute('data-fc-validation-target');
            if (nextId && focusTarget(document.getElementById(nextId))) return true;
        }
    }

    const firstLinkedTarget = linkedTargets
        .map((link) => document.getElementById(link.getAttribute('data-fc-validation-target') ?? ''))
        .find((candidate) => candidate instanceof HTMLElement && candidate.isConnected && !candidate.hasAttribute('disabled'));
    return focusTarget(firstLinkedTarget
        ?? orderedTargets.find((candidate) => candidate.getAttribute('aria-invalid') === 'true'));
}

export function focusElementById(id) {
    return focusTarget(document.getElementById(id));
}

export function focusActiveLifecycle() {
    const active = document.querySelector('[data-fc-active-lifecycle="true"] [data-fc-lifecycle-heading]');
    return focusTarget(active);
}

export function hasActiveLifecycle() {
    const active = document.querySelector('[data-fc-active-lifecycle="true"] [data-fc-lifecycle-heading]');
    return active instanceof HTMLElement && active.isConnected && !active.hasAttribute('disabled');
}

export function focusFirstEditableWithin(element) {
    if (!(element instanceof HTMLElement)) return false;
    const target = element.querySelector(editableSelector);
    return focusTarget(target);
}

const editableSelector = [
    '[data-fc-validation-field]:not([disabled])',
    'input:not([disabled])',
    'textarea:not([disabled])',
    'select:not([disabled])',
    'button:not([disabled])',
    'fluent-button:not([disabled])',
    '[contenteditable="true"]',
].join(', ');

const fieldEditableSelector = [
    '[data-fc-validation-field]:not([disabled])',
    'input:not([disabled])',
    'textarea:not([disabled])',
    'select:not([disabled])',
    '[contenteditable="true"]',
].join(', ');

function focusTarget(target) {
    if (!(target instanceof HTMLElement) || !target.isConnected || target.hasAttribute('disabled')) return false;
    if (!target.hasAttribute('tabindex') && /^H[1-6]$/.test(target.tagName)) {
        target.setAttribute('tabindex', '-1');
    }
    target.scrollIntoView({ block: 'nearest' });
    target.focus({ preventScroll: true });
    return document.activeElement === target;
}

function cssEscape(value) {
    return typeof CSS !== 'undefined' && typeof CSS.escape === 'function'
        ? CSS.escape(value)
        : value.replace(/["\\]/g, '\\$&');
}

export function restoreOverlayOrigin(forceRouteHeading = false) {
    const origin = window.__fcOverlayOrigin;
    window.__fcOverlayOrigin = null;
    window.__fcOverlayOpenIntent = null;
    let userMovedFocus = false;
    const markUserMove = () => { userMovedFocus = true; };
    document.addEventListener('pointerdown', markUserMove, true);
    document.addEventListener('keydown', markUserMove, true);
    runAfterDismiss(() => {
        document.removeEventListener('pointerdown', markUserMove, true);
        document.removeEventListener('keydown', markUserMove, true);
        if (userMovedFocus) {
            return;
        }
        const target = !forceRouteHeading && origin instanceof HTMLElement && origin.isConnected && !origin.disabled
            ? origin
            : document.querySelector('#fc-main-content h1, main h1');
        if (!(target instanceof HTMLElement)) {
            return;
        }
        if (!target.hasAttribute('tabindex') && target.tagName === 'H1') {
            target.setAttribute('tabindex', '-1');
        }
        target.scrollIntoView({ block: 'nearest' });
        target.focus({ preventScroll: true });
    });
}

export function focusOverlayEntry(id, respectOpeningIntent = false) {
    const intent = respectOpeningIntent ? window.__fcOverlayOpenIntent : null;
    if (respectOpeningIntent) window.__fcOverlayOpenIntent = null;
    if (intent?.moved) return false;
    const guard = new AbortController();
    let inputOrigin = null;
    let moved = false;
    const recordInput = () => { inputOrigin = document.activeElement; };
    document.addEventListener('keydown', recordInput, { capture: true, signal: guard.signal });
    document.addEventListener('pointerdown', recordInput, { capture: true, signal: guard.signal });
    document.addEventListener('focusin', () => {
        if (inputOrigin && document.activeElement !== inputOrigin) moved = true;
    }, { capture: true, signal: guard.signal });
    const focus = () => {
        const target = document.getElementById(id);
        if (!(target instanceof HTMLElement) || !target.isConnected || target.hasAttribute('disabled')) return false;
        target.scrollIntoView({ block: 'nearest' });
        target.focus({ preventScroll: true });
        return true;
    };
    focus();
    setTimeout(() => {
        guard.abort();
        if (!moved) focus();
    }, 150);
    return document.activeElement === document.getElementById(id);
}

export function preserveRouteFocusAfterOverlay(openedRoute) {
    const openedPath = new URL(openedRoute, document.baseURI).pathname;
    const origin = window.__fcOverlayOrigin;
    window.__fcOverlayOrigin = null;
    window.__fcOverlayOpenIntent = null;
    const focusHeadingIfNeeded = () => {
        if (window.location.pathname === openedPath) {
            return;
        }
        const active = document.activeElement;
        if (active !== document.body && active !== origin
            && !(origin instanceof HTMLElement && active instanceof Node && origin.contains(active))) {
            return;
        }
        const heading = document.querySelector('#fc-main-content h1, main h1');
        if (heading instanceof HTMLElement) {
            if (!heading.hasAttribute('tabindex')) {
                heading.setAttribute('tabindex', '-1');
            }
            heading.focus({ preventScroll: true });
        }
    };
    const onFocus = (event) => {
        if (window.location.pathname === openedPath
            || !(event.target instanceof HTMLElement)
            || (event.target.getAttribute('data-testid') !== 'fc-palette-trigger'
                && !(origin instanceof HTMLElement && event.composedPath().includes(origin)))) {
            return;
        }
        const heading = document.querySelector('#fc-main-content h1, main h1');
        if (heading instanceof HTMLElement) {
            if (!heading.hasAttribute('tabindex')) {
                heading.setAttribute('tabindex', '-1');
            }
            queueMicrotask(() => heading.focus({ preventScroll: true }));
            document.removeEventListener('focusin', onFocus, true);
        }
    };
    document.addEventListener('focusin', onFocus, true);
    runAfterDismiss(focusHeadingIfNeeded);
    setTimeout(focusHeadingIfNeeded, 150);
    setTimeout(() => document.removeEventListener('focusin', onFocus, true), 3000);
}

export function focusRouteHeading(routeKey, routeFocusOwner, preservePaletteFocus = false, ownedActivation = false, canonicalAlias = false) {
    const path = normalizePath(new URL(routeKey, document.baseURI).pathname);
    if (pendingTabFocus && pendingTabFocus.path !== path) {
        pendingTabFocus = null;
    }
    routeFocusGuardController?.abort();
    routeFocusGuardController = new AbortController();
    const focusGuard = routeFocusGuardController;
    routeHeadingObserver?.disconnect();
    clearTimeout(routeFocusTimer);
    pendingRoutePath = routeKey;
    let confirmedHeading = null;
    let confirmedText = null;
    let deniedReported = false;
    let allowRouteFocusRestore = false;
    const stopRouteFocusRestore = () => { allowRouteFocusRestore = false; };
    document.addEventListener('pointerdown', stopRouteFocusRestore, { capture: true, once: true, signal: focusGuard.signal });
    document.addEventListener('keydown', stopRouteFocusRestore, { capture: true, once: true, signal: focusGuard.signal });
    // Restoration only recovers focus dropped by the navigation itself; once the guard window
    // closes, a later deliberate move to the page body must not be pulled back to the heading.
    setTimeout(() => {
        focusGuard.abort();
        allowRouteFocusRestore = false;
    }, 3000);

    const confirm = () => {
        if (pendingRoutePath !== routeKey || normalizePath(window.location.pathname) !== path) {
            return false;
        }
        if (canonicalAlias) {
            // Manifest-declared aliases confirm only their canonical child.
            return false;
        }
        // AuthorizeRouteView can leave the outgoing heading mounted while the
        // destination is pending. A reused routed component, however, keeps
        // the same heading element after its route parameter changes.
        if (document.querySelector('[data-fc-route-authorizing="true"]')) {
            lastFocusedRoute = null;
            return false;
        }
        if (document.querySelector('[data-fc-route-unavailable="true"]')) {
            return true;
        }
        const routeContainers = Array.from(document.querySelectorAll('[data-fc-route-content]'));
        const routeContainer = routeContainers.find((candidate) => candidate.getAttribute('data-fc-route-content') === routeKey);
        if (routeContainers.length && !routeContainer) {
            return false;
        }
        const denied = routeContainer?.querySelector('[data-fc-route-denied="true"]');
        if (denied) {
            const deniedHeading = denied.querySelector('h1');
            if (!(deniedHeading instanceof HTMLElement)) {
                return false;
            }
            if (!ownedActivation && (lastFocusedRoute !== routeKey || lastFocusedHeading !== deniedHeading)) {
                if (!deniedHeading.hasAttribute('tabindex')) {
                    deniedHeading.setAttribute('tabindex', '-1');
                }
                deniedHeading.focus({ preventScroll: true });
                lastFocusedRoute = routeKey;
                lastFocusedHeading = deniedHeading;
            }
            if (!deniedReported) {
                deniedReported = true;
                routeFocusOwner.invokeMethodAsync('ReportRouteHeadingDenied', routeKey).catch(() => {});
            }
            clearTimeout(routeFocusTimer);
            return true;
        }
        const headings = Array.from((routeContainer ?? document).querySelectorAll('#fc-main-content h1, main h1, h1'))
            .filter((candidate) => candidate instanceof HTMLElement && candidate.isConnected);
        if (headings.length !== 1) {
            return false;
        }
        const heading = headings[0];
        const label = heading.textContent?.trim();
        if (!label) {
            return false;
        }
        const active = document.activeElement;
        const tabRoot = Array.from((routeContainer ?? document).querySelectorAll('[data-fc-module-route]'))
            .find((root) => {
                const modulePath = root.getAttribute('data-fc-module-route')?.replace(/\/$/, '');
                return modulePath && (path === modulePath || path.startsWith(`${modulePath}/`));
            });
        const selectedTab = tabRoot?.querySelector('fluent-tab[aria-selected="true"]');
        const tabMatchesRoute = selectedTab instanceof HTMLElement
            && (path.toLowerCase().endsWith(`/${selectedTab.id.toLowerCase()}`)
                || path.toLowerCase() === tabRoot.getAttribute('data-fc-module-route')?.toLowerCase());
        if (tabRoot && !tabMatchesRoute) {
            // Reused workspace headings can render before the incoming tab state.
            // Wait for the selected panel before acknowledging the route.
            return false;
        }
        if (pendingTabFocus?.path === path && selectedTab?.id !== pendingTabFocus.tabId) {
            return false;
        }
        if (confirmedHeading === heading && confirmedText === label) {
            if (allowRouteFocusRestore && (document.activeElement === document.body
                || !(document.activeElement instanceof HTMLElement)
                || !document.activeElement.isConnected)) {
                heading.focus({ preventScroll: true });
            }
            return true;
        }
        let tabRetainsFocus = tabMatchesRoute && active instanceof HTMLElement
            && active.closest('[data-fc-module-tab="true"]') !== null;
        const palette = preservePaletteFocus ? document.querySelector('[data-testid="fc-palette-root"]') : null;
        if (palette instanceof HTMLElement && palette.isConnected) {
            if (!palette.contains(active)) {
                const search = palette.querySelector('[data-testid="fc-palette-search"]');
                if (search instanceof HTMLElement) {
                    search.focus({ preventScroll: true });
                }
            }
            tabRetainsFocus = true;
        }
        if (pendingTabFocus?.path === path && selectedTab instanceof HTMLElement
            && selectedTab.id === pendingTabFocus.tabId) {
            selectedTab.focus({ preventScroll: true });
            tabRetainsFocus = true;
            pendingTabFocus = null;
        }
        const samePageQueryUpdate = lastFocusedRoute !== null
            && normalizePath(new URL(lastFocusedRoute, document.baseURI).pathname) === path
            && lastFocusedHeading === heading;
        const replacementOnConfirmedRoute = lastFocusedRoute === routeKey && lastFocusedHeading !== heading;
        const canFocusReplacement = !replacementOnConfirmedRoute || active === document.body
            || !(active instanceof HTMLElement) || !active.isConnected
            || (active === lastFocusedHeading && !lastFocusedHeading?.isConnected);
        if (!tabRetainsFocus && !samePageQueryUpdate && canFocusReplacement
            && (lastFocusedRoute !== routeKey || lastFocusedHeading !== heading)) {
            if (!heading.hasAttribute('tabindex')) {
                heading.setAttribute('tabindex', '-1');
            }
            heading.scrollIntoView({ block: 'nearest' });
            heading.focus({ preventScroll: true });
            allowRouteFocusRestore = true;
        }
        lastFocusedRoute = routeKey;
        lastFocusedHeading = heading;
        confirmedHeading = heading;
        confirmedText = label;
        clearTimeout(routeFocusTimer);
        routeFocusOwner.invokeMethodAsync('ConfirmRouteHeading', routeKey)
            .catch(() => {});
        return true;
    };

    const alreadyConfirmed = confirm();
    if (document.querySelector('[data-fc-route-unavailable="true"]')) {
        return;
    }
    routeHeadingObserver = new MutationObserver(confirm);
    routeHeadingObserver.observe(document.body, { attributes: true, childList: true, characterData: true, subtree: true });
    if (alreadyConfirmed) {
        return;
    }
    const authorizationDeadline = Date.now() + 30000;
    const expire = () => {
        if (confirm() || pendingRoutePath !== routeKey) {
            return;
        }
        if (document.querySelector('[data-fc-route-authorizing="true"]') && Date.now() < authorizationDeadline) {
            routeFocusTimer = setTimeout(expire, 5000);
            return;
        }
        routeHeadingObserver?.disconnect();
        routeHeadingObserver = null;
        pendingTabFocus = null;
        routeFocusOwner.invokeMethodAsync('ReportRouteHeadingMissing', routeKey)
            .catch(() => {});
    };
    routeFocusTimer = setTimeout(expire, 5000);
}

function runAfterDismiss(callback) {
    // Pass-5 P11 — wrap the callback so a synchronous throw does not escape to window.onerror
    // (which in Blazor Server triggers a circuit disconnect).
    const safe = () => {
        try {
            callback();
        } catch (e) {
            // eslint-disable-next-line no-console
            console.error("[FcFocus]", e);
        }
    };

    if (typeof requestAnimationFrame === "function") {
        requestAnimationFrame(safe);
        return;
    }

    setTimeout(safe, 0);
}

export function focusBodyIfNeeded() {
    runAfterDismiss(() => {
        const active = document.activeElement;
        if (
            !active ||
            active === document.body ||
            !(active instanceof HTMLElement) ||
            !active.isConnected
        ) {
            if (document.body && typeof document.body.focus === "function") {
                // Pass-5 P10 — <body> is not focusable unless it carries a tabindex.
                // Set tabindex="-1" so programmatic .focus() lands on body; no visual change,
                // no tab-order change. Leave the attribute in place — removing it synchronously
                // after focus would immediately blur the element.
                if (!document.body.hasAttribute("tabindex")) {
                    document.body.setAttribute("tabindex", "-1");
                }

                document.body.focus({ preventScroll: true });
            }
        }
    });
}
