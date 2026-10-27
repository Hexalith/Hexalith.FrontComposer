// No payload is logged or persisted. Each operation owns its temporary browser resources.
const pending = new Map();

export function beginDownload(operationId) {
    pending.set(operationId, { cancelled: false });
}

export function cancelDownload(operationId) {
    const operation = pending.get(operationId);
    if (operation) operation.cancelled = true;
    pending.delete(operationId);
}

export async function downloadStream(operationId, fileName, contentType, streamReference) {
    const operation = pending.get(operationId);
    if (!operation || operation.cancelled) return;
    let anchor;
    let url;
    try {
        const bytes = await streamReference.arrayBuffer();
        if (operation.cancelled) return;
        const blob = new Blob([bytes], { type: contentType });
        url = URL.createObjectURL(blob);
        anchor = document.createElement("a");
        anchor.href = url;
        anchor.download = fileName;
        document.body.appendChild(anchor);
        if (!operation.cancelled) anchor.click();
    } finally {
        anchor?.remove();
        if (url) URL.revokeObjectURL(url);
        pending.delete(operationId);
    }
}
