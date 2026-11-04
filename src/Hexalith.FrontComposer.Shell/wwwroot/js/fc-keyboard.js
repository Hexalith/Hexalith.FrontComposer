// Same modal containers as fc-focus.js captureOverlayOrigin. A non-modal popover that merely
// carries role="dialog" does not hold the single modal slot.
const modalContainerSelector = 'fluent-dialog, dialog, [aria-modal="true"]';

// Capture each open before an asynchronous Blazor callback. Only a real focus move
// following later user input suppresses entry focus; typing alone does not.
if (!window.__fcFocusIntentTrackerInstalled) {
    window.__fcFocusIntentTrackerInstalled = true;
    const record = (event) => {
        const intent = window.__fcOverlayOpenIntent;
        if (intent) intent.watchFocus = true;
        const reservation = window.__fcModalReservation;
        if (reservation) reservation.watchFocus = true;
        const trigger = (typeof event.composedPath === 'function' ? event.composedPath() : [event.target])
            .filter((node) => node instanceof Element)
            .map((node) => node.closest('[data-testid="fc-settings-button"], [data-testid="fc-palette-trigger"]'))
            .find((node) => node instanceof HTMLElement);
        const opensTrigger = event.type === 'pointerdown' || event.key === 'Enter' || event.key === ' ';
        if (opensTrigger && trigger instanceof HTMLElement
            && !document.activeElement?.closest(modalContainerSelector)
            && !window.__fcModalReservation) {
            window.__fcOverlayOrigin = trigger;
            window.__fcOverlayOpenIntent = { origin: trigger, moved: false, watchFocus: false };
        }
    };
    document.addEventListener('pointerdown', record, true);
    document.addEventListener('keydown', record, true);
    document.addEventListener('focusin', (event) => {
        // Entry focus consumes its short-lived intent. The modal session must keep tracking
        // operator moves until delayed disposal releases the reservation.
        const reservation = window.__fcModalReservation;
        if (reservation?.watchFocus && event.target !== reservation.origin
            && !event.target?.closest?.(modalContainerSelector)) reservation.moved = true;
        const intent = window.__fcOverlayOpenIntent;
        if (intent?.watchFocus && event.target !== intent.origin
            && !event.target?.closest?.(modalContainerSelector)
            && !event.target?.matches?.('[data-testid="fc-palette-search"], #fc-settings-heading')) {
            intent.moved = true;
        }
    }, true);
}

// Story 13.6 D3/O4 — a shell overlay carries one dialog name. The palette content and the
// navigation hamburger declare it with data-fc-dialog-label. Fluent's dialog and drawer forward
// their host aria-label to the native dialog, so naming the host as soon as either side is
// inserted names the dialog before Fluent opens it.
const dialogLabelSelector = '[data-fc-dialog-label]';
const dialogHostSelector = 'fluent-dialog, fluent-drawer';

function applyDialogLabel(source) {
    const label = source?.getAttribute?.('data-fc-dialog-label');
    if (!label) return;
    // The palette content sits inside its dialog. FluentLayoutHamburger renders its drawer outside
    // its own wrapper, with the id "{hamburger id}-drawer"; the browser regression pins that link.
    const drawer = source.id ? document.getElementById(`${source.id}-drawer`) : null;
    for (const host of [source.closest('fluent-dialog'), ...source.querySelectorAll(dialogHostSelector), drawer?.matches('fluent-drawer') ? drawer : null]) {
        if (!(host instanceof HTMLElement)) continue;
        if (host.getAttribute('aria-label') !== label) host.setAttribute('aria-label', label);
        // A host that was bound before the label arrived keeps its stale template value in
        // engines with native ARIA reflection; align the open shadow dialog with the host.
        const native = host.shadowRoot?.querySelector('dialog');
        if (native && native.getAttribute('aria-label') !== label) native.setAttribute('aria-label', label);
    }
}

