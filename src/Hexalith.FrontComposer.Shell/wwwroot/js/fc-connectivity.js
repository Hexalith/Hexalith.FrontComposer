const listeners = new Map();
let nextId = 1;

export function isOnline() {
    return navigator.onLine;
}

export function watchConnectivity(dotNet, host, offlineMessage) {
    const id = nextId++;
    const binding = host?.getAttribute("data-fc-connectivity-binding");
    const scope = host?.closest(".fc-shell-root") ?? host?.parentElement;
    const stale = new Map();
    let observation = 0;
    let publication;
    let serverRevision;
    let browserOffline = !navigator.onLine;
    let offlineEpisode = browserOffline ? 1 : 0;

    const current = () => !host || (host.isConnected
        && host.getAttribute("data-fc-connectivity-binding") === binding
        && scope?.contains(host));
    const publish = (message, identity) => {
        const target = host?.querySelector("[data-fc-browser-status]");
        if (target && identity !== publication) {
            publication = identity;
            target.replaceChildren(document.createTextNode(message));
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
        if (browserOffline) {
            publish(offlineMessage, `offline:${offlineEpisode}`);
            serverRevision = revision;
        } else if (revision !== serverRevision) {
            serverRevision = revision;
            // An offline callback can arrive after online evidence. It cannot resurrect AM-30.
            if (message !== offlineMessage) {
                publish(message, `server:${revision}`);
            }
        }
        for (const label of scope.querySelectorAll("[data-fc-browser-stale]")) {
            const identity = label.getAttribute("data-fc-cached-result");
            if (browserOffline) {
                stale.set(label, identity);
            } else if (stale.has(label) && stale.get(label) !== identity) {
                // Only a matching data read changes this marker; online alone is not freshness.
                stale.delete(label);
            }
            const visible = stale.has(label) || label.getAttribute("data-fc-server-stale") === "true";
            label.hidden = !visible;
            if (visible) {
                label.setAttribute("data-testid", "fc-projection-stale-notice");
            } else {
                label.removeAttribute("data-testid");
            }
        }
        for (const label of stale.keys()) {
            if (!scope.contains(label)) {
                stale.delete(label);
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
        }
        if (wasOffline && !browserOffline) {
            publish("", `online:${observation}`);
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
        attributeFilter: ["data-fc-status-revision", "data-fc-status-message", "data-fc-cached-result", "data-fc-server-stale"] });
    listeners.set(id, { online, offline, observer });
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
}
