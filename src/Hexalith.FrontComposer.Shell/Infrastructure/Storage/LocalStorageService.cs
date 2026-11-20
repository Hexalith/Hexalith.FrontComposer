using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Threading.Channels;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Infrastructure.Storage;

/// <summary>
/// WASM- and Blazor-Server-compatible <see cref="IStorageService"/> backed by the browser
/// <c>localStorage</c> API via <see cref="IJSRuntime"/> (Story 3-1 D9 / D15 / D16 / D18 / AC6).
/// </summary>
/// <remarks>
/// <para>
/// <b>Fire-and-forget writes:</b> <see cref="SetAsync{T}"/> enqueues a <see cref="PendingWrite"/>
/// onto an unbounded <see cref="Channel{T}"/>. A single drain worker consumes the channel and
/// issues one <c>localStorage.setItem</c> JS interop call at a time. Callers observe a completed
/// task immediately — the render thread is never blocked. <see cref="FlushAsync"/> enqueues a
/// sentinel carrying a <see cref="TaskCompletionSource"/> and awaits that TCS so the drain worker
/// reports the first write failure since the previous observed flush, or completes successfully.
/// Cancelled flush waits leave that failure available to the next caller. During disposal,
/// accepted writes have a five-second grace period before stalled interop is cancelled.
/// </para>
/// <para>
/// <b>LRU eviction:</b> a <see cref="ConcurrentDictionary{TKey,TValue}"/> tracks the last-access
/// timestamp (<see cref="TimeProvider.GetUtcNow()"/>.UtcTicks) per key. When
/// <see cref="FcShellOptions.LocalStorageMaxEntries"/> is exceeded, <see cref="EvictIfOverCap"/>
/// removes the entries with the oldest timestamps and emits remove-writes onto the same drain
/// channel. The scan is O(n) per insertion; n is capped at 10 000 by options validation.
/// </para>
/// <para>
/// <b>Concurrency:</b> <c>ConcurrentDictionary</c> is used (not <c>Dictionary</c>) per Pre-mortem
/// Analysis #2 — Blazor Server circuit async continuations can interleave <see cref="SetAsync{T}"/>
/// calls, and a non-concurrent dictionary mutation during the eviction scan would throw
/// <see cref="InvalidOperationException"/>.
/// </para>
/// </remarks>
public sealed class LocalStorageService : IStorageService, IAsyncDisposable {
    /// <summary>Internal sentinel key that flags a <see cref="FlushAsync"/> drain request.</summary>
    internal const string SentinelKey = "\0fc-flush\0";

    /// <summary>
    /// Shared <see cref="JsonSerializerOptions"/> used by every read / write through this service.
    /// Exposed <c>internal</c> so schema-lock tests pin the actual production wire format rather
    /// than the default serializer (Story 3-6 Review Finding F-AA-001 / F-BH-003).
    /// </summary>
    internal static JsonSerializerOptions SchemaLockJsonOptions { get; } = FcJson.StorageCompact;

    // Give accepted writes a chance to finish before cancelling a disconnected circuit's outstanding JS call.
    private static readonly TimeSpan ShutdownDrainTimeout = TimeSpan.FromSeconds(5);

    private readonly IJSRuntime _js;
    private readonly FcShellOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<LocalStorageService> _logger;
    private readonly ConcurrentDictionary<string, long> _lruTimestamps = new(StringComparer.Ordinal);
    private readonly Channel<PendingWrite> _writes = Channel.CreateUnbounded<PendingWrite>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _drainTask;
    private Exception? _pendingDrainFailure;
    private int _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalStorageService"/> class.
    /// </summary>
    /// <param name="js">The JS runtime used to reach <c>window.localStorage</c>.</param>
    /// <param name="options">Accessor for the shell options (LRU cap lives on <see cref="FcShellOptions"/>).</param>
    /// <param name="time">The time provider used to timestamp LRU entries.</param>
    /// <param name="logger">Logger for drain-failure + deserialization diagnostics.</param>
    public LocalStorageService(
        IJSRuntime js,
        IOptions<FcShellOptions> options,
        TimeProvider time,
        ILogger<LocalStorageService> logger) {
        ArgumentNullException.ThrowIfNull(js);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(time);
        ArgumentNullException.ThrowIfNull(logger);

        _js = js;
        _options = options.Value;
        _time = time;
        _logger = logger;
        _drainTask = Task.Run(() => DrainLoopAsync(_cts.Token));
    }

    /// <summary>Gets the number of keys currently tracked by the LRU eviction set. Used by tests.</summary>
    internal int TrackedKeyCount => _lruTimestamps.Count;

    /// <inheritdoc />
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = "IStorageService callers persist Fluxor state records with preserved members (ThemeValue, DensityLevel, DataGridNavigationState); the interface's generic T is the contract authorising the reflective path.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "BL0016", Justification = "Storage read failures belong to IStorageService callers; preserving the failed task lets each consumer select its existing recovery policy.")]
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrEmpty(key);
        string? json;
        try {
            json = await _js.InvokeAsync<string?>("localStorage.getItem", cancellationToken, key)
                .ConfigureAwait(false);
        }
        catch (JSException) {
            cancellationToken.ThrowIfCancellationRequested();
            throw;
        }
        if (json is null) {
            _ = _lruTimestamps.TryRemove(key, out _);
            return default;
        }

