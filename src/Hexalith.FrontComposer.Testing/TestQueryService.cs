using System.Collections.Concurrent;

using Hexalith.FrontComposer.Contracts.Communication;

namespace Hexalith.FrontComposer.Testing;

/// <summary>
/// Deterministic fake query service for component tests that need projection data without network access.
/// </summary>
public sealed class TestQueryService : IQueryService
{
    private readonly ConcurrentDictionary<Type, object> _results = new();
    private readonly FrontComposerTestOptions _options;

    internal TestQueryService(FrontComposerTestOptions options) => _options = options;

    /// <summary>Gets captured query evidence.</summary>
    public IReadOnlyList<ProjectionPageEvidence> Evidence { get; private set; } = [];

    /// <summary>Configures a successful typed query result.</summary>
    public void SucceedWith<T>(IReadOnlyList<T> items, string? etag = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        _results[typeof(T)] = new QueryResult<T>(items, items.Count, etag);
    }

    /// <inheritdoc />
    public Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Evidence =
        [
            .. Evidence,
            new ProjectionPageEvidence(
                typeof(T).FullName ?? typeof(T).Name,
                0,
                0,
                _options.TestTenantId,
                _options.TestUserId,
                "query",
                _options.TimeProvider.GetUtcNow()),
        ];

        if (_results.TryGetValue(typeof(T), out object? value) && value is QueryResult<T> result)
        {
            return Task.FromResult(result);
        }

        return Task.FromResult(new QueryResult<T>([], 0, null));
    }

    /// <summary>Clears configured results and captured evidence.</summary>
    public void Reset()
    {
        _results.Clear();
        Evidence = [];
    }
}
