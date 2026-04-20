
using System.Collections.Concurrent;

namespace Hexalith.FrontComposer.Contracts.Storage;
/// <summary>
/// In-memory implementation of <see cref="IStorageService"/> for server-side rendering
/// and bUnit testing. Thread-safe via <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// </summary>
public sealed class InMemoryStorageService : IStorageService {
    private static readonly object NullSentinel = new();

    private readonly ConcurrentDictionary<string, object> _store = new();

    /// <inheritdoc/>
    public Task FlushAsync(CancellationToken cancellationToken = default) {
        _ = _store.Count;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) {
        if (_store.TryGetValue(key, out object? value)) {
            if (ReferenceEquals(value, NullSentinel)) {
                return Task.FromResult<T?>(default);
            }

            return Task.FromResult((T?)value);
        }

        return Task.FromResult<T?>(default);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> GetKeysAsync(string prefix, CancellationToken cancellationToken = default) {
        IReadOnlyList<string> keys = _store.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();
        return Task.FromResult(keys);
    }

    /// <inheritdoc/>
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) {
        _ = _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default) {
        _store[key] = value is null
            ? NullSentinel
            : value;
        return Task.CompletedTask;
    }
}
