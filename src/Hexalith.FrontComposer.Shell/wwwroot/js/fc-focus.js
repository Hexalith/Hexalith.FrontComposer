let lastFocusedRoute = null;
let lastFocusedHeading = null;
let routeHeadingObserver = null;
let routeFocusTimer = null;
let pendingRoutePath = null;
let pendingTabFocus = null;
let routeFocusGuardController = null;
const editedOrigins = new WeakMap();
const containedDialogs = new WeakSet();
let lastEditedRootRef = null;

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

// Modal containers that already hold the single modal slot. A non-modal popover that merely carries
// role="dialog" (for example the page-toolbar filter or the column prioritizer) is not one of them.
const modalContainerSelector = 'fluent-dialog, dialog, [aria-modal="true"]';

export function captureOverlayOrigin(testId = null, preserveExisting = false, owner = null) {
    if (document.activeElement?.closest(modalContainerSelector)) return false;
    const existingReservation = window.__fcModalReservation;
    if (isLiveModalReservation(existingReservation)) {
        // Every modal entry point shares one explicit reservation. Keep the destructive-command
        // reservation for the whole derived-value refresh instead of inferring ownership from a
        // short-lived focus intent, and refuse shell overlays until the owner releases it.
        return false;
    }
    window.__fcModalReservation = null;
    if (existingReservation) {
        // The keyboard tracker deliberately cannot replace origin state while a reservation exists.
        // Once that reservation expires, its origin and intent are stale too; start this launch from
        // the current trigger/focus instead of preserving the abandoned owner's connected element.
        window.__fcOverlayOrigin = null;
        window.__fcOverlayOpenIntent = null;
    }
    let preservesTrackedOrigin = false;
    if (preserveExisting) {
        // Shell overlays (palette, settings, shortcuts) keep an origin the keyboard tracker or an
        // earlier trigger already captured, including its `moved` state, exactly as before the
        // destructive-confirmation reservation existed. Tracker intents carry no `createdAt`.
        if (window.__fcOverlayOrigin instanceof HTMLElement
            && window.__fcOverlayOrigin.isConnected
            && window.__fcOverlayOpenIntent) {
            preservesTrackedOrigin = true;
        }
    }
    if (!preservesTrackedOrigin) {
        const candidate = testId
            ? document.querySelector(`[data-testid="${testId}"]`)
            : document.activeElement;
        window.__fcOverlayOrigin = candidate instanceof HTMLElement && candidate.isConnected && !candidate.disabled
            ? candidate
            : null;
        window.__fcOverlayOpenIntent = { origin: window.__fcOverlayOrigin, moved: false, watchFocus: false };
    }
    window.__fcModalReservation = {
        createdAt: Date.now(),
        durable: !preserveExisting,
        owner: typeof owner === 'string' && owner.length > 0 ? owner : null,
        origin: window.__fcOverlayOrigin,
    };
    return true;
}

// Story 13.3 BH10-01 — releases a durable reservation only for the owner that took it, without moving
// focus. A destructive renderer calls this when its confirmation never opened, including when the
// capture reply was lost, and retries it after a failed release, so an interop failure cannot keep
// shell overlays and destructive commands refused until the page reloads.
export function releaseOverlayReservation(owner) {
    const reservation = window.__fcModalReservation;
    if (!reservation || typeof owner !== 'string' || owner.length === 0 || reservation.owner !== owner) {
        return false;
    }
    window.__fcModalReservation = null;
    window.__fcOverlayOrigin = null;
    window.__fcOverlayOpenIntent = null;
    return true;
}

// Shell launch reservations expire if their overlay never opens. Destructive-command reservations
// are durable because derived-value refresh can legitimately take longer than this recovery window;
// the owning renderer releases them by owner token on every completion and exception path.
const modalReservationRecoveryMs = 5000;

// Recheck the original owner after asynchronous preparation so an abandoned renderer cannot open
// its delayed confirmation over the replacement interaction that recovered the slot.
export function ownsOverlayReservation(owner) {
    const reservation = window.__fcModalReservation;
    return typeof owner === 'string' && owner.length > 0
        && reservation?.owner === owner && isLiveModalReservation(reservation);
}

