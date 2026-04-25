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
function isEditableInComposedPath(event) {
    if (typeof event.composedPath !== "function") {
        return isEditableElement(event.target);
    }

    const path = event.composedPath();
    for (const node of path) {
        if (node instanceof HTMLElement && isEditableElement(node)) {
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
    if (element && typeof element.focus === "function") {
        element.focus();
    }
}

export function isEditableElementActive() {
    return isEditableElement(document.activeElement);
}

export function registerShellKeyFilter(element) {
    if (!element || element.__fcShellKeyFilter) {
        return;
    }

    const handler = (event) => {
        const key = (event.key ?? "").toLowerCase();
        const hasModifier =
            event.ctrlKey || event.metaKey || event.shiftKey || event.altKey;

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
}

// P9 (2026-04-21 pass-3): teardown companions for the register* pair so hot-reload and
// circuit reconnect paths don't accumulate stale handlers on DOM nodes that get re-attached
// to new Blazor components. Callers invoke these from DisposeAsync via JS interop.
export function unregisterShellKeyFilter(element) {
    if (!element || !element.__fcShellKeyFilter) {
        return;
    }

    element.removeEventListener("keydown", element.__fcShellKeyFilter);
    element.__fcShellKeyFilter = null;
}

export function unregisterPaletteKeyFilter(element) {
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
