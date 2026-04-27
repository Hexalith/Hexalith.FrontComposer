using System.Diagnostics;

using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.State.ProjectionConnection;

/// <summary>EventStore projection hub connectivity states surfaced to Shell components.</summary>
public enum ProjectionConnectionStatus {
    Connected,
    Reconnecting,
    Disconnected,
}

/// <summary>Immutable public snapshot of the current EventStore projection connection state.</summary>
/// <param name="Status">Current connection status.</param>
/// <param name="LastTransitionAt">UTC timestamp of the latest transition.</param>
/// <param name="ReconnectAttempt">Current reconnect attempt count, if known.</param>
/// <param name="LastFailureCategory">Bounded non-sensitive failure category.</param>
public sealed record ProjectionConnectionSnapshot(
    ProjectionConnectionStatus Status,
    DateTimeOffset LastTransitionAt,
    int ReconnectAttempt,
    string? LastFailureCategory) {
    /// <summary>Gets a value indicating whether realtime projection nudges are unavailable.</summary>
    public bool IsDisconnected => Status is ProjectionConnectionStatus.Reconnecting or ProjectionConnectionStatus.Disconnected;
}

/// <summary>Connection-state transition produced by the EventStore hub wrapper/subscription service.</summary>
/// <param name="Status">New status.</param>
/// <param name="FailureCategory">Bounded non-sensitive category for logging and UI diagnostics.</param>
/// <param name="ReconnectAttempt">Reconnect attempt count, if known.</param>
public sealed record ProjectionConnectionTransition(
    ProjectionConnectionStatus Status,
    string? FailureCategory = null,
    int ReconnectAttempt = 0);

/// <summary>Scoped read/subscribe API for projection connection state.</summary>
public interface IProjectionConnectionState {
    ProjectionConnectionSnapshot Current { get; }

    IDisposable Subscribe(Action<ProjectionConnectionSnapshot> handler, bool replay = true);

    void Apply(ProjectionConnectionTransition transition);
}

/// <summary>Scoped per-circuit projection connection state service.</summary>
public sealed class ProjectionConnectionStateService(
    TimeProvider timeProvider,
    ILogger<ProjectionConnectionStateService> logger) : IProjectionConnectionState {
    private readonly object _sync = new();
    private readonly List<Action<ProjectionConnectionSnapshot>> _handlers = [];
    private readonly Dictionary<string, ConnectionLogBucket> _logBuckets = new(StringComparer.Ordinal);
    private ProjectionConnectionSnapshot _current = new(
        ProjectionConnectionStatus.Connected,
        timeProvider.GetUtcNow(),
        ReconnectAttempt: 0,
        LastFailureCategory: null);

    /// <inheritdoc />
    public ProjectionConnectionSnapshot Current {
        get {
            lock (_sync) {
                return _current;
            }
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe(Action<ProjectionConnectionSnapshot> handler, bool replay = true) {
        ArgumentNullException.ThrowIfNull(handler);

        // P14 — invoke replay under the lock so a concurrent Apply that runs between
        // add-to-handlers and replay cannot deliver fresh-then-stale ordering. The lock is
        // re-entrant by virtue of running on the same thread; subscribers must not call back
        // into Apply/Subscribe inside their replay handler.
        lock (_sync) {
            _handlers.Add(handler);
            if (replay) {
                InvokeSafe(handler, _current);
            }
        }

        return new Subscription(this, handler);
    }

    /// <inheritdoc />
    public void Apply(ProjectionConnectionTransition transition) {
        ArgumentNullException.ThrowIfNull(transition);

        Action<ProjectionConnectionSnapshot>[] handlers;
        ProjectionConnectionSnapshot snapshot;
        lock (_sync) {
            // P6 — accumulate ReconnectAttempt across consecutive Reconnecting transitions.
            // The transition's ReconnectAttempt is used as a lower bound so first-attempt
            // callers can pass 1; subsequent Reconnecting events without Connected in between
            // increment from the current value.
            int attempt = transition.Status switch {
                ProjectionConnectionStatus.Connected => 0,
                ProjectionConnectionStatus.Reconnecting when _current.Status is ProjectionConnectionStatus.Reconnecting
                    => Math.Max(_current.ReconnectAttempt + 1, Math.Max(1, transition.ReconnectAttempt)),
                _ => Math.Max(0, transition.ReconnectAttempt),
            };

            snapshot = new ProjectionConnectionSnapshot(
                transition.Status,
                timeProvider.GetUtcNow(),
                attempt,
                BoundCategory(transition.FailureCategory));

            // P9 — short-circuit when no logical change occurred (status / attempt / category).
            // Excludes LastTransitionAt because it always differs and would defeat the dedupe.
            if (IsSameLogicalState(_current, snapshot)) {
                return;
            }

            _current = snapshot;
            handlers = [.. _handlers];
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

        // P7 — wrap handler invocations: a single throwing subscriber must not skip the rest
        // of the chain or escalate up the SignalR dispatcher. Failures are logged at warning
        // with the redacted exception type only (no payload/tenant data).
        foreach (Action<ProjectionConnectionSnapshot> handler in handlers) {
            InvokeSafe(handler, snapshot);
        }
    }

    private static bool IsSameLogicalState(ProjectionConnectionSnapshot a, ProjectionConnectionSnapshot b)
        => a.Status == b.Status
            && a.ReconnectAttempt == b.ReconnectAttempt
            && string.Equals(a.LastFailureCategory, b.LastFailureCategory, StringComparison.Ordinal);

    private void InvokeSafe(Action<ProjectionConnectionSnapshot> handler, ProjectionConnectionSnapshot snapshot) {
        try {
            handler(snapshot);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) {
            logger.LogWarning(
                "Projection connection state subscriber threw. FailureCategory={FailureCategory}",
                ex.GetType().Name);
        }
    }

    private void Unsubscribe(Action<ProjectionConnectionSnapshot> handler) {
        lock (_sync) {
            _ = _handlers.Remove(handler);
        }
    }

    private int ShouldLogConnectionTransition(ProjectionConnectionSnapshot snapshot, out bool emitLog) {
        if (snapshot.Status is ProjectionConnectionStatus.Connected or ProjectionConnectionStatus.Disconnected) {
            lock (_sync) {
                int suppressed = _logBuckets.Values.Sum(static bucket => bucket.SuppressedCount);
                _logBuckets.Clear();
                emitLog = true;
                return suppressed;
            }
        }

        string key = string.Concat(snapshot.Status, "|", snapshot.LastFailureCategory ?? "none");
        DateTimeOffset now = timeProvider.GetUtcNow();
        lock (_sync) {
            if (!_logBuckets.TryGetValue(key, out ConnectionLogBucket? bucket)
                || now - bucket.WindowStartedAt >= TimeSpan.FromSeconds(30)) {
                int suppressed = bucket?.SuppressedCount ?? 0;
                _logBuckets[key] = new ConnectionLogBucket(now, 0);
                emitLog = true;
                return suppressed;
            }

            bucket.SuppressedCount++;
            emitLog = false;
            return bucket.SuppressedCount;
        }
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

    private sealed class Subscription(ProjectionConnectionStateService owner, Action<ProjectionConnectionSnapshot> handler) : IDisposable {
        private int _disposed;

        public void Dispose() {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) {
                owner.Unsubscribe(handler);
            }
        }
    }

    private sealed class ConnectionLogBucket(DateTimeOffset windowStartedAt, int suppressedCount) {
        public DateTimeOffset WindowStartedAt { get; } = windowStartedAt;
        public int SuppressedCount { get; set; } = suppressedCount;
    }
}