function applyDialogLabels() {
    for (const source of document.querySelectorAll(dialogLabelSelector)) applyDialogLabel(source);
}

if (!window.__fcDialogLabelObserverInstalled) {
    window.__fcDialogLabelObserverInstalled = true;
    const relevant = `${dialogLabelSelector}, ${dialogHostSelector}`;
    new MutationObserver((records) => {
        const inserted = records.some((record) => Array.from(record.addedNodes).some((node) =>
            node instanceof Element && (node.matches(relevant) || node.querySelector(relevant))));
        if (inserted) applyDialogLabels();
    }).observe(document.documentElement, { childList: true, subtree: true });
    applyDialogLabels();
}

function isEditableElement(element) {
    if (!element || !(element instanceof HTMLElement)) {
        return false;
    }

    if (element.tagName === "INPUT" || element.tagName === "TEXTAREA") {
        return true;
    }

    if (element.isContentEditable) {
        return true;
    }

    return (
        element.closest(
            '[contenteditable="true"], [contenteditable=""], [role="textbox"]',
        ) !== null
    );
}

// P17 (Pass-6): walk event.composedPath() to detect editable elements inside shadow DOM.
// Shadow DOM retargets event.target to the shadow host, so a third-party web-component editor
// wrapping its <input> in a shadow root would bypass plain isEditableElement(event.target).
// Used by registerShellKeyFilter for the bare-letter chord-prefix guard.
//
// P10 (Pass-1 review): three robustness improvements over P17:
//   (a) Synthetic events with `composedPath() === []` (e.g., manually constructed `KeyboardEvent`s
//       that omit `bubbles: true`) used to short-circuit to `false`; now they fall through to the
//       `event.target` fallback so test harnesses and synthetic-event consumers behave correctly.
//   (b) Closed shadow roots (`attachShadow({ mode: "closed" })`) clip `composedPath()` at the
//       shadow host. The walker fails open by returning `false`, but `event.target` for a shadow-
//       DOM event is the host itself; checking `event.target` (and `closest()` on it) catches the
//       common case where the host is the editable element or has an editable ancestor.
//   (c) Each `composedPath()` node is also probed for `host`/shadow-root subtrees that the walker
//       would otherwise skip — best-effort detection where the browser exposes it.
function isEditableInComposedPath(event) {
    // (a) + (b): always check the immediate target as a baseline so the closed-shadow / synthetic-
    // event paths cannot silently bypass the editable guard.
    if (isEditableElement(event.target)) {
        return true;
    }

    if (typeof event.composedPath !== "function") {
        return false;
    }

    const path = event.composedPath();
    for (const node of path) {
        if (node instanceof HTMLElement && isEditableElement(node)) {
            return true;
        }

        // (c) best-effort probe of any open shadow root the walker exposed via `node.shadowRoot`.
        if (node && node.shadowRoot && node.shadowRoot.activeElement
            && isEditableElement(node.shadowRoot.activeElement)) {
            return true;
        }
    }

    return false;
}

function registerFilter(element, marker, predicate) {
    if (!element || element[marker]) {
        return;
    }

    const handler = (event) => {
        if (predicate(event)) {
            event.preventDefault();
        }
    };

    element.addEventListener("keydown", handler);
    element[marker] = handler;
}

