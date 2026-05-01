namespace Hexalith.FrontComposer.Shell.Services.DevMode;

/// <summary>
/// Development-only clipboard JS module boundary.
/// </summary>
public interface IClipboardJSModule : IAsyncDisposable {
    /// <summary>Copies text to the browser clipboard.</summary>
    ValueTask<ClipboardCopyResult> CopyToClipboardAsync(string text, CancellationToken cancellationToken = default);
}
