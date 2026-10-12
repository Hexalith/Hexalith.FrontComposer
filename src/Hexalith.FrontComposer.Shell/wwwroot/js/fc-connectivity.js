const listeners = new Map();
const bindings = new WeakMap();
let nextId = 1;

function freshnessForDisplayedPage(identity) {
    if (identity?.startsWith("pages:")) {
        const [pageText, ...versions] = identity.slice(6).split("|");
        const page = Number(pageText);
        if (Number.isInteger(page)) {
            const match = versions.find(entry => Number(entry.split(":", 1)[0]) === page);
            return { page, version: Number(match?.split(":")[1] ?? 0) };
        }
    }
    return identity;
}

function pageVersions(identity) {
    const versions = new Map();
    if (identity?.startsWith("pages:")) {
        for (const entry of identity.slice(6).split("|").slice(1)) {
            const [page, version] = entry.split(":").map(Number);
            if (Number.isInteger(page) && Number.isFinite(version)) {
                versions.set(page, version);
            }
        }
    }
    return versions;
}

function matchingFreshRead(previous, identity) {
    if (previous && typeof previous === "object") {
        if (!identity?.startsWith("pages:")) { return false; }
        return identity.slice(6).split("|").slice(1).some(entry => {
            const [page, version] = entry.split(":");
            return Number(page) === previous.page && Number(version) > previous.version;
        });
    }
    return previous !== identity;
}

export function isOnline() {
    return navigator.onLine;
}

