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
        ProjectionConnectionSnapshot snapshot;
        lock (_sync) {
            _handlers.Add(handler);
            snapshot = _current;
        }

        if (replay) {
            handler(snapshot);
        }

        return new Subscription(this, handler);
    }

    /// <inheritdoc />
    public void Apply(ProjectionConnectionTransition transition) {
        ArgumentNullException.ThrowIfNull(transition);
        ProjectionConnectionSnapshot snapshot = new(
            transition.Status,
            timeProvider.GetUtcNow(),
            Math.Max(0, transition.ReconnectAttempt),
            BoundCategory(transition.FailureCategory));

        Action<ProjectionConnectionSnapshot>[] handlers;
        lock (_sync) {
            if (_current == snapshot) {
                return;
            }

            _current = snapshot;
            handlers = [.. _handlers];
        }

        logger.LogInformation(
            "EventStore projection connection state changed. Status={Status}, Attempt={Attempt}, FailureCategory={FailureCategory}",
            snapshot.Status,
            snapshot.ReconnectAttempt,
            snapshot.LastFailureCategory ?? "none");

        foreach (Action<ProjectionConnectionSnapshot> handler in handlers) {
            handler(snapshot);
        }
    }

    private void Unsubscribe(Action<ProjectionConnectionSnapshot> handler) {
        lock (_sync) {
            _ = _handlers.Remove(handler);
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
}
