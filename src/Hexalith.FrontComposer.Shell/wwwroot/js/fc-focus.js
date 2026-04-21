function runAfterDismiss(callback) {
    if (typeof requestAnimationFrame === "function") {
        requestAnimationFrame(() => callback());
        return;
    }

    setTimeout(callback, 0);
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
                document.body.focus({ preventScroll: true });
            }
        }
    });
}