export function watchConnectivity(dotNet, host, offlineMessage) {
    const binding = host?.getAttribute("data-fc-connectivity-binding");
    const existing = host && bindings.get(host);
    if (existing && existing.binding === binding && listeners.has(existing.id)) {
        return existing.id;
    }
    if (existing) {
        unwatchConnectivity(existing.id);
    }
    const id = nextId++;
    const scope = host?.closest(".fc-shell-root") ?? host?.parentElement;
    // A surface can host more than one mounted view. Each label owns its own
    // displayed-page baseline, and its stable owner survives a DOM replacement.
    const staleByOwner = new Map();
    let observation = 0;
    let publication;
    let serverRevision;
    let browserOffline = !navigator.onLine;
    let awaitingFreshServerEvidence = false;
    let offlineEpisode = browserOffline ? 1 : 0;
    const initialStatus = host?.querySelector("[data-fc-status-revision]");
    const initialCopy = initialStatus?.getAttribute("data-fc-status-message") ?? "";
    if (initialStatus?.querySelector("[data-fc-browser-status]")?.textContent === initialCopy) {
        // The Blazor-rendered fallback already reached the persistent node before setup.
        // Register its revision without replacing identical text and speaking it twice.
        serverRevision = initialStatus.getAttribute("data-fc-status-revision");
        publication = browserOffline && initialCopy === offlineMessage
            ? `offline:${offlineEpisode}` : `server:${serverRevision}`;
    }

    const current = () => !host || (host.isConnected
        && host.getAttribute("data-fc-connectivity-binding") === binding
        && scope?.contains(host));
    const publish = (message, identity, adoptRendered = false) => {
        const target = host?.querySelector("[data-fc-browser-status]");
        if (target && (identity !== publication || target.textContent !== message)) {
            publication = identity;
            if (!adoptRendered || target.textContent !== message) {
                target.replaceChildren(document.createTextNode(message));
            }
        }
    };
    const present = () => {
        if (!current()) {
            unwatchConnectivity(id);
            return;
        }
        if (!host) {
            return;
        }
        const offlineBar = host.querySelector("[data-fc-browser-offline]");
        if (offlineBar) {
            offlineBar.hidden = !browserOffline;
            offlineBar.querySelector("[data-testid]")?.setAttribute("data-testid",
                browserOffline ? "fc-projection-connection-status" : "fc-browser-offline-status");
        }
        for (const serverBar of host.querySelectorAll("[data-fc-server-connectivity]")) {
            serverBar.hidden = browserOffline;
        }
        const status = host.querySelector("[data-fc-status-revision]");
        const revision = status?.getAttribute("data-fc-status-revision");
        const message = status?.getAttribute("data-fc-status-message") ?? "";
        const serverFallback = status?.getAttribute("data-fc-status-fallback") === "true";
        if (browserOffline) {
            publish(offlineMessage, `offline:${offlineEpisode}`);
            serverRevision = revision;
        } else if (revision !== serverRevision) {
            serverRevision = revision;
            // The offline copy cannot return after online evidence. A non-empty server
            // message is restored on that transition even when it is not the fallback copy.
            if (message !== offlineMessage && (!awaitingFreshServerEvidence || serverFallback || message)) {
                publish(message, `server:${revision}`, true);
            }
            awaitingFreshServerEvidence = false;
        }
        const activeStaleOwners = new Set();
        for (const label of scope.querySelectorAll("[data-fc-browser-stale]")) {
            const identity = label.getAttribute("data-fc-cached-result");
            const surface = label.getAttribute("data-fc-stale-key") ?? "";
            const owner = label.getAttribute("data-fc-stale-owner") ?? surface;
            activeStaleOwners.add(owner);
            const scopeGeneration = label.getAttribute("data-fc-stale-scope") ?? "";
            let marker = staleByOwner.get(owner);
            if (marker && (marker.surface !== surface || marker.scope !== scopeGeneration)) {
                staleByOwner.delete(owner);
                marker = undefined;
            }
            const displayed = freshnessForDisplayedPage(identity);
            const previous = marker?.baseline;
            if (previous && typeof previous === "object" && displayed && typeof displayed === "object"
                && previous.page !== displayed.page) {
                // Keep the outage-time baseline for every cached page. A page read
                // while offscreen can already be fresh when it becomes visible.
                if (!marker.pageBaselines.has(displayed.page)) {
                    // This page had no successful read in the outage snapshot.
                    // Its first positive sequence therefore proves a later read.
                    marker.pageBaselines.set(displayed.page, 0);
                }
                marker.baseline = { page: displayed.page, version: marker.pageBaselines.get(displayed.page) };
            }
            if (browserOffline) {
                if (!marker) {
                    marker = { surface, scope: scopeGeneration, baseline: displayed,
                        pageBaselines: pageVersions(identity), clearedPages: new Set(), cleared: false };
                    staleByOwner.set(owner, marker);
                }
            } else if (marker && matchingFreshRead(marker.baseline, identity)) {
                // Only a matching data read changes this marker; online alone is not freshness.
                if (marker.baseline && typeof marker.baseline === "object") {
                    marker.clearedPages.add(marker.baseline.page);
                } else {
                    marker.cleared = true;
                }
            }
            const browserStale = !!marker && (browserOffline || (marker.baseline && typeof marker.baseline === "object"
                ? !marker.clearedPages.has(marker.baseline.page) : !marker.cleared));
            const visible = browserStale || label.getAttribute("data-fc-server-stale") === "true";
            label.hidden = !visible;
            if (visible) {
                label.setAttribute("data-testid", "fc-projection-stale-notice");
            } else {
                label.removeAttribute("data-testid");
            }
        }
        for (const owner of staleByOwner.keys()) {
            if (!activeStaleOwners.has(owner)) {
                staleByOwner.delete(owner);
            }
        }
    };
    const changed = () => {
        if (!current()) {
            unwatchConnectivity(id);
            return;
        }
        const wasOffline = browserOffline;
        browserOffline = !navigator.onLine;
        ++observation;
        if (!wasOffline && browserOffline) {
            ++offlineEpisode;
            staleByOwner.clear();
        }
        if (wasOffline && !browserOffline) {
            publish("", `online:${observation}`);
            awaitingFreshServerEvidence = true;
            // Offline presentation displaced the server message even when its revision stayed put.
            // Let present() restore the current non-empty server message on this same node.
            serverRevision = undefined;
        }
        present();
        dotNet.invokeMethodAsync("OnBrowserConnectivityObserved", !browserOffline, observation).catch(() => {});
    };
    const online = changed;
    const offline = changed;
    window.addEventListener("online", online);
    window.addEventListener("offline", offline);
    const observer = host && scope ? new MutationObserver(present) : null;
    observer?.observe(scope, { subtree: true, childList: true, attributes: true,
        attributeFilter: ["data-fc-status-revision", "data-fc-status-message", "data-fc-status-fallback", "data-fc-cached-result", "data-fc-server-stale", "data-fc-stale-scope", "data-fc-stale-key", "data-fc-stale-owner"] });
    listeners.set(id, { online, offline, observer, host, binding });
    if (host) {
        bindings.set(host, { id, binding });
    }
    host?.setAttribute("data-fc-connectivity-ready", "true");
    present();
    return id;
}

export function unwatchConnectivity(id) {
    const listener = listeners.get(id);
    if (!listener) {
        return;
    }

    window.removeEventListener("online", listener.online);
    window.removeEventListener("offline", listener.offline);
    listener.observer?.disconnect();
    listeners.delete(id);
    if (listener.host && bindings.get(listener.host)?.id === id) {
        bindings.delete(listener.host);
        listener.host.removeAttribute("data-fc-connectivity-ready");
    }
}

export function unwatchConnectivityBinding(host, binding) {
    const owned = host && bindings.get(host);
    if (owned?.binding === binding) {
        unwatchConnectivity(owned.id);
    }
}
