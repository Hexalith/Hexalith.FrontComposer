using Hexalith.FrontComposer.Contracts.Lifecycle;

namespace Hexalith.FrontComposer.Mcp.Invocation;

/// <summary>Bridges a dispatcher callback across acknowledgement and the end of its request scope.</summary>
internal sealed class McpDispatchLifecycleObserver {
    private readonly object _gate = new();
    private List<(CommandLifecycleState State, string? MessageId)>? _pending = [];
    private Action<CommandLifecycleState, string?>? _record;

    public void Observe(CommandLifecycleState state, string? messageId) {
        lock (_gate) {
            if (_record is not null) {
                _record(state, messageId);
            }
            else {
                _pending?.Add((state, messageId));
            }
        }
    }

    public IReadOnlyList<(CommandLifecycleState State, string? MessageId)> TakePending() {
        lock (_gate) {
            var captured = _pending!;
            _pending = [];
            return captured;
        }
    }

    public void Bind(Action<CommandLifecycleState, string?> record) {
        lock (_gate) {
            // A callback may arrive while the captured batch is being validated and replayed.
            // Deliver it before callbacks that see the live sink.
            foreach ((CommandLifecycleState state, string? messageId) in _pending!) {
                record(state, messageId);
            }

            _pending = null;
            _record = record;
        }
    }

    public void StopBuffering() {
        lock (_gate) {
            // A failed dispatch has no entry. Ignore any further callbacks from its producer.
            _pending = null;
        }
    }
}
