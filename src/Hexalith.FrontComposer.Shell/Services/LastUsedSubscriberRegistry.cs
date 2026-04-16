using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Story 2-2 Decision D35 — per-circuit registry that lazily resolves and tracks active
/// per-command <c>{Command}LastUsedSubscriber</c> instances. Idempotent <see cref="Ensure{T}"/>
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
    /// </summary>
    /// <typeparam name="TSubscriber">Concrete subscriber type emitted by <c>LastUsedSubscriberEmitter</c>.</typeparam>
    public void Ensure<TSubscriber>() where TSubscriber : class, IDisposable {
        if (_disposed) {
            return;
        }

        lock (_gate) {
            if (!_registered.Add(typeof(TSubscriber))) {
                return;
            }

            TSubscriber subscriber = _services.GetRequiredService<TSubscriber>();
            _instances.Add(subscriber);
        }
    }

    /// <inheritdoc/>
    public void Dispose() {
        lock (_gate) {
            if (_disposed) {
                return;
            }

            _disposed = true;
            foreach (IDisposable instance in _instances) {
                try {
                    instance.Dispose();
                }
                catch {
                    // Subscriber disposal must never throw — circuit teardown is best-effort.
                }
            }

            _instances.Clear();
            _registered.Clear();
        }
    }
}
