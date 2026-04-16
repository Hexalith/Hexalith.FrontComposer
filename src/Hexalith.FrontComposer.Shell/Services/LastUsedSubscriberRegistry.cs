using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Story 2-2 Decision D35 — per-circuit registry that lazily resolves and tracks active
/// per-command <c>{Command}LastUsedSubscriber</c> instances. Idempotent <see cref="Ensure{TSubscriber}"/>
/// prevents hot-reload subscriber accumulation; lazy resolution avoids startup latency on large
/// command surfaces.
/// </summary>
public sealed class LastUsedSubscriberRegistry : ILastUsedSubscriberRegistry, IDisposable {
    private readonly IServiceProvider _services;
    private readonly HashSet<Type> _registered = new();
    private readonly List<IDisposable> _instances = new();
    private readonly object _gate = new();
    private bool _disposed;

    public LastUsedSubscriberRegistry(IServiceProvider services) {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Resolves the per-command subscriber from DI on first call; subsequent calls are no-ops.
    /// Thread-safe. DI resolution runs outside the registry lock to avoid holding the lock during
    /// potentially slow or re-entrant service resolution; a failed resolution leaves the type
    /// unregistered so a later call can retry.
    /// </summary>
    /// <typeparam name="TSubscriber">Concrete subscriber type emitted by <c>LastUsedSubscriberEmitter</c>.</typeparam>
    public void Ensure<TSubscriber>() where TSubscriber : class, IDisposable {
        // Fast path without lock — a post-check under the lock enforces correctness on the race.
        lock (_gate) {
            if (_disposed || _registered.Contains(typeof(TSubscriber))) {
                return;
            }
        }

        TSubscriber subscriber = _services.GetRequiredService<TSubscriber>();

        bool keep = false;
        try {
            lock (_gate) {
                if (_disposed) {
                    // Raced with Dispose — drop the just-resolved instance outside the lock.
                    return;
                }

                if (_registered.Add(typeof(TSubscriber))) {
                    _instances.Add(subscriber);
                    keep = true;
                }
            }
        }
        finally {
            if (!keep) {
                try {
                    subscriber.Dispose();
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

        // Dispose outside the lock — a subscriber that calls back into the registry during its
        // own Dispose would otherwise deadlock / observe partially-disposed state.
        foreach (IDisposable instance in toDispose) {
            try {
                instance.Dispose();
            }
            catch (ObjectDisposedException) {
                // Already disposed by the owning scope — benign.
            }
            catch (InvalidOperationException) {
                // Subscriber's Dispose observed an inconsistent state; circuit teardown is
                // best-effort and we refuse to let one bad subscriber crash the registry.
            }
        }
    }
}
