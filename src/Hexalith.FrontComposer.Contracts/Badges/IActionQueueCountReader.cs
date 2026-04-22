namespace Hexalith.FrontComposer.Contracts.Badges;

/// <summary>
/// Produces actionable-item counts for ActionQueue-hinted projection types. Consumed by Story
/// 3-5's <c>BadgeCountService</c> for both the initial fan-out fetch and the per-type re-fetch on
/// <see cref="Hexalith.FrontComposer.Contracts.Communication.IProjectionChangeNotifier"/> events.
/// </summary>
/// <remarks>
/// <para>
/// The default Shell registration is <c>NullActionQueueCountReader</c>, which returns <c>0</c> for
/// every type. This lets Story 3-5 merge BEFORE Story 5-1 without blocking — Counter.Web sees zero
/// counts, which renders the home-directory "All caught up" / first-visit state (a graceful degraded
/// experience).
/// </para>
/// <para>
/// Story 5-1's <c>AddHexalithEventStore()</c> WILL register a real reader at <c>Scoped</c> lifetime
/// that delegates to EventStore's count endpoint (with ETag caching per Story 5-2). The
/// <see cref="System.Threading.Tasks.ValueTask{TResult}"/> return shape matches the hot-path: the
/// EventStore reader usually has an async network call but can return synchronously on cache hit.
/// </para>
/// </remarks>
public interface IActionQueueCountReader {
    /// <summary>
    /// Gets the current actionable-item count for the given projection runtime type.
    /// </summary>
    /// <param name="projectionType">The projection runtime type to fetch the count for.</param>
    /// <param name="cancellationToken">
    /// A token to observe for cancellation. <c>BadgeCountService</c> propagates its 5-second
    /// umbrella timeout here during the initial fan-out fetch.
    /// </param>
    /// <returns>The non-negative count of actionable items for the projection.</returns>
    ValueTask<int> GetCountAsync(Type projectionType, CancellationToken cancellationToken);
}
