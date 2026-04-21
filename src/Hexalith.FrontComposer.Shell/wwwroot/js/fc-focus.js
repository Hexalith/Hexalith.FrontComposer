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
