using System.Reflection;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Badges;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.State.ETagCache;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Badges;

/// <summary>
/// Story 5-2 T4 / AC7 — first real <see cref="IActionQueueCountReader"/>. Delegates to
/// <see cref="IQueryService"/> with the framework-allowlisted action-queue cache discriminator
/// so badge refreshes share the same response classifier and ETag cache seam as projection
/// page queries.
/// </summary>
/// <remarks>
/// <para>
/// <b>Take=0:</b> the request asks the server for zero rows; the actionable count is read
/// from <see cref="QueryResult{T}.TotalCount"/>. EventStore returns 304 Not Modified when the
/// cached ETag is still valid; <c>EventStoreQueryClient</c> reuses the cached payload and
/// the resulting <c>QueryResult.IsNotModified</c> short-circuits change emission upstream.
/// </para>
/// <para>
/// <b>429:</b> the underlying classifier raises <see cref="QueryFailureException"/> which
/// <c>BadgeCountService</c> catches in its existing handler. The previously published count
/// remains on the badge (Story 5-2 AC7 "preserves prior visible count on 429").
/// </para>
/// <para>
/// <b>401:</b> the classifier raises <see cref="AuthRedirectRequiredException"/>. The
/// reader does not invoke the redirector itself — that is the form-side responsibility — but
/// the exception bubbles through <c>BadgeCountService</c>'s catch and the host's auth
/// redirector picks it up on the next user interaction.
/// </para>
/// </remarks>
public sealed class EventStoreActionQueueCountReader : IActionQueueCountReader {
    private readonly IQueryService _queryService;
    private readonly IUserContextAccessor _userContext;
    private readonly ILogger<EventStoreActionQueueCountReader> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreActionQueueCountReader"/> class.
    /// </summary>
    public EventStoreActionQueueCountReader(
        IQueryService queryService,
        IUserContextAccessor userContext,
        ILogger<EventStoreActionQueueCountReader> logger) {
        ArgumentNullException.ThrowIfNull(queryService);
        ArgumentNullException.ThrowIfNull(userContext);
        ArgumentNullException.ThrowIfNull(logger);
        _queryService = queryService;
        _userContext = userContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<int> GetCountAsync(Type projectionType, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(projectionType);

        string projectionTypeName = projectionType.FullName ?? projectionType.Name;
        string? tenant = _userContext.TenantId;
        if (string.IsNullOrWhiteSpace(tenant)) {
            // Fail-closed: no authenticated tenant context, no count. The Null reader's
            // contract is "0 means caught up"; preserving that here avoids leaking a stale
            // visible count from a different tenant context.
            return 0;
        }

        string? domain = projectionType.GetCustomAttribute<BoundedContextAttribute>()?.Name;
        QueryRequest request = new(
            ProjectionType: projectionTypeName,
            TenantId: tenant!,
            Take: 0,
            Domain: domain,
            AggregateId: projectionTypeName,
            QueryType: projectionTypeName,
            CacheDiscriminator: ETagCacheDiscriminator.ForActionQueueCount(projectionTypeName));

        QueryResult<object> result = await _queryService
            .QueryAsync<object>(request, cancellationToken)
            .ConfigureAwait(false);
        return result.TotalCount;
    }
}
