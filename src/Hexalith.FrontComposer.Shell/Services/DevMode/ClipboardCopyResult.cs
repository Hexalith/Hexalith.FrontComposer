namespace Hexalith.FrontComposer.Shell.Services.DevMode;

/// <summary>
/// Clipboard copy outcomes surfaced by the dev-mode overlay.
/// </summary>
public enum ClipboardCopyResult {
    /// <summary>Copy succeeded.</summary>
    Success,

    /// <summary>The browser denied clipboard access.</summary>
    Denied,

    /// <summary>Clipboard APIs were unavailable.</summary>
    Unavailable,

    /// <summary>Copy failed for an unexpected JS or interop reason.</summary>
    Failed,

    /// <summary>Copy timed out.</summary>
    TimedOut,
}