        _lruTimestamps[key] = _time.GetUtcNow().UtcTicks;

        try {
            return JsonSerializer.Deserialize<T>(json, SchemaLockJsonOptions);
        }
        catch (JsonException ex) {
            FrontComposerWarningLog.LocalStorageDeserializeFailed(_logger, key, ex);
            return default;
        }
    }

    /// <inheritdoc />
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = "IStorageService callers persist Fluxor state records with preserved members (ThemeValue, DensityLevel, DataGridNavigationState); the interface's generic T is the contract authorising the reflective path.")]
    public Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        _lruTimestamps[key] = _time.GetUtcNow().UtcTicks;
        EvictIfOverCap();
        string json = JsonSerializer.Serialize(value, SchemaLockJsonOptions);
        ObjectDisposedException.ThrowIf(!_writes.Writer.TryWrite(new PendingWrite(key, json, FlushSignal: null)), this);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        _ = _lruTimestamps.TryRemove(key, out _);
        ObjectDisposedException.ThrowIf(!_writes.Writer.TryWrite(new PendingWrite(key, SerializedValue: null, FlushSignal: null)), this);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "BL0016", Justification = "Storage enumeration failures belong to IStorageService callers; returning an empty inventory here would incorrectly report successful storage access.")]
    public async Task<IReadOnlyList<string>> GetKeysAsync(string prefix, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(prefix);
        // D16: single round-trip — ask the browser for the full key set and filter on the .NET side.
        string[] keys;
        try {
            keys = await _js.InvokeAsync<string[]>(
                "eval",
                cancellationToken,
                "Object.keys(window.localStorage)").ConfigureAwait(false);
        }
        catch (JSException) {
            cancellationToken.ThrowIfCancellationRequested();
            throw;
        }
        return [.. keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal))];
    }

    /// <inheritdoc />
    public async Task FlushAsync(CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        TaskCompletionSource signal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenRegistration registration = cancellationToken.Register(() => signal.TrySetCanceled(cancellationToken));
        if (_writes.Writer.TryWrite(new PendingWrite(SentinelKey, SerializedValue: null, FlushSignal: signal))) {
            await signal.Task.ConfigureAwait(false);
            return;
        }

        // The closed channel is itself a flush boundary. A racing caller must wait for its accepted writes.
        await _drainTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        if (_pendingDrainFailure is { } failure) {
            ExceptionDispatchInfo.Throw(failure);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) {
            await _drainTask.ConfigureAwait(false);
            return;
        }

        _ = _writes.Writer.TryComplete();
        try {
            using CancellationTokenSource deadline = new(ShutdownDrainTimeout, _time);
            using CancellationTokenRegistration registration = deadline.Token.Register(_cts.Cancel);
            await _drainTask.ConfigureAwait(false);
        }
        finally {
            _cts.Dispose();
        }
    }

    private void EvictIfOverCap() {
        int cap = _options.LocalStorageMaxEntries;
        while (_lruTimestamps.Count > cap) {
            string? oldestKey = null;
            long oldestTimestamp = long.MaxValue;
            foreach (KeyValuePair<string, long> kvp in _lruTimestamps) {
                if (kvp.Value < oldestTimestamp) {
                    oldestTimestamp = kvp.Value;
                    oldestKey = kvp.Key;
                }
            }

            if (oldestKey is null || !_lruTimestamps.TryRemove(oldestKey, out _)) {
                // Concurrent remove raced us — the dictionary shrank by itself; loop continues.
                return;
            }

            // Emit a remove-write onto the drain so localStorage stays in sync with our LRU view.
            _ = _writes.Writer.TryWrite(new PendingWrite(oldestKey, SerializedValue: null, FlushSignal: null));
        }
    }

    private async Task DrainLoopAsync(CancellationToken cancellationToken) {
        try {
            await foreach (PendingWrite write in _writes.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)) {
                if (write.FlushSignal is { } signal) {
                    if (_pendingDrainFailure is { } failure) {
                        // A caller that cancelled its wait has not observed this failure.
                        if (signal.TrySetException(failure)) {
                            _pendingDrainFailure = null;
                        }
                    }
                    else {
                        _ = signal.TrySetResult();
                    }

                    continue;
                }

                try {
                    if (write.SerializedValue is null) {
                        await _js.InvokeVoidAsync("localStorage.removeItem", cancellationToken, write.Key)
                            .ConfigureAwait(false);
                    }
                    else {
                        await _js.InvokeVoidAsync("localStorage.setItem", cancellationToken, write.Key, write.SerializedValue)
                            .ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                    throw;
                }
                catch (Exception ex) {
                    _pendingDrainFailure ??= ex;
                    FrontComposerWarningLog.LocalStorageDrainWriteFailed(_logger, write.Key, ex);
                }
            }
        }
        catch (Exception ex) {
            _pendingDrainFailure = ex;
            _ = _writes.Writer.TryComplete(ex);
            while (_writes.Reader.TryRead(out PendingWrite pending)) {
                _ = pending.FlushSignal?.TrySetException(ex);
            }

            if (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested) {
                throw;
            }
        }
    }
}
