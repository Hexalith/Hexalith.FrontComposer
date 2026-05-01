// Dev-mode clipboard JS module. Never throws — every outcome is returned as a structured
// string so the C# side can map without locale-sensitive string sniffing.
export async function copyToClipboard(text) {
    if (!navigator.clipboard || typeof navigator.clipboard.writeText !== "function") {
        return "Unavailable";
    }

    try {
        await navigator.clipboard.writeText(text);
        return "Success";
    } catch (error) {
        const name = error && typeof error.name === "string" ? error.name : "";
        if (name === "NotAllowedError" || name === "SecurityError") {
            return "Denied";
        }

        return "Failed";
    }
}
