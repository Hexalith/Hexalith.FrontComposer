using System.Diagnostics;

using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.State.ProjectionConnection;

/// <summary>Scoped per-circuit projection connection state service.</summary>
/// <remarks>
/// Story 11.15 (M19 cluster 5): the handler-list / current-and-replay / fault-isolation /
/// idempotent-unsubscribe mechanics are delegated to the shared <see cref="SnapshotPublisher{T}"/>
/// primitive. This service retains its distinct semantics: reconnect-attempt accumulation (P6),
/// logical-state dedup (P9), rate-limited transition logging with 30-second buckets + F16 eviction,
/// telemetry, sensitive-value bounding, and the F07 dispose flush.
/// </remarks>
public sealed class ProjectionConnectionStateService(
    TimeProvider timeProvider,
    ILogger<ProjectionConnectionStateService> logger) : IProjectionConnectionState, IDisposable, IAsyncDisposable {
    /// <summary>F16 — cap on distinct bucket keys to prevent unbounded growth under churning
    /// failure categories. When the cap is hit, the oldest bucket is evicted (its suppressed
    /// count is preserved on the next visible log via the closing flush).</summary>
    private const int MaxLogBuckets = 16;

    /// <summary>Guards the log-suppression buckets only; the subscriber list / current snapshot are
    /// owned by <see cref="_publisher"/>.</summary>
    private readonly object _logSync = new();
    private readonly Dictionary<string, ConnectionLogBucket> _logBuckets = new(StringComparer.Ordinal);
    private readonly SnapshotPublisher<ProjectionConnectionSnapshot> _publisher = new(
        new ProjectionConnectionSnapshot(
            ProjectionConnectionStatus.Connected,
            timeProvider.GetUtcNow(),
            ReconnectAttempt: 0,
            LastFailureCategory: null),
        ex => FrontComposerHotPathLog.ProjectionStateSubscriberFailed(logger, ex.GetType().Name));

    private int _disposed;

    /// <inheritdoc />
    public ProjectionConnectionSnapshot Current => _publisher.Current;

    /// <inheritdoc />
    public IDisposable Subscribe(Action<ProjectionConnectionSnapshot> handler, bool replay = true)
        => _publisher.Subscribe(handler, replay);

    /// <inheritdoc />
    public void Apply(ProjectionConnectionTransition transition) {
        ArgumentNullException.ThrowIfNull(transition);

        // P6 / P9 — reconnect-attempt accumulation + logical-state dedup are evaluated atomically
        // against the current snapshot under the publisher's lock (returning null short-circuits when
        // no logical change occurred). Logging/telemetry then run between the state advance and the
        // fan-out (Deliver), preserving the original "mutate → log → fan-out" ordering.
        if (!_publisher.TryApply(
                current => {
                    // P6 — accumulate ReconnectAttempt across consecutive Reconnecting transitions.
                    // The transition's ReconnectAttempt is used as a lower bound so first-attempt
                    // callers can pass 1; subsequent Reconnecting events without Connected in between
                    // increment from the current value.
                    int attempt = transition.Status switch {
                        ProjectionConnectionStatus.Connected => 0,
                        ProjectionConnectionStatus.Reconnecting when current.Status is ProjectionConnectionStatus.Reconnecting
                            => Math.Max(current.ReconnectAttempt + 1, Math.Max(1, transition.ReconnectAttempt)),
                        _ => Math.Max(0, transition.ReconnectAttempt),
                    };

                    ProjectionConnectionSnapshot candidate = new(
                        transition.Status,
                        timeProvider.GetUtcNow(),
                        attempt,
                        BoundCategory(transition.FailureCategory));

                    // P9 — short-circuit when no logical change occurred (status / attempt / category).
                    // Excludes LastTransitionAt because it always differs and would defeat the dedupe.
                    return IsSameLogicalState(current, candidate) ? null : candidate;
                },
                out ProjectionConnectionSnapshot snapshot,
                out Action<ProjectionConnectionSnapshot>[] handlers)) {
            return;
        }

        int suppressedCount = ShouldLogConnectionTransition(snapshot, out bool emitLog);
        using Activity? activity = FrontComposerTelemetry.StartProjectionConnectionTransition(
            snapshot.Status.ToString(),
            snapshot.LastFailureCategory,
            snapshot.ReconnectAttempt,
            suppressedCount);
        if (emitLog) {
            FrontComposerLog.ProjectionConnectionChanged(
                logger,
                snapshot.Status.ToString(),
                snapshot.ReconnectAttempt,
                snapshot.LastFailureCategory ?? "none",
                suppressedCount);
        }

        // P7 — a single throwing subscriber must not skip the rest of the chain or escalate up the
        // SignalR dispatcher; the primitive's Deliver applies per-handler fault isolation.
        _publisher.Deliver(handlers, snapshot);
    }

    private static bool IsSameLogicalState(ProjectionConnectionSnapshot a, ProjectionConnectionSnapshot b)
        => a.Status == b.Status
            && a.ReconnectAttempt == b.ReconnectAttempt
            && string.Equals(a.LastFailureCategory, b.LastFailureCategory, StringComparison.Ordinal);

    private int ShouldLogConnectionTransition(ProjectionConnectionSnapshot snapshot, out bool emitLog) {
        if (snapshot.Status is ProjectionConnectionStatus.Connected or ProjectionConnectionStatus.Disconnected) {
            int suppressed = 0;
            lock (_logSync) {
                // F15 — sum + clear under lock without LINQ allocation. The previous
                // _logBuckets.Values.Sum(...) call allocated an enumerator on every Connected
                // or Disconnected transition (the chatty branch under reconnect storms).
                foreach (ConnectionLogBucket bucket in _logBuckets.Values) {
                    suppressed += bucket.SuppressedCount;
                }

                _logBuckets.Clear();
            }

            emitLog = true;
            return suppressed;
        }

        string key = string.Concat(snapshot.Status, "|", snapshot.LastFailureCategory ?? "none");
        DateTimeOffset now = timeProvider.GetUtcNow();
        lock (_logSync) {
            if (!_logBuckets.TryGetValue(key, out ConnectionLogBucket? bucket)
                || now - bucket.WindowStartedAt >= TimeSpan.FromSeconds(30)) {
                int suppressed = bucket?.SuppressedCount ?? 0;
                // F16 — evict the oldest bucket when the cap is hit so churning failure
                // categories cannot grow the dictionary unbounded. The evicted bucket's
                // suppressed count is folded into the about-to-emit window so operators
                // never lose the suppression signal.
                if (bucket is null && _logBuckets.Count >= MaxLogBuckets) {
                    KeyValuePair<string, ConnectionLogBucket> oldest = default;
                    DateTimeOffset oldestStart = DateTimeOffset.MaxValue;
                    foreach (KeyValuePair<string, ConnectionLogBucket> entry in _logBuckets) {
                        if (entry.Value.WindowStartedAt < oldestStart) {
                            oldestStart = entry.Value.WindowStartedAt;
                            oldest = entry;
                        }
                    }

                    if (oldest.Key is not null) {
                        suppressed += oldest.Value.SuppressedCount;
                        _ = _logBuckets.Remove(oldest.Key);
                    }
                }

                _logBuckets[key] = new ConnectionLogBucket(now, 0);
                emitLog = true;
                return suppressed;
            }

            bucket.SuppressedCount++;
            emitLog = false;
            return bucket.SuppressedCount;
        }
    }

    /// <summary>F07 — flush the suppression-count totals on circuit/process teardown so
    /// operators never see a "1 reconnect log, then silence" pattern when the host disposes
    /// before the next visible Connected/Disconnected transition.</summary>
    public void Dispose() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) {
            return;
        }

        (ProjectionConnectionSnapshot Current, int Suppressed) flush = _publisher.ReadCurrent(current => {
            lock (_logSync) {
                int suppressed = 0;
                foreach (ConnectionLogBucket bucket in _logBuckets.Values) {
                    suppressed += bucket.SuppressedCount;
                }

                _logBuckets.Clear();
                return (current, suppressed);
            }
        });

        if (flush.Suppressed > 0) {
            FrontComposerLog.ProjectionConnectionChanged(
                logger,
                flush.Current.Status.ToString(),
                flush.Current.ReconnectAttempt,
                flush.Current.LastFailureCategory ?? "none",
                flush.Suppressed);
        }
    }

    public ValueTask DisposeAsync() {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private static string? BoundCategory(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        ReadOnlySpan<char> span = value.AsSpan().Trim();
        Span<char> buffer = stackalloc char[Math.Min(span.Length, 48)];
        int written = 0;
        foreach (char ch in span) {
            if (written >= buffer.Length) {
                break;
            }

            buffer[written++] = char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_';
        }

        return new string(buffer[..written]);
    }

    private sealed class ConnectionLogBucket(DateTimeOffset windowStartedAt, int suppressedCount) {
        public DateTimeOffset WindowStartedAt { get; } = windowStartedAt;
        public int SuppressedCount { get; set; } = suppressedCount;
    }
}
