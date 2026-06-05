using Counter.Domain;

using Hexalith.FrontComposer.Contracts.Communication;

namespace Counter.Web;

internal sealed class CounterMcpSampleQueryService : IQueryService {
    public Task<QueryResult<T>> QueryAsync<T>(
        QueryRequest request,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (typeof(T) == typeof(CounterProjection)) {
            CounterProjection[] items = [
                new() {
                    Id = "counter-main",
                    Count = 42,
                    LastUpdated = DateTimeOffset.Parse(
                        "2026-06-05T12:00:00+00:00",
                        System.Globalization.CultureInfo.InvariantCulture),
                },
            ];

            QueryResult<CounterProjection> result = new(items, items.Length, "counter-mcp-e2e");
            return Task.FromResult((QueryResult<T>)(object)result);
        }

        return Task.FromResult(new QueryResult<T>([], 0, null));
    }
}