export function focusElement(element) {
    // Mounted skip targets are ready immediately. A delayed retry could steal a
    // later focus move after navigation or resize; only overlay editors need it.
    if (element?.id === 'fc-main-content' || element?.id === 'fc-nav') {
        if (element.isConnected && typeof element.focus === 'function') element.focus();
        return;
    }
    const isPaletteEntry = element?.getAttribute?.('data-testid') === 'fc-palette-search';
    const intent = isPaletteEntry ? window.__fcOverlayOpenIntent : null;
    if (isPaletteEntry) window.__fcOverlayOpenIntent = null;
    if (intent?.moved) return;
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
        if (element?.isConnected && typeof element.focus === "function") element.focus();
    };
    const enter = () => {
        if (!moved) focus();
        setTimeout(() => {
            guard.abort();
            if (!moved) focus();
        }, 150);
    };
    const modalHost = isPaletteEntry ? element.closest('fluent-dialog') : null;
    if (modalHost && !modalHost.shadowRoot?.querySelector('dialog')?.open) {
        // First render can precede Fluent's queued showModal. A closed dialog cannot focus its
        // editors; enter when it opens, keeping later operator focus moves authoritative.
        modalHost.addEventListener('toggle', () => {
            if (modalHost.shadowRoot?.querySelector('dialog')?.open) enter();
            else guard.abort();
        }, { once: true, signal: guard.signal });
        const observer = new MutationObserver(() => {
            if (!element.isConnected) guard.abort();
        });
        observer.observe(document.body, { childList: true, subtree: true });
        guard.signal.addEventListener('abort', () => observer.disconnect(), { once: true });
    } else {
        enter();
    }
}

