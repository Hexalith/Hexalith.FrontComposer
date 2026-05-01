using Hexalith.FrontComposer.Contracts;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Services.DevMode;

/// <summary>
/// Scoped JS-module wrapper for dev-mode starter-template clipboard copies.
/// </summary>
public sealed class ClipboardJSModule(
    IJSRuntime? jsRuntime = null,
    IOptions<FcShellOptions>? options = null,
    ILogger<ClipboardJSModule>? logger = null) : IClipboardJSModule {
    private const string ModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-devmode-clipboard.js";

    private readonly IJSRuntime? _jsRuntime = jsRuntime;
    private readonly FcShellOptions _options = options?.Value ?? new FcShellOptions();
    private readonly ILogger<ClipboardJSModule>? _logger = logger;
    private readonly object _sync = new();
    private Task<IJSObjectReference>? _moduleTask;
    private int _disposed;

    /// <inheritdoc />
    public async ValueTask<ClipboardCopyResult> CopyToClipboardAsync(string text, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(text);
        if (_jsRuntime is null || Volatile.Read(ref _disposed) == 1) {
            return ClipboardCopyResult.Unavailable;
        }

        // P12 — cap clipboard payload size to prevent JS-interop DOS via runaway starter emission.
        int maxBytes = _options.DevMode.MaxClipboardPayloadBytes;
        if (System.Text.Encoding.UTF8.GetByteCount(text) > maxBytes) {
            _logger?.LogInformation(
                "Dev-mode clipboard payload exceeded MaxClipboardPayloadBytes={MaxBytes}; copy rejected.",
                maxBytes);
            return ClipboardCopyResult.Failed;
        }

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(_options.DevMode.CopyTimeoutMilliseconds));

        try {
            IJSObjectReference? module = await GetModuleAsync(timeout.Token).ConfigureAwait(false);
            if (module is null) {
                return ClipboardCopyResult.Unavailable;
            }

            // P11 — JS module never throws; always returns a structured outcome string.
            string outcome = await module
                .InvokeAsync<string>("copyToClipboard", timeout.Token, text)
                .ConfigureAwait(false);

            ClipboardCopyResult result = MapOutcome(outcome);
            _logger?.LogInformation(
                "Dev-mode clipboard copy completed. Outcome={Outcome}",
                result);
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            _logger?.LogInformation("Dev-mode clipboard copy timed out.");
            return ClipboardCopyResult.TimedOut;
        }
        catch (JSDisconnectedException) {
            return ClipboardCopyResult.Failed;
        }
        catch (JSException ex) {
            // P11 — JS side categorizes outcomes; reaching this catch means the JS module itself
            // failed to load or threw a non-categorizable error. Treat as Failed without locale-
            // sensitive string sniffing.
            _logger?.LogInformation(
                "Dev-mode clipboard copy failed unexpectedly. ExceptionType={ExceptionType}",
                ex.GetType().FullName);
            return ClipboardCopyResult.Failed;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) {
            return;
        }

        Task<IJSObjectReference>? snapshot;
        lock (_sync) {
            snapshot = _moduleTask;
            _moduleTask = null;
        }

        if (snapshot is null) {
            return;
        }

        try {
            // P17 — IJSObjectReference.DisposeAsync drops the JS module reference without
            // requiring a separate disposeClipboard token plumbing. The previous token-based
            // dispose was dead code (JS Set never read).
            IJSObjectReference module = await snapshot.ConfigureAwait(false);
            await module.DisposeAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
        catch (InvalidOperationException) { }
    }

    private Task<IJSObjectReference> GetOrStartImport() {
        lock (_sync) {
            _moduleTask ??= _jsRuntime!.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
            return _moduleTask;
        }
    }

    private async ValueTask<IJSObjectReference?> GetModuleAsync(CancellationToken cancellationToken) {
        Task<IJSObjectReference> task = GetOrStartImport();
        try {
            return await task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch {
            lock (_sync) {
                if (ReferenceEquals(_moduleTask, task)) {
                    _moduleTask = null;
                }
            }

            throw;
        }
    }

    private static ClipboardCopyResult MapOutcome(string outcome)
        => outcome switch {
            "Success" => ClipboardCopyResult.Success,
            "Denied" => ClipboardCopyResult.Denied,
            "Unavailable" => ClipboardCopyResult.Unavailable,
            "TimedOut" => ClipboardCopyResult.TimedOut,
            _ => ClipboardCopyResult.Failed,
        };
}
