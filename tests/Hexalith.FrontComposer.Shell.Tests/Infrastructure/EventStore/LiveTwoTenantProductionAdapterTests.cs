#pragma warning disable CA2007 // Test fixture awaits use the xUnit synchronization context.

using System.Text.Json;
using System.Net.Http.Headers;
using System.Text;

using Counter.Domain;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Badges;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.Services.Auth;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;
using Hexalith.FrontComposer.Testing;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

/// <summary>
/// Opt-in proof using the real HTTP query/count clients and SignalR connection against an
/// AppHost EventStore. The caller supplies two tenant identities; commands seed distinct
/// Counter projections through EventStore during this test.
/// </summary>
public sealed class LiveTwoTenantProductionAdapterTests {
    [Fact]
    [Trait("Category", "Performance")]
    public async Task ProvisionedTenants_QueryCountAndSubscribe_StayIsolated() {
        string? endpoint = Environment.GetEnvironmentVariable("FC_TEN_SCOPE_EVENTSTORE_URL");
        string? tenantA = Environment.GetEnvironmentVariable("FC_TEN_SCOPE_TENANT_A");
        string? tenantB = Environment.GetEnvironmentVariable("FC_TEN_SCOPE_TENANT_B");
        string? userA = Environment.GetEnvironmentVariable("FC_TEN_SCOPE_USER_A");
        string? userB = Environment.GetEnvironmentVariable("FC_TEN_SCOPE_USER_B");
        string? tokenA = Environment.GetEnvironmentVariable("FC_TEN_SCOPE_TOKEN_A");
        string? tokenB = Environment.GetEnvironmentVariable("FC_TEN_SCOPE_TOKEN_B");
        if (new[] { endpoint, tenantA, tenantB, userA, userB, tokenA, tokenB }
                .Any(string.IsNullOrWhiteSpace)) {
            Assert.Skip("Live two-tenant EventStore endpoint, identities and bearer tokens are not configured.");
        }

        Uri baseAddress = new(endpoint!, UriKind.Absolute);
        tenantA.ShouldNotBe(tenantB);
        string aggregateId = "scope-proof-" + Guid.NewGuid().ToString("N");
        string rowA = "scope-a-" + Guid.NewGuid().ToString("N");
        string rowB1 = "scope-b-" + Guid.NewGuid().ToString("N");
        string rowB2 = "scope-b-" + Guid.NewGuid().ToString("N");

        await using LiveScope a = new(baseAddress, tenantA!, userA!, tokenA!);
        await using LiveScope b = new(baseAddress, tenantB!, userB!, tokenB!);
        a.StorageKey().ShouldNotBe(b.StorageKey());

        await a.SubscribeAsync(TestContext.Current.CancellationToken);
        await b.SubscribeAsync(TestContext.Current.CancellationToken);
        await a.TriggerAsync(aggregateId, rowA, TestContext.Current.CancellationToken);
        await a.WaitForNudgeAsync(TestContext.Current.CancellationToken);
        await Task.Delay(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        b.NudgeCount.ShouldBe(0);
        a.OnlyObservedOwnTenant.ShouldBeTrue();
        int countAfterA = a.NudgeCount;
        await b.TriggerAsync(aggregateId, rowB1, TestContext.Current.CancellationToken);
        await b.WaitForNudgeAsync(TestContext.Current.CancellationToken);
        await b.TriggerAsync(aggregateId, rowB2, TestContext.Current.CancellationToken);

        QueryResult<JsonElement> rowsA = await a.WaitForRowsAsync([rowA], TestContext.Current.CancellationToken);
        QueryResult<JsonElement> rowsB = await b.WaitForRowsAsync([rowB1, rowB2], TestContext.Current.CancellationToken);
        string serializedA = JsonSerializer.Serialize(rowsA.Items);
        string serializedB = JsonSerializer.Serialize(rowsB.Items);
        serializedA.ShouldContain(rowA);
        serializedA.ShouldNotContain(rowB1);
        serializedA.ShouldNotContain(rowB2);
        serializedB.ShouldContain(rowB1);
        serializedB.ShouldContain(rowB2);
        serializedB.ShouldNotContain(rowA);

        int countA = await a.GetCountAsync(TestContext.Current.CancellationToken);
        int countB = await b.GetCountAsync(TestContext.Current.CancellationToken);
        countA.ShouldBe(1);
        countB.ShouldBe(2);
        await Task.Delay(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        a.NudgeCount.ShouldBe(countAfterA);
        a.OnlyObservedOwnTenant.ShouldBeTrue();
        b.OnlyObservedOwnTenant.ShouldBeTrue();

        CommandEvidenceAssertions.AssertRedacted(
            new CommandDispatchEvidence(
                "tenant-scope-proof", null, null, "Counter", "read", string.Empty, string.Empty,
                "passed", [], DateTimeOffset.UtcNow,
                JsonSerializer.Serialize(new { RowsA = rowsA.Items.Count, RowsB = rowsB.Items.Count, CountA = countA, CountB = countB })),
            tenantA!, userA!);
        CommandEvidenceAssertions.AssertRedacted(
            new CommandDispatchEvidence(
                "tenant-scope-proof", null, null, "Counter", "read", string.Empty, string.Empty,
                "passed", [], DateTimeOffset.UtcNow,
                JsonSerializer.Serialize(new { RowsA = rowsA.Items.Count, RowsB = rowsB.Items.Count, CountA = countA, CountB = countB })),
            tenantB!, userB!);
    }

    private sealed class LiveScope : IAsyncDisposable {
        private readonly HttpClient _http;
        private readonly EventStoreQueryClient _query;
        private readonly EventStoreActionQueueCountReader _count;
        private readonly ProjectionSubscriptionService _subscription;
        private readonly ProjectionConnectionStateService _connectionState;
        private readonly string _tenant;
        private readonly string _token;
        private readonly StorageScopeResolver _storageScope;
        private readonly TaskCompletionSource _nudge = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly System.Collections.Concurrent.ConcurrentQueue<string> _observedTenants = new();
        private int _nudgeCount;

        public int NudgeCount => Volatile.Read(ref _nudgeCount);
        public bool OnlyObservedOwnTenant => _observedTenants.All(
            tenant => string.Equals(tenant, _tenant, StringComparison.Ordinal));

        public LiveScope(Uri baseAddress, string tenant, string user, string token) {
            _tenant = tenant;
            _token = token;
            _http = new HttpClient { BaseAddress = baseAddress };
            IUserContextAccessor identity = new FixedUserContext(tenant, user);
            _storageScope = new StorageScopeResolver(identity, NullLogger.Instance,
                new FrontComposerTenantContextAccessor(identity, MsOptions.Create(new FcShellOptions()),
                    NullLogger<FrontComposerTenantContextAccessor>.Instance));
            EventStoreOptions eventStoreOptions = new() {
                BaseAddress = baseAddress,
                AccessTokenProvider = _ => ValueTask.FromResult<string?>(token),
            };
            IOptions<EventStoreOptions> options = MsOptions.Create(eventStoreOptions);
            _query = new EventStoreQueryClient(
                new SingleClientFactory(_http), options, identity,
                EventStoreTestSupport.CreateClassifier(), new EventStoreTestSupport.NoCache(),
                new NoOpAuthRedirector(), NullLogger<EventStoreQueryClient>.Instance,
                MsOptions.Create(new FcShellOptions()));
            _count = new EventStoreActionQueueCountReader(
                _query, identity, NullLogger<EventStoreActionQueueCountReader>.Instance);
            _connectionState = new ProjectionConnectionStateService(
                TimeProvider.System, NullLogger<ProjectionConnectionStateService>.Instance);
            ProjectionChangeNotifier notifier = new();
            notifier.ProjectionChangedForTenant += (projection, observedTenant) => {
                if (string.Equals(projection, "counter-projection", StringComparison.Ordinal)) {
                    _observedTenants.Enqueue(observedTenant);
                    _ = Interlocked.Increment(ref _nudgeCount);
                    _nudge.TrySetResult();
                }
            };
            _subscription = new ProjectionSubscriptionService(
                options, new SignalRProjectionHubConnectionFactory(), _connectionState,
                Substitute.For<IProjectionFallbackRefreshScheduler>(),
                notifier,
                NullLogger<ProjectionSubscriptionService>.Instance,
                userContextAccessor: identity,
                shellOptions: MsOptions.Create(new FcShellOptions()));
        }

        public Task<QueryResult<JsonElement>> QueryAsync(CancellationToken cancellationToken)
            => _query.QueryAsync<JsonElement>(QueryRequest.Create(
                Criteria: new ProjectionQuery("counter-projection", Take: 100),
                TenantId: _tenant,
                Domain: "Counter",
                AggregateId: "counter",
                QueryType: typeof(CounterProjection).FullName), cancellationToken);

        public string StorageKey() {
            _storageScope.TryResolveScope(out string tenant, out string user, "Theme", "hydrate").ShouldBeTrue();
            return StorageKeys.BuildKey(tenant, user, "theme");
        }

        public async Task<QueryResult<JsonElement>> WaitForRowsAsync(string[] markers, CancellationToken cancellationToken) {
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            while (true) {
                QueryResult<JsonElement> rows = await QueryAsync(timeout.Token);
                string serialized = JsonSerializer.Serialize(rows.Items);
                if (markers.All(marker => serialized.Contains(marker, StringComparison.Ordinal))) {
                    return rows;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(250), timeout.Token);
            }
        }

        public ValueTask<int> GetCountAsync(CancellationToken cancellationToken)
            => _count.GetCountAsync(typeof(CounterProjection), cancellationToken);

        public Task SubscribeAsync(CancellationToken cancellationToken)
            => _subscription.SubscribeAsync("counter-projection", _tenant, cancellationToken);

        public async Task TriggerAsync(string aggregateId, string marker, CancellationToken cancellationToken) {
            string body = JsonSerializer.Serialize(new {
                MessageId = Guid.NewGuid().ToString("N"),
                Tenant = _tenant,
                Domain = "counter",
                AggregateId = aggregateId,
                CommandType = "add-scope-row",
                Payload = new { RowId = marker, AggregateId = aggregateId },
            });
            using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands") {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);
            response.IsSuccessStatusCode.ShouldBeTrue("the configured live change trigger must be accepted");
        }

        public Task WaitForNudgeAsync(CancellationToken cancellationToken)
            => _nudge.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);

        public async ValueTask DisposeAsync() {
            await _subscription.DisposeAsync();
            await _connectionState.DisposeAsync();
            _http.Dispose();
        }
    }

    private sealed class FixedUserContext(string tenant, string user) : IUserContextAccessor {
        public string TenantId => tenant;

        public string UserId => user;
    }

    private sealed class SingleClientFactory(HttpClient client) : IHttpClientFactory {
        public HttpClient CreateClient(string name) => client;
    }
}
