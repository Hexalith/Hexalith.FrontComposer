const listeners = new Map();
let nextId = 1;

export function isOnline() {
    return navigator.onLine;
}

export function watchConnectivity(dotNet) {
    const id = nextId++;
    const online = () => dotNet.invokeMethodAsync("OnBrowserConnectivityChanged", true);
    const offline = () => dotNet.invokeMethodAsync("OnBrowserConnectivityChanged", false);
    window.addEventListener("online", online);
    window.addEventListener("offline", offline);
    listeners.set(id, { online, offline });
    return id;
}

export function unwatchConnectivity(id) {
    const listener = listeners.get(id);
    if (!listener) {
        return;
    }

    window.removeEventListener("online", listener.online);
    window.removeEventListener("offline", listener.offline);
    listeners.delete(id);
}
