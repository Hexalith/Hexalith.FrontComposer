using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Channels;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Storage;

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
/// signals "everything before me has been flushed" without closing the channel.
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

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault,
    };

    /// <summary>
    /// Shared <see cref="JsonSerializerOptions"/> used by every read / write through this service.
    /// Exposed <c>internal</c> so schema-lock tests pin the actual production wire format rather
    /// than the default serializer (Story 3-6 Review Finding F-AA-001 / F-BH-003).
    /// </summary>
    internal static JsonSerializerOptions SchemaLockJsonOptions => JsonOptions;

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
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrEmpty(key);
        string? json = await _js.InvokeAsync<string?>("localStorage.getItem", cancellationToken, key)
            .ConfigureAwait(false);
        if (json is null) {
            _ = _lruTimestamps.TryRemove(key, out _);
            return default;
        }

        _lruTimestamps[key] = _time.GetUtcNow().UtcTicks;

        try {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException ex) {
            _logger.LogWarning(ex, "LocalStorageService: failed to deserialize value for key '{Key}'", key);
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
        _lruTimestamps[key] = _time.GetUtcNow().UtcTicks;
        EvictIfOverCap();
        string json = JsonSerializer.Serialize(value, JsonOptions);
        _ = _writes.Writer.TryWrite(new PendingWrite(key, json, FlushSignal: null));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrEmpty(key);
        _ = _lruTimestamps.TryRemove(key, out _);
        _ = _writes.Writer.TryWrite(new PendingWrite(key, SerializedValue: null, FlushSignal: null));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetKeysAsync(string prefix, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(prefix);
        // D16: single round-trip — ask the browser for the full key set and filter on the .NET side.
        string[] keys = await _js.InvokeAsync<string[]>(
            "eval",
            cancellationToken,
            "Object.keys(window.localStorage)").ConfigureAwait(false);
        return [.. keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal))];
    }

    /// <inheritdoc />
    public async Task FlushAsync(CancellationToken cancellationToken = default) {
        if (Volatile.Read(ref _disposed) != 0) {
            return;
        }

        TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_writes.Writer.TryWrite(new PendingWrite(SentinelKey, SerializedValue: null, FlushSignal: tcs))) {
            // Channel already closed (disposal in flight) — treat Flush as a no-op.
            return;
        }

        await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) {
            return;
        }

        _ = _writes.Writer.TryComplete();
        try {
            await _drainTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            // Normal on cancellation.
        }

        try {
            await _cts.CancelAsync().ConfigureAwait(false);
        }
        catch (ObjectDisposedException) {
            // Already disposed; safe to ignore.
        }

        _cts.Dispose();
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
                try {
                    if (write.FlushSignal is not null) {
                        if (_pendingDrainFailure is not null) {
                            Exception failure = _pendingDrainFailure;
                            _pendingDrainFailure = null;
                            _ = write.FlushSignal.TrySetException(failure);
                        }
                        else {
                            _ = write.FlushSignal.TrySetResult();
                        }

                        continue;
                    }

                    if (write.SerializedValue is null) {
                        await _js.InvokeVoidAsync("localStorage.removeItem", cancellationToken, write.Key)
                            .ConfigureAwait(false);
                    }
                    else {
                        await _js.InvokeVoidAsync("localStorage.setItem", cancellationToken, write.Key, write.SerializedValue)
                            .ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) {
                    throw;
                }
                catch (Exception ex) {
                    _pendingDrainFailure ??= ex;
                    _logger.LogWarning(ex, "LocalStorageService: drain write failed for key '{Key}'", write.Key);
                    write.FlushSignal?.TrySetException(ex);
                }
            }
        }
        catch (OperationCanceledException) {
            // Expected on disposal.
        }
    }
}

/// <summary>
/// A queued write awaiting the drain worker.
/// </summary>
/// <param name="Key">The localStorage key (or <see cref="LocalStorageService.SentinelKey"/> for a flush marker).</param>
/// <param name="SerializedValue">The JSON payload, or <see langword="null"/> to signal a remove.</param>
/// <param name="FlushSignal">When non-null, the drain worker completes this TCS once the record is observed (used by <see cref="LocalStorageService.FlushAsync"/>).</param>
internal readonly record struct PendingWrite(
    string Key,
    string? SerializedValue,
    TaskCompletionSource? FlushSignal);
