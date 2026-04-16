using Hexalith.FrontComposer.Contracts.Lifecycle;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.FrontComposer.Shell.Services.Lifecycle;

/// <summary>
/// Story 2-3 Decision D5 — per-circuit registry that lazily resolves and tracks active per-command
/// <c>{Command}LifecycleBridge</c> instances. Idempotent <see cref="Ensure{TBridge}"/> prevents
/// hot-reload bridge accumulation; lazy resolution avoids startup latency on large command surfaces.
/// Mirrors <see cref="LastUsedSubscriberRegistry"/> verbatim.
/// </summary>
public sealed class LifecycleBridgeRegistry : ILifecycleBridgeRegistry, IDisposable {
    private readonly IServiceProvider _services;
    private readonly HashSet<Type> _registered = new();
    private readonly List<IDisposable> _instances = new();
    private readonly object _gate = new();
    private bool _disposed;

    public LifecycleBridgeRegistry(IServiceProvider services) {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Resolves the per-command bridge from DI on first call; subsequent calls are no-ops. Thread-safe.
    /// DI resolution runs outside the registry lock to avoid holding the lock during potentially slow
    /// or re-entrant service resolution. A bridge whose constructor throws (Chaos CM1 — e.g., Fluxor
    /// <c>SubscribeToAction</c> fault) leaves the type unregistered so a later call can retry.
    /// </summary>
    /// <typeparam name="TBridge">Concrete bridge type emitted by <c>CommandLifecycleBridgeEmitter</c>.</typeparam>
    public void Ensure<TBridge>() where TBridge : class, IDisposable {
        lock (_gate) {
            if (_disposed || _registered.Contains(typeof(TBridge))) {
                return;
            }
        }

        TBridge bridge = _services.GetRequiredService<TBridge>();

        bool keep = false;
        try {
            lock (_gate) {
                if (_disposed) {
                    return;
                }

                if (_registered.Add(typeof(TBridge))) {
                    _instances.Add(bridge);
                    keep = true;
                }
            }
        }
        finally {
            if (!keep) {
                try {
                    bridge.Dispose();
                }
                catch (ObjectDisposedException) {
                    // Already disposed by the scope — benign.
                }
                catch (InvalidOperationException) {
                    // Best-effort cleanup on the losing side of the race.
                }
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose() {
        List<IDisposable> toDispose;
        lock (_gate) {
            if (_disposed) {
                return;
            }

            _disposed = true;
            toDispose = new List<IDisposable>(_instances);
            _instances.Clear();
            _registered.Clear();
        }

        foreach (IDisposable instance in toDispose) {
            try {
                instance.Dispose();
            }
            catch (ObjectDisposedException) {
                // Already disposed by the owning scope — benign.
            }
            catch (InvalidOperationException) {
                // Bridge's Dispose observed an inconsistent Fluxor subscription state;
                // circuit teardown is best-effort.
            }
        }
    }
}