function isLiveModalReservation(reservation) {
    if (!reservation) return false;
    if (hasOpenModal()) return true;
    if (reservation.durable === true) {
        // A lost release reply must not strand the modal slot after navigation removes its invoker.
        // An open modal still owns the slot, and a connected invoker retains it during long preparation.
        return !(reservation.origin instanceof HTMLElement) || reservation.origin.isConnected;
    }
    return typeof reservation.createdAt === 'number'
        && Date.now() - reservation.createdAt < modalReservationRecoveryMs;
}

function hasOpenModal() {
    if (document.querySelector('dialog[open]')) return true;
    return Array.from(document.querySelectorAll('fluent-dialog')).some((host) => {
        const dialog = host.shadowRoot?.querySelector('dialog');
        return dialog instanceof HTMLDialogElement && dialog.open;
    });
}

export function captureEditedOrigin(root = null, fieldName = null, sequence = 0) {
    if (!(root instanceof HTMLElement) || !fieldName) return false;
    const previous = editedOrigins.get(root);
    if (previous && sequence < previous.sequence) return false;

    // Bind the origin to the field named by the edit event, never to whatever is focused now:
    // by the time this completes, focus may already sit on a warning action.
    const fieldHost = root.querySelector(`[name="${cssEscape(fieldName)}"]`);
    const declaredEditor = fieldHost instanceof HTMLElement
        ? fieldHost.matches(fieldEditableSelector)
            ? fieldHost
            : fieldHost.querySelector(fieldEditableSelector)
        : null;
    if (!(declaredEditor instanceof HTMLElement)
        || !declaredEditor.isConnected
        || declaredEditor.hasAttribute('disabled')) return false;

    editedOrigins.set(root, { origin: declaredEditor, sequence });
    lastEditedRootRef = typeof WeakRef === 'function' ? new WeakRef(root) : { deref: () => root };
    return true;
}

