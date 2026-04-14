namespace Hexalith.FrontComposer.Contracts.Storage;

/// <summary>
/// Provisional contract for key-value storage used by state management and caching.
/// Story 1.3 may extend the storage surface through companion abstractions while
/// keeping this interface stable for netstandard2.0 implementers.
/// Cache key pattern: {tenantId}:{userId}:{featureName}:{discriminator}.
/// </summary>
public interface IStorageService {
    /// <summary>
    /// Flushes any buffered writes before the host shuts down.
    /// Implementations with no buffered state may treat this operation as a no-op.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task FlushAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a value by key. Returns <c>null</c> (not throws) when the key is not found.
    /// </summary>
    /// <typeparam name="T">The stored value type.</typeparam>
    /// <param name="key">The storage key.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The stored value, or <c>null</c> if not found.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all keys matching the given prefix.
    /// </summary>
    /// <param name="prefix">The key prefix to filter by.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A read-only list of matching keys.</returns>
    Task<IReadOnlyList<string>> GetKeysAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value by key. Does not throw when the key is not found.
    /// </summary>
    /// <param name="key">The storage key to remove.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a value under the given key, overwriting any existing value.
    /// </summary>
    /// <typeparam name="T">The value type to store.</typeparam>
    /// <param name="key">The storage key.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default);
}
