using System.Collections.Concurrent;

using Hexalith.FrontComposer.Contracts.Communication;

namespace Hexalith.FrontComposer.Testing;

/// <summary>
/// Deterministic fake query service for component tests that need projection data without network access.
/// </summary>
public sealed class TestQueryService : IQueryService {
    private readonly ConcurrentDictionary<Type, object> _results = new();
    private readonly ConcurrentQueue<ProjectionPageEvidence> _evidence = new();
    private readonly FrontComposerTestOptions _options;

    internal TestQueryService(FrontComposerTestOptions options) => _options = options;

    /// <summary>Gets captured query evidence.</summary>
    public IReadOnlyList<ProjectionPageEvidence> Evidence => [.. _evidence];

    /// <summary>Configures a successful typed query result.</summary>
    public void SucceedWith<T>(IReadOnlyList<T> items, string? etag = null) {
        ArgumentNullException.ThrowIfNull(items);
        _results[typeof(T)] = new QueryResult<T>(items, items.Count, etag);
    }

    /// <summary>Configures a not-modified typed query result backed by cached items.</summary>
    public void NotModifiedWith<T>(IReadOnlyList<T> cachedItems, string? etag = null) {
        ArgumentNullException.ThrowIfNull(cachedItems);
        _results[typeof(T)] = QueryResult<T>.NotModifiedFromCache(cachedItems, cachedItems.Count, etag);
    }

    /// <inheritdoc />
    public Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        QueryResult<T>? result = _results.TryGetValue(typeof(T), out object? value)
            ? value as QueryResult<T>
            : null;
        EnqueueBounded(new ProjectionPageEvidence(
            request.ProjectionType,
            request.Skip ?? 0,
            request.Take ?? 0,
            request.TenantId ?? _options.TestTenantId,
            _options.TestUserId,
            result is null ? "empty" : (result.IsNotModified ? "not-modified" : "configured"),
            _options.TimeProvider.GetUtcNow()));

        if (result is not null) {
            return Task.FromResult(result);
        }

        return Task.FromResult(new QueryResult<T>([], 0, null));
    }

    /// <summary>Clears configured results and captured evidence.</summary>
    public void Reset() {
        _results.Clear();
        while (_evidence.TryDequeue(out _)) {
        }
    }

    private void EnqueueBounded(ProjectionPageEvidence evidence) {
        _evidence.Enqueue(evidence);
        while (_evidence.Count > _options.MaxEvidenceRecords && _evidence.TryDequeue(out _)) {
        }
    }
}
