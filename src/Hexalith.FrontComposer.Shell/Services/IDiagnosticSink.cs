using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Dev-mode diagnostic event surfaced by the shell — consumed by <c>FcDiagnosticsPanel</c> and
/// forwarded to <see cref="ILogger"/>. Story 2-2 Tasks 3.5/3.5a, surfacing Decision D31
/// fail-closed conditions (tenant/user missing) in a way that is visible to developers but
/// never in production.
/// </summary>
/// <param name="Code">Short diagnostic code (e.g. <c>"D31"</c>, <c>"D38"</c>).</param>
/// <param name="Category">Dev-facing category label (e.g. <c>"LastUsed"</c>).</param>
/// <param name="Message">Human-readable message.</param>
/// <param name="CapturedAt">UTC timestamp (for the panel's ordering / rate-limit window).</param>
public sealed record DevDiagnosticEvent(
    string Code,
    string Category,
    string Message,
    DateTimeOffset CapturedAt);

/// <summary>
/// Publishes diagnostic events to any subscribed surface (panel + logger).
/// </summary>
public interface IDiagnosticSink {
    /// <summary>Publishes a single event.</summary>
    void Publish(DevDiagnosticEvent evt);

    /// <summary>Returns the most-recent retained events (newest-first).</summary>
    IReadOnlyList<DevDiagnosticEvent> RecentEvents { get; }
}

/// <summary>
/// In-memory, rate-limited sink for the current circuit. Retains the last N events for the
/// <c>FcDiagnosticsPanel</c> UI and forwards every event to the shared <see cref="ILogger"/>.
/// </summary>
public sealed class InMemoryDiagnosticSink : IDiagnosticSink {
    private readonly ILogger<InMemoryDiagnosticSink>? _logger;
    private readonly object _gate = new();
    private readonly LinkedList<DevDiagnosticEvent> _events = new();
    private readonly HashSet<string> _seenCodesThisCircuit = new(StringComparer.Ordinal);
    private readonly int _capacity;

    public InMemoryDiagnosticSink(ILogger<InMemoryDiagnosticSink>? logger = null, int capacity = 32) {
        _logger = logger;
        _capacity = capacity > 0 ? capacity : 32;
    }

    /// <inheritdoc/>
    public IReadOnlyList<DevDiagnosticEvent> RecentEvents {
        get {
            lock (_gate) {
                return [.. _events];
            }
        }
    }

    /// <inheritdoc/>
    public void Publish(DevDiagnosticEvent evt) {
        ArgumentNullException.ThrowIfNull(evt);

        bool firstTime;
        lock (_gate) {
            firstTime = _seenCodesThisCircuit.Add(evt.Code);
            _events.AddFirst(evt);
            while (_events.Count > _capacity) {
                _events.RemoveLast();
            }
        }

        // Rate-limit duplicate logs to once-per-circuit per code (D31 behavior preserved for prod observability).
        if (firstTime) {
            _logger?.LogWarning("[{Code}/{Category}] {Message}", evt.Code, evt.Category, evt.Message);
        }
    }
}
