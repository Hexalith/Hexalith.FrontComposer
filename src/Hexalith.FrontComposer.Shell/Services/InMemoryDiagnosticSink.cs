using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// In-memory, rate-limited sink for the current circuit. Retains the last N events for the
/// <c>FcDiagnosticsPanel</c> UI and forwards every event to the shared <see cref="ILogger"/>.
/// </summary>
public sealed class InMemoryDiagnosticSink : IDiagnosticSink {
    /// <summary>Maximum retained events — guards against pathological capacity values.</summary>
    private const int MaxCapacity = 10_000;

    private readonly ILogger<InMemoryDiagnosticSink>? _logger;
    private readonly object _gate = new();
    private readonly LinkedList<DevDiagnosticEvent> _events = new();
    private readonly HashSet<string> _seenCodesThisCircuit = new(StringComparer.Ordinal);
    private readonly int _capacity;

    public InMemoryDiagnosticSink(ILogger<InMemoryDiagnosticSink>? logger = null, int capacity = 32) {
        _logger = logger;
        _capacity = capacity switch {
            <= 0 => 32,
            > MaxCapacity => MaxCapacity,
            _ => capacity,
        };
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
