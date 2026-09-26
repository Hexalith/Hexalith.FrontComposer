using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Badges;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Badges;

/// <summary>
/// Story 5-2 T4 / AC7 — verify the EventStore-backed action-queue count reader builds the
/// allowlisted cache discriminator, fail-closes when no tenant context is available, and
/// surfaces 429 / 401 exceptions cleanly so <c>BadgeCountService</c> can preserve the prior
/// visible count.
/// </summary>
public class EventStoreActionQueueCountReaderTests {
    [Fact]
    public async Task GetCountAsync_BuildsAllowlistedCacheDiscriminator_AndReturnsTotalCount() {
        RecordingQueryService queryService = new(_ => new QueryResult<object>([], 7, "\"v1\""));
        EventStoreActionQueueCountReader reader = new(
            queryService,
            new TestUserContext("acme", "alice"),
            NullLogger<EventStoreActionQueueCountReader>.Instance);

        int count = await reader.GetCountAsync(typeof(SampleProjection), TestContext.Current.CancellationToken);

        count.ShouldBe(7);
        queryService.LastRequest.ShouldNotBeNull();
        queryService.LastRequest!.TenantId.ShouldBe("acme");
        queryService.LastRequest!.Criteria.Take.ShouldBe(0);
        queryService.LastRequest.Criteria.ProjectionType.ShouldBe("sample-projection");
        queryService.LastRequest.QueryType.ShouldBe(typeof(SampleProjection).FullName);
        queryService.LastRequest.CacheDiscriminator.ShouldStartWith("action-queue-count:");
    }

    [Fact]
    public async Task GetCountAsync_FailsClosed_WhenNoAuthenticatedTenant() {
        RecordingQueryService queryService = new(_ => new QueryResult<object>([], 99, null));
        EventStoreActionQueueCountReader reader = new(
            queryService,
            new TestUserContext(tenantId: null, userId: "alice"),
            NullLogger<EventStoreActionQueueCountReader>.Instance);

        int count = await reader.GetCountAsync(typeof(SampleProjection), TestContext.Current.CancellationToken);

        count.ShouldBe(0);
        queryService.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task GetCountAsync_PropagatesQueryFailureException_429() {
        RecordingQueryService queryService = new(_ => throw new QueryFailureException(QueryFailureKind.RateLimited, ProblemDetailsPayload.Empty, System.TimeSpan.FromSeconds(5)));
        EventStoreActionQueueCountReader reader = new(
            queryService,
            new TestUserContext("acme", "alice"),
            NullLogger<EventStoreActionQueueCountReader>.Instance);

        QueryFailureException ex = await Should.ThrowAsync<QueryFailureException>(
            async () => await reader.GetCountAsync(typeof(SampleProjection), TestContext.Current.CancellationToken).ConfigureAwait(true))
            .ConfigureAwait(true);

        ex.Kind.ShouldBe(QueryFailureKind.RateLimited);
    }

    [Fact]
    public async Task GetCountAsync_HeldTenantARequest_UsesExplicitTenantAndNextRequestUsesB() {
        string tenant = "tenant-a";
        IUserContextAccessor context = Substitute.For<IUserContextAccessor>();
        context.TenantId.Returns(_ => tenant);
        context.UserId.Returns("user-1");
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ManualResetEventSlim release = new();
        RecordingQueryService queryService = new(request => {
            if (request.TenantId == "tenant-a") {
                entered.TrySetResult();
                release.Wait(TestContext.Current.CancellationToken);
            }

            return new QueryResult<object>([], 7, null);
        });
        EventStoreActionQueueCountReader reader = new(queryService, context,
            NullLogger<EventStoreActionQueueCountReader>.Instance);

        Task<int> old = Task.Run(async () => await reader.GetCountAsync(
            typeof(SampleProjection), TestContext.Current.CancellationToken).ConfigureAwait(true), TestContext.Current.CancellationToken);
        try {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            queryService.LastRequest.ShouldNotBeNull().TenantId.ShouldBe("tenant-a");
            tenant = "tenant-b";
        }
        finally {
            release.Set();
        }

        TenantContextException stale = await Should.ThrowAsync<TenantContextException>(
            async () => await old.ConfigureAwait(true));
        stale.FailureCategory.ShouldBe(TenantContextFailureCategory.StaleTenantContext);
        (await reader.GetCountAsync(typeof(SampleProjection), TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBe(7);
        queryService.LastRequest.ShouldNotBeNull().TenantId.ShouldBe("tenant-b");
    }

    private sealed class SampleProjection { }

    private sealed class TestUserContext(string? tenantId, string? userId) : IUserContextAccessor {
        public string? TenantId { get; } = tenantId;
        public string? UserId { get; } = userId;
    }

    private sealed class RecordingQueryService(System.Func<QueryRequest, QueryResult<object>> respond) : IQueryService {
        public QueryRequest? LastRequest { get; private set; }

        public Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default) {
            LastRequest = request;
            QueryResult<object> result = respond(request);
            // Adapt to T — only used with T = object in these tests.
            return Task.FromResult((QueryResult<T>)(object)result);
        }
    }
}