export function restoreEditedOrigin(root = null) {
    const formRoot = root instanceof HTMLElement ? root : lastEditedRootRef?.deref() ?? null;
    const origin = formRoot ? editedOrigins.get(formRoot)?.origin : null;
    runAfterDismiss(() => {
        // A later navigation can reopen this warning before the deferred focus return runs.
        if (formRoot?.parentElement?.querySelector('[data-testid="fc-form-abandonment-warning"]')) return;
        const connectedRoot = formRoot instanceof HTMLElement && formRoot.isConnected ? formRoot : null;
        const target = origin instanceof HTMLElement && origin.isConnected && !origin.disabled
            ? origin
            : connectedRoot?.querySelector('[data-fc-form-heading="true"]')
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
    // Fluent binds these native-dialog attributes from its host after initialization. Keep the
    // source attributes populated so a deferred binding cannot erase the accessible identity.
    host.setAttribute('aria-labelledby', titleId);
    if (title) host.setAttribute('aria-label', title);
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

const dialogTabbableSelector = [
    'fluent-button:not([disabled])',
    'button:not([disabled])',
    'a[href]',
    'input:not([disabled])',
    'select:not([disabled])',
    'textarea:not([disabled])',
    '[tabindex]:not([tabindex="-1"]):not([disabled])',
].join(', ');

// A native modal dialog makes the page inert but lets Tab leave the document for browser chrome.
// Wrap Tab and Shift+Tab across the dialog's own actions so keyboard focus stays contained.
export function containDialogFocus(testId, retryOnce = true) {
    const content = document.querySelector(`[data-testid="${cssEscape(testId)}"]`);
    const host = content?.closest('fluent-dialog');
    if (!(host instanceof HTMLElement)) {
        // The dialog host can attach one frame after its first render; retry exactly once.
        if (retryOnce) runAfterDismiss(() => containDialogFocus(testId, false));
        return false;
    }
    if (containedDialogs.has(host)) return true;

    host.addEventListener('keydown', (event) => {
        if (event.key !== 'Tab' || event.altKey || event.ctrlKey || event.metaKey) return;
        const tabbables = Array.from(host.querySelectorAll(dialogTabbableSelector))
            .filter((candidate) => candidate instanceof HTMLElement
                && candidate.isConnected
                && candidate.tabIndex >= 0
                && candidate.getClientRects().length > 0);
        if (tabbables.length === 0) return;
        const first = tabbables[0];
        const last = tabbables[tabbables.length - 1];
        const active = document.activeElement;
        const inside = active instanceof Node && host.contains(active);
        const atBoundary = event.shiftKey ? active === first : active === last;
        if (!inside || atBoundary) {
            event.preventDefault();
            (event.shiftKey ? last : first).focus();
        }
    }, true);
    containedDialogs.add(host);
    return true;
}

export function focusValidationOutcome(summaryId) {
    runAfterDismiss(() => {
        const summary = document.getElementById(summaryId);
        if (focusTarget(summary)) return;
        // FM-06 — without a rendered summary, the first invalid generated editor takes focus.
        const form = document.querySelector(`[data-fc-validation-summary-id="${cssEscape(summaryId)}"]`);
        const invalid = Array.from(form?.querySelectorAll('[data-fc-validation-field][data-fc-invalid="true"]') ?? [])
            .find((candidate) => candidate instanceof HTMLElement && isRendered(candidate));
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
        ?? orderedTargets.find((candidate) => candidate.getAttribute('data-fc-invalid') === 'true'));
}

// Story 13.3 VR-01 / BH3-08 — generated editors are Fluent web components whose focusable control
// lives in an open shadow root, where host aria-invalid / aria-describedby never reach it. The form
// marks each editor host with the non-ARIA data-fc-invalid state; this projection mirrors that state
// onto the focusable control and relates the control to its Fluent description and error message
// through ARIA element reflection, which may reference light-DOM elements from inside a shadow root.
// The Fluent field renders the visible error text once; its element gets the stable
// "{editor id}-error" id for the control's description relationship. Validation summary links
// target the editor id itself, not this error id.
const fieldAccessibilityObservers = new WeakMap();

const fieldControlSelector = [
    'input:not([type="hidden"])',
    'textarea',
    'select',
    '[role="combobox"]',
    '[role="textbox"]',
    '[role="switch"]',
    '[role="checkbox"]',
    'button',
].join(', ');

export function observeFieldAccessibility(rootId) {
    const root = typeof rootId === 'string' ? document.getElementById(rootId) : rootId;
    if (!(root instanceof HTMLElement)) return false;
    if (fieldAccessibilityObservers.has(root)) {
        syncFieldAccessibility(root);
        return true;
    }

    let scheduled = false;
    const schedule = () => {
        if (scheduled) return;
        scheduled = true;
        queueMicrotask(() => {
            scheduled = false;
            if (root.isConnected) syncFieldAccessibility(root);
        });
    };
    const observer = new MutationObserver((records) => {
        if (!root.isConnected) {
            observer.disconnect();
            fieldAccessibilityObservers.delete(root);
            return;
        }
        if (records.some((record) => record.target instanceof Node && root.contains(record.target))) {
            schedule();
        }
    });
    // characterData — an error message whose text changes in place must refresh the aria-description
    // fallback of engines without ARIA element reflection, not keep announcing the previous error.
    observer.observe(root, {
        subtree: true,
        childList: true,
        characterData: true,
        attributes: true,
        attributeFilter: ['data-fc-invalid'],
    });
    // Route removal can detach both the form and its parent. Observe removals at the document
    // boundary so that removing any ancestor releases the form and its accessibility observer.
    observer.observe(document, { childList: true, subtree: true });
    fieldAccessibilityObservers.set(root, observer);
    syncFieldAccessibility(root);
    // Fluent editors upgrade asynchronously; project again once each editor's shadow control exists.
    const pendingTags = new Set(Array.from(root.querySelectorAll('[data-fc-validation-field]'))
        .filter((host) => host.localName.includes('-') && !host.shadowRoot)
        .map((host) => host.localName));
    for (const tag of pendingTags) {
        customElements.whenDefined(tag).then(() => requestAnimationFrame(schedule));
    }
    return true;
}

export function syncFieldAccessibility(root) {
    if (!(root instanceof HTMLElement)) return 0;
    let synced = 0;
    for (const host of root.querySelectorAll('[data-fc-validation-field]')) {
        if (!(host instanceof HTMLElement)) continue;
        const control = fieldControl(host);
        const field = host.closest('fluent-field');
        const invalid = host.getAttribute('data-fc-invalid') === 'true';
        const error = invalid && field
            ? Array.from(field.children).find((child) => child.classList.contains('fluent-validation-message'))
            : null;
        if (error instanceof HTMLElement && host.id) {
            error.id = `${host.id}-error`;
        }

        const description = host.id ? document.getElementById(`${host.id}-description`) : null;
        const describedBy = [description, error].filter((element) => element instanceof HTMLElement);
        if (invalid) {
            control.setAttribute('aria-invalid', 'true');
        }
        else {
            control.removeAttribute('aria-invalid');
        }

        if ('ariaDescribedByElements' in control) {
            control.ariaDescribedByElements = describedBy.length > 0 ? describedBy : null;
        }
        else {
            // Engines without ARIA element reflection still get the text relationship.
            const text = describedBy.map((element) => element.textContent?.trim()).filter(Boolean).join(' ');
            if (text) control.setAttribute('aria-description', text);
            else control.removeAttribute('aria-description');
        }

        synced++;
    }

    return synced;
}

function fieldControl(host) {
    // A text-like editor focuses an input inside its shadow root; a dropdown may slot its control into
    // the light DOM; a switch is itself the focusable control.
    const inner = host.shadowRoot?.querySelector(fieldControlSelector)
        ?? host.querySelector(':scope > [slot="control"]');
    return inner instanceof HTMLElement ? inner : host;
}

// Story 13.3 FM-09 / BH3-05 — a blocked attempt keeps focus on the attempted control. When the
// attempt came from inside the owning form (for example Enter in a field), focus already sits on the
// attempted control and stays there; `force` moves focus back after a withdrawn action.
// BH4-02 — a zero-field inline trigger id is shared by every renderer instance of the same command,
// so the focused element carrying that id is the pressed trigger and keeps focus.
export function focusAttemptedControl(controlId, force = false) {
    if (!force && controlId) {
        const active = document.activeElement;
        if (active instanceof HTMLElement && active.isConnected && active.id === controlId) return true;
    }

    const control = controlId ? document.getElementById(controlId) : null;
    if (!(control instanceof HTMLElement) || !control.isConnected) return false;
    if (!force) {
        const active = document.activeElement;
        const form = control.closest('[data-fc-command-form="true"]');
        if (form && active instanceof HTMLElement && active !== document.body && active.isConnected && form.contains(active)) {
            return true;
        }
    }

    return focusTarget(control);
}

export function focusElementById(id) {
    return focusTarget(document.getElementById(id));
}

// Story 13.3 FM-01 / AM-26 — a background authorization refresh may replace a form with its
// denied state. The heading takes focus only when the replaced content held focus: the element
// focused before the replacement render is gone and focus fell back to the document. An initial
// denial, or a refresh while focus sits elsewhere, renders silently and leaves focus alone.
const focusBeforeReplacement = new Map();

export function captureFocusBeforeReplacement(key) {
    if (!key) return false;
    const active = document.activeElement;
    focusBeforeReplacement.delete(key);
    focusBeforeReplacement.set(key, active instanceof HTMLElement && active !== document.body ? active : null);
    // A refresh that settles allowed never consumes its capture; keep the map bounded.
    while (focusBeforeReplacement.size > 32) {
        focusBeforeReplacement.delete(focusBeforeReplacement.keys().next().value);
    }
    return true;
}

export function focusReplacementHeading(key, headingId) {
    const prior = focusBeforeReplacement.get(key);
    focusBeforeReplacement.delete(key);
    if (!(prior instanceof HTMLElement) || prior.isConnected) return false;
    const active = document.activeElement;
    if (active instanceof HTMLElement && active !== document.body && active.isConnected) return false;
    return focusTarget(document.getElementById(headingId));
}

// Story 13.3 FM-09 / BH3-07 — only a rendered active lifecycle heading counts: a lifecycle inside a
// hidden zero-field form or a closed popover cannot receive focus, so it offers no action.
function activeLifecycleHeading() {
    return Array.from(document.querySelectorAll('[data-fc-active-lifecycle="true"] [data-fc-lifecycle-heading]'))
        .find((heading) => heading instanceof HTMLElement
            && heading.isConnected
            && !heading.hasAttribute('disabled')
            && isRendered(heading)) ?? null;
}

export function focusActiveLifecycle() {
    const heading = activeLifecycleHeading();
    if (!focusTarget(heading)) return false;
    watchLifecycleSettle(heading);
    return true;
}

export function hasActiveLifecycle() {
    return activeLifecycleHeading() !== null;
}

// Story 13.3 BH3-04 — the active lifecycle heading renders only while its command is Submitting,
// Acknowledged, or Syncing. When it unmounts while focused, focus moves to the owning form's first
// editable control, or its submit control when it has none. When neither accepts focus, it moves to
// the owning form heading, or the route h1, so it never falls to the document body.
function watchLifecycleSettle(heading) {
    const wrapper = heading.closest('[data-fc-active-lifecycle]');
    if (!(wrapper instanceof HTMLElement)) return;
    const form = wrapper.closest('[data-fc-command-form="true"]') ?? wrapper;
    let done = false;
    const observer = new MutationObserver(() => {
        if (done || heading.isConnected) return;
        finish();
        const active = document.activeElement;
        if (active instanceof HTMLElement && active !== document.body && active.isConnected) return;
        if (focusFirstEditableWithin(form) || focusFirstEditableWithin(wrapper)) return;
        focusTarget(form.querySelector('[data-fc-form-heading="true"]')
            ?? document.querySelector('[data-fc-form-heading="true"]')
            ?? document.querySelector('#fc-main-content h1, main h1'));
    });
    const onFocusOut = (event) => {
        // The operator moved on while the heading is still mounted: focus is no longer ours to repair.
        if (heading.isConnected && event.relatedTarget instanceof Node) finish();
    };
    const finish = () => {
        done = true;
        observer.disconnect();
        heading.removeEventListener('focusout', onFocusOut);
    };
    heading.addEventListener('focusout', onFocusOut);
    observer.observe(form, { childList: true, subtree: true });
}

function isRendered(element) {
    return element instanceof HTMLElement && element.getClientRects().length > 0;
}

export function focusFirstEditableWithin(element) {
    if (!(element instanceof HTMLElement)) return false;
    // Declared generated editors win over incidental form plumbing (for example the hidden
    // antiforgery input EditForm renders first); only a rendered, focus-accepting control counts.
    const candidates = [
        ...element.querySelectorAll('[data-fc-validation-field]:not([disabled])'),
        ...element.querySelectorAll(editableSelector),
    ];
    return candidates.some((candidate) => candidate instanceof HTMLElement
        && candidate.getClientRects().length > 0
        && focusTarget(candidate));
}

const editableSelector = [
    '[data-fc-validation-field]:not([disabled])',
    'input:not([disabled]):not([type="hidden"])',
    'textarea:not([disabled])',
    'select:not([disabled])',
    'button:not([disabled])',
    'fluent-button:not([disabled])',
    '[contenteditable="true"]',
].join(', ');

const fieldEditableSelector = [
    '[data-fc-validation-field]:not([disabled])',
    'input:not([disabled]):not([type="hidden"])',
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
    // A Fluent select (fluent-dropdown) forwards focus to the combobox it slots into its light DOM,
    // so focus that lands inside the target also counts as focusing it.
    const active = document.activeElement;
    return active === target || (active instanceof Node && target.contains(active));
}

function cssEscape(value) {
    return typeof CSS !== 'undefined' && typeof CSS.escape === 'function'
        ? CSS.escape(value)
        : value.replace(/["\\]/g, '\\$&');
}

export function restoreOverlayOrigin(forceRouteHeading = false, owner = null) {
    // An owner-scoped restore never clears a slot that a later launch now holds.
    if (typeof owner === 'string' && owner.length > 0
        && window.__fcModalReservation && window.__fcModalReservation.owner !== owner) {
        return;
    }
    const origin = window.__fcOverlayOrigin;
    window.__fcOverlayOrigin = null;
    window.__fcOverlayOpenIntent = null;
    window.__fcModalReservation = null;
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
    window.__fcModalReservation = null;
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
            // Cancel incoming inline-form smooth scrolls before confirming the focused route.
            heading.scrollIntoView({ block: 'start', behavior: 'instant' });
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