export function focusVisibleElementById(id) {
    if (!id) {
        return false;
    }

    const escaper =
        typeof CSS !== "undefined" && typeof CSS.escape === "function"
            ? CSS.escape
            : (value) => value.replace(/"/g, '\\"');
    const candidates = Array.from(document.querySelectorAll(`#${escaper(id)}`));
    const visible = candidates.filter((element) => {
        if (!(element instanceof HTMLElement) || element.hidden) {
            return false;
        }

        const style = getComputedStyle(element);
        return style.display !== "none"
            && style.visibility !== "hidden"
            && element.getClientRects().length > 0;
    });
    const target = visible.at(-1);
    if (!target || typeof target.focus !== "function") {
        return false;
    }

    target.focus();
    return true;
}

export function isEditableElementActive() {
    return isEditableElement(document.activeElement);
}

export function registerShellKeyFilter(element) {
    if (!element || element.__fcShellKeyFilter) {
        return;
    }

    // Keep the route heading's announcement, but restore forward access to bypass links
    // on the next Tab. This also supports adopters using Blazor's FocusOnNavigate.
    const isRouteHeading = (target) => target instanceof HTMLElement
        && target.matches('h1[tabindex]')
        && target.closest('#fc-main-content')?.closest('.fc-shell-root') === element
        && !target.closest(modalContainerSelector)
        && !isEditableElement(target);
    let announcedHeading = isRouteHeading(document.activeElement) ? document.activeElement : null;
    let pointerHeading = null;
    let pointerHeadingLocation = null;
    let headingLocation = window.location.href;
    let tabFocusPending = false;
    const recordPointer = (event) => {
        announcedHeading = null;
        tabFocusPending = false;
        if (event.target instanceof Element && !event.target.closest(modalContainerSelector)) {
            pointerHeading = event.target.closest('h1');
            pointerHeadingLocation = window.location.href;
            headingLocation = window.location.href;
        }
    };
    element.addEventListener('pointerdown', recordPointer, true);
    element.__fcShellSkipPointer = recordPointer;

    // Native Tab scrolling in WebKit can align the control border with an inner
    // scrollport and clip its outer focus ring. Run after that native scroll so
    // the authored scroll-margin/scroll-padding can protect the complete target.
    // Keep this within the mounted shell and never move focus or follow a stale target.
    const showFocusedTarget = (event) => {
        const target = event.target;
        const wasPointerHeading = target === pointerHeading && window.location.href === pointerHeadingLocation;
        announcedHeading = !tabFocusPending && !wasPointerHeading && isRouteHeading(target) ? target : null;
        tabFocusPending = false;
        if (isRouteHeading(target)) headingLocation = window.location.href;
        cancelAnimationFrame(element.__fcShellFocusFrame);
        element.__fcShellFocusFrame = requestAnimationFrame(() => {
            if (element.isConnected && target instanceof HTMLElement && target.isConnected
                && (document.activeElement === target || target.matches(':focus-within'))) {
                target.scrollIntoView({ block: 'nearest', inline: 'nearest', behavior: 'instant' });
            }
        });
    };
    element.addEventListener('focusin', showFocusedTarget);
    element.__fcShellFocusVisibility = showFocusedTarget;

    const handler = (event) => {
        const key = (event.key ?? "").toLowerCase();
        const active = document.activeElement;
        const hasModifier =
            event.ctrlKey || event.metaKey || (event.shiftKey && (key.length !== 1 || /\p{L}/u.test(key))) || event.altKey;

        tabFocusPending = key === 'tab';
        // A routed component may reuse its focused heading, so no new focusin fires.
        const heading = announcedHeading
            ?? (window.location.href !== headingLocation && isRouteHeading(active) ? active : null);
        if (key === 'tab' && !hasModifier && !event.defaultPrevented && !event.isComposing
            && active === heading && isRouteHeading(active) && !window.__fcModalReservation) {
            const skip = element.querySelector(':scope > a.fc-skip-link[href]');
            if (skip instanceof HTMLElement && skip.tabIndex >= 0 && !skip.closest('[inert]')) {
                skip.focus();
                if (document.activeElement === skip) {
                    event.preventDefault();
                    return;
                }
            }
        }

        if (key === "/" && (event.isComposing || event.keyCode === 229 || (!hasModifier && isEditableInComposedPath(event)))) {
            event.stopPropagation();
            return;
        }

        // Modifier-bearing framework shortcuts: prevent browser default AND let Blazor route them.
        // Accept Ctrl+K/, on Windows+Linux AND Cmd+K/, on macOS — `meta+k` is registered as a
        // distinct shortcut server-side so Mac Safari/Chrome users aren't sent to the address bar.
        const isPrimaryMod =
            (event.ctrlKey && !event.metaKey) ||
            (event.metaKey && !event.ctrlKey);
        if (
            isPrimaryMod &&
            !event.shiftKey &&
            !event.altKey &&
            (key === "k" || key === ",")
        ) {
            const active = document.activeElement;
            if (!active?.closest(modalContainerSelector) && !window.__fcModalReservation) {
                window.__fcOverlayOrigin = active instanceof HTMLElement && active.isConnected && !active.disabled ? active : null;
                window.__fcOverlayOpenIntent = { origin: active, moved: false, watchFocus: false };
            }
            event.preventDefault();
            return;
        }

        // P25 — Ctrl+Shift+D collides with browser bookmark defaults (Chrome/Edge "Bookmark all
        // tabs", Firefox "Bookmarks sidebar"). Prevent the browser default so the dev-mode overlay
        // toggle reaches the Blazor handler. Mac Cmd+Shift+D maps the same way.
        if (
            isPrimaryMod &&
            event.shiftKey &&
            !event.altKey &&
            key === "d"
        ) {
            event.preventDefault();
            return;
        }

        // Bare-letter keys targeting an editable element must NEVER reach the Blazor global
        // router — stopPropagation avoids the circuit round-trip previously paid by
        // IsEditableElementActiveAsync (DN3). P17 (Pass-6): walk composedPath() so shadow-DOM
        // hosts (third-party web-component editors) don't escape the guard.
        if (
            !hasModifier &&
            key.length === 1 &&
            isEditableInComposedPath(event)
        ) {
            event.stopPropagation();
        }
    };

    element.addEventListener("keydown", handler);
    element.__fcShellKeyFilter = handler;
}

export function registerPaletteKeyFilter(element) {
    registerFilter(element, "__fcPaletteKeyFilter", (event) => {
        const key = event.key ?? "";
        return (
            key === "ArrowDown" ||
            key === "ArrowUp" ||
            key === "Enter" ||
            key === "Escape"
        );
    });
    registerPaletteAccessibility(element);
}

function registerPaletteAccessibility(element) {
    if (!(element instanceof HTMLElement) || element.__fcPaletteAccessibility) return;
    const state = { disposed: false, frame: 0, observer: null, shadowObserver: null, shadowRoot: null };
    const sync = () => {
        if (state.disposed || !element.isConnected) return;
        applyDialogLabel(element);
        const host = element.querySelector('[data-testid="fc-palette-search"]');
        if (!(host instanceof HTMLElement)) return;
        if (host.shadowRoot !== state.shadowRoot) {
            state.shadowObserver?.disconnect();
            state.shadowRoot = host.shadowRoot;
            if (state.shadowRoot) {
                state.shadowObserver = new MutationObserver(sync);
                state.shadowObserver.observe(state.shadowRoot, { childList: true, subtree: true });
            }
        }
        const input = host?.shadowRoot?.querySelector('input');
        if (!(input instanceof HTMLInputElement)) return;
        const resultId = host.getAttribute('data-fc-palette-controls');
        const results = resultId ? element.querySelector(`#${CSS.escape(resultId)}`) : null;
        const listbox = results?.querySelector('[role="listbox"]') ?? null;
        const activeId = host.getAttribute('data-fc-palette-active-descendant');
        const active = activeId && listbox ? listbox.querySelector(`#${CSS.escape(activeId)}`) : null;
        const reflected = 'ariaControlsElements' in input && 'ariaActiveDescendantElement' in input;
        const owner = reflected ? input : host;
        const label = host.getAttribute('aria-label') ?? host.getAttribute('placeholder') ?? '';
        for (const [name, value] of [
            ['role', 'combobox'], ['aria-haspopup', 'listbox'], ['aria-autocomplete', 'list'],
            ['aria-expanded', host.getAttribute('data-fc-palette-expanded') ?? 'false'], ['aria-label', label],
        ]) {
            if (owner.getAttribute(name) !== value) owner.setAttribute(name, value);
        }
        if (reflected) {
            // ID strings in the shadow input cannot reference the light-DOM results.
            // Element references preserve those real accessibility relationships.
            const controlled = input.ariaControlsElements ?? [];
            if (controlled.length !== (listbox ? 1 : 0) || (listbox && controlled[0] !== listbox)) {
                input.ariaControlsElements = listbox ? [listbox] : [];
            }
            if (input.ariaActiveDescendantElement !== active) input.ariaActiveDescendantElement = active;
            for (const name of ['role', 'aria-haspopup', 'aria-autocomplete', 'aria-expanded', 'aria-controls', 'aria-activedescendant']) {
                host.removeAttribute(name);
            }
        } else {
            // Retain the existing host semantics on older engines without reflection.
            owner.setAttribute('aria-controls', listbox?.id ?? resultId ?? '');
            if (activeId) owner.setAttribute('aria-activedescendant', activeId);
            else owner.removeAttribute('aria-activedescendant');
        }
    };
    state.observer = new MutationObserver(sync);
    state.observer.observe(element, {
        childList: true,
        subtree: true,
        attributes: true,
        attributeFilter: ['data-fc-palette-controls', 'data-fc-palette-active-descendant', 'data-fc-palette-expanded', 'aria-label', 'placeholder'],
    });
    element.__fcPaletteAccessibility = state;
    sync();
    customElements.whenDefined('fluent-text-input').then(() => {
        if (!state.disposed) state.frame = requestAnimationFrame(sync);
    });
}

// P9 (2026-04-21 pass-3): teardown companions for the register* pair so hot-reload and
// circuit reconnect paths don't accumulate stale handlers on DOM nodes that get re-attached
// to new Blazor components. Callers invoke these from DisposeAsync via JS interop.
export function unregisterShellKeyFilter(element) {
    if (!element || !element.__fcShellKeyFilter) {
        return;
    }

    element.removeEventListener("keydown", element.__fcShellKeyFilter);
    element.removeEventListener('focusin', element.__fcShellFocusVisibility);
    element.removeEventListener('pointerdown', element.__fcShellSkipPointer, true);
    cancelAnimationFrame(element.__fcShellFocusFrame);
    element.__fcShellFocusVisibility = null;
    element.__fcShellSkipPointer = null;
    element.__fcShellKeyFilter = null;
}

export function unregisterPaletteKeyFilter(element) {
    const accessibility = element?.__fcPaletteAccessibility;
    if (accessibility) {
        accessibility.disposed = true;
        accessibility.observer.disconnect();
        accessibility.shadowObserver?.disconnect();
        cancelAnimationFrame(accessibility.frame);
        element.__fcPaletteAccessibility = null;
    }
    if (!element || !element.__fcPaletteKeyFilter) {
        return;
    }

    element.removeEventListener("keydown", element.__fcPaletteKeyFilter);
    element.__fcPaletteKeyFilter = null;
}

// Story 4-3 T6.2 / D10 — focus-scope query + focus target used by the `/` shortcut. Returns
// true when:
//   - the active element is inside an editable control → NEVER fire (user is typing and the
//     literal `/` must reach the input unmolested); this also covers typing INTO a column
//     filter cell, which is itself an <input> nested in [data-fc-datagrid].
//   - the active element is body or null (page load, no explicit focus) AND at least one
//     [data-fc-datagrid] container exists on the page → fire (relaxed gate per Review pass 2
//     decision #3b: AC1's "one-keystroke" affordance shouldn't require a prior Tab).
//   - otherwise, scope-gate to focus inside a [data-fc-datagrid] container.
export function isFocusWithinDataGrid() {
    const active = document.activeElement;
    if (isEditableElement(active)) {
        return false;
    }
    if (!active || active === document.body || active === document.documentElement) {
        return document.querySelector("[data-fc-datagrid]") !== null;
    }
    return active.closest("[data-fc-datagrid]") !== null;
}

export function focusFirstColumnFilter(viewKey) {
    if (!viewKey) {
        return false;
    }
    const escaper =
        typeof CSS !== "undefined" && typeof CSS.escape === "function"
            ? CSS.escape
            : (v) => v.replace(/"/g, '\\"');
    const selector =
        '[data-fc-datagrid="' +
        escaper(viewKey) +
        '"] [data-testid="fc-column-filter"] input';
    const input = document.querySelector(selector);
    if (input && typeof input.focus === "function") {
        input.focus();
        return true;
    }
    return false;
}

export function activeDataGridViewKey() {
    const active = document.activeElement;
    if (!active) {
        return null;
    }
    const container = active.closest("[data-fc-datagrid]");
    return container ? container.getAttribute("data-fc-datagrid") : null;
}

export function focusSolePageSearch() {
    if (isEditableElement(document.activeElement)) {
        return false;
    }

    const currentRoute = Array.from(document.querySelectorAll('[data-fc-route-content]'))
        .find((container) => {
            const rendered = new URL(container.getAttribute('data-fc-route-content'), document.baseURI);
            return rendered.pathname === window.location.pathname && rendered.search === window.location.search;
        });
    if (!currentRoute || currentRoute.querySelector('[data-fc-route-authorizing="true"]')) {
        return false;
    }

    const candidates = Array.from(currentRoute.querySelectorAll('#fc-main-content [data-fc-page-search], [data-fc-page-search]'))
        .map((host) => ({ host, input: host.shadowRoot?.querySelector('input') ?? host.querySelector('input') }))
        .filter(({ host, input }) => {
            if (!(input instanceof HTMLInputElement) || input.disabled || input.hidden || host.hasAttribute('disabled')) {
                return false;
            }
            const toolbar = host.closest('[data-fc-page-toolbar]');
            if (!toolbar) {
                return false;
            }
            const style = getComputedStyle(host);
            return style.display !== 'none' && style.visibility !== 'hidden' && host.getClientRects().length > 0;
        });
    if (candidates.length !== 1) {
        return false;
    }

    candidates[0].input.focus({ preventScroll: true });
    candidates[0].host.scrollIntoView({ block: 'nearest' });
    return true;
}
