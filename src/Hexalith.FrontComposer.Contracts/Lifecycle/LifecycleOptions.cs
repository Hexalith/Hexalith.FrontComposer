namespace Hexalith.FrontComposer.Contracts.Lifecycle;

/// <summary>
/// Configuration for <see cref="ILifecycleStateService"/>. Bind via <c>IOptions&lt;LifecycleOptions&gt;</c>.
/// </summary>
public sealed class LifecycleOptions {
    /// <summary>
    /// Bounded LRU capacity for cross-CorrelationId duplicate-MessageId detection (Decision D10).
    /// Default 1024. Adopters with high legitimate throughput may raise this; the cache is cleared on
    /// scope disposal so memory is still bounded by circuit lifetime.
    /// </summary>
    /// <remarks>
    /// <c>GracePeriod</c> and <c>PruneInterval</c> were considered and cut per ADR-019 —
    /// scope-lifetime eviction replaces time-based eviction.
    /// </remarks>
    public int MessageIdCacheCapacity { get; set; } = 1024;
}
