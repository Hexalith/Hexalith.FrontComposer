using System.Collections.Concurrent;

using Hexalith.FrontComposer.Contracts.Communication;

namespace Hexalith.FrontComposer.Testing;

/// <summary>
/// Deterministic fake query service for component tests that need projection data without network access.
/// </summary>
public sealed class TestQueryService : IQueryService {
    private readonly ConcurrentDictionary<Type, object> _configurations = new();
    private readonly ConcurrentQueue<ProjectionPageEvidence> _evidence = new();
    private readonly FrontComposerTestOptions _options;

    internal TestQueryService(FrontComposerTestOptions options) => _options = options;

    /// <summary>Gets captured query evidence.</summary>
    public IReadOnlyList<ProjectionPageEvidence> Evidence => [.. _evidence];

    /// <summary>Configures a successful typed query result.</summary>
    public void SucceedWith<T>(IReadOnlyList<T> items, string? etag = null) {
        ArgumentNullException.ThrowIfNull(items);
        _configurations[typeof(T)] = TestQueryConfiguration<T>.FromResult(new QueryResult<T>(items, items.Count, etag));
    }

    /// <summary>Configures a successful result selected from each request.</summary>
    public void SucceedWith<T>(Func<QueryRequest, QueryResult<T>> callback) {
        ArgumentNullException.ThrowIfNull(callback);
        _configurations[typeof(T)] = TestQueryConfiguration<T>.FromCallback(callback);
    }

    /// <summary>Configures a not-modified typed query result backed by cached items.</summary>
    public void NotModifiedWith<T>(IReadOnlyList<T> cachedItems, string? etag = null) {
        ArgumentNullException.ThrowIfNull(cachedItems);
        _configurations[typeof(T)] = TestQueryConfiguration<T>.FromResult(
            QueryResult<T>.NotModifiedFromCache(cachedItems, cachedItems.Count, etag));
    }

    /// <inheritdoc />
    public Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        TestQueryConfiguration<T>? configuration = _configurations.TryGetValue(typeof(T), out object? configured)
            ? configured as TestQueryConfiguration<T>
            : null;
        bool callbackMode = configuration?.Callback is not null;
        QueryResult<T>? result = null;
        Exception? callbackFailure = null;
        try {
            result = callbackMode
                ? configuration!.Callback!(request)
                    ?? throw new InvalidOperationException("The configured query callback returned null.")
                : configuration?.Result;
        }
        catch (Exception ex) {
            callbackFailure = ex;
        }
        EnqueueBounded(new ProjectionPageEvidence(
            request.Criteria.ProjectionType,
            request.Criteria.Skip ?? 0,
            request.Criteria.Take ?? 0,
            request.TenantId ?? _options.TestTenantId,
            _options.TestUserId,
            callbackFailure is not null ? "callback-failed" : result is null ? "empty" : result.IsNotModified ? "not-modified" : callbackMode ? "callback" : "configured",
            _options.TimeProvider.GetUtcNow()));

        if (callbackFailure is not null) {
            if (callbackFailure is OperationCanceledException canceled) {
                CancellationToken canceledToken = canceled.CancellationToken.IsCancellationRequested
                    ? canceled.CancellationToken
                    : new CancellationToken(canceled: true);
                return Task.FromCanceled<QueryResult<T>>(canceledToken);
            }

            return Task.FromException<QueryResult<T>>(callbackFailure);
        }

        if (result is not null) {
            return Task.FromResult(result);
        }

        return Task.FromResult(new QueryResult<T>([], 0, null));
    }

    /// <summary>Clears configured results and captured evidence.</summary>
    public void Reset() {
        _configurations.Clear();
        while (_evidence.TryDequeue(out _)) {
        }
    }

    private void EnqueueBounded(ProjectionPageEvidence evidence) {
        _evidence.Enqueue(evidence);
        while (_evidence.Count > _options.MaxEvidenceRecords && _evidence.TryDequeue(out _)) {
        }
    }
}
