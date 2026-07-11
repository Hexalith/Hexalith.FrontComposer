using System.Collections.Concurrent;
using System.Collections.Immutable;

using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

namespace Hexalith.FrontComposer.Testing;

/// <summary>
/// Deterministic fake projection page loader for generated DataGrid virtualization tests.
/// </summary>
public sealed class TestProjectionPageLoader : IProjectionPageLoader {
    private readonly ConcurrentDictionary<string, TestProjectionPageConfiguration> _configurations = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<ProjectionPageEvidence> _evidence = new();
    private readonly FrontComposerTestOptions _options;

    internal TestProjectionPageLoader(FrontComposerTestOptions options) => _options = options;

    /// <summary>Gets captured page-load evidence.</summary>
    public IReadOnlyList<ProjectionPageEvidence> Evidence => [.. _evidence];

    /// <summary>Configures a successful page for one projection type.</summary>
    public void SucceedWith(string projectionTypeFqn, IReadOnlyList<object> items, int? totalCount = null, string? etag = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionTypeFqn);
        ArgumentNullException.ThrowIfNull(items);
        _configurations[projectionTypeFqn] = TestProjectionPageConfiguration.FromResult(
            new ProjectionPageResult(items, totalCount ?? items.Count, etag));
    }

    /// <summary>Configures a page result selected from all inputs for one projection type.</summary>
    public void SucceedWith(string projectionTypeFqn, Func<ProjectionPageRequest, ProjectionPageResult> callback) {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionTypeFqn);
        ArgumentNullException.ThrowIfNull(callback);
        _configurations[projectionTypeFqn] = TestProjectionPageConfiguration.FromCallback(callback);
    }

    /// <summary>Configures a not-modified response for one projection type.</summary>
    public void NotModified(string projectionTypeFqn, IReadOnlyList<object> cachedItems, int? totalCount = null, string? etag = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionTypeFqn);
        ArgumentNullException.ThrowIfNull(cachedItems);
        _configurations[projectionTypeFqn] = TestProjectionPageConfiguration.FromResult(
            new ProjectionPageResult(cachedItems, totalCount ?? cachedItems.Count, etag, IsNotModified: true));
    }

    /// <inheritdoc />
    public Task<ProjectionPageResult> LoadPageAsync(
        string projectionTypeFqn,
        int skip,
        int take,
        IImmutableDictionary<string, string> filters,
        string? sortColumn,
        bool sortDescending,
        string? searchQuery,
        CancellationToken cancellationToken) {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionTypeFqn);
        ArgumentNullException.ThrowIfNull(filters);
        cancellationToken.ThrowIfCancellationRequested();
        bool configured = _configurations.TryGetValue(projectionTypeFqn, out TestProjectionPageConfiguration? configuration);
        bool callbackMode = configuration?.Callback is not null;
        ProjectionPageResult? result = configuration?.Result;
        bool hasPage = configured && result is not null;
        Exception? callbackFailure = null;
        if (callbackMode) {
            try {
                result = configuration!.Callback!(new(projectionTypeFqn, skip, take, filters, sortColumn, sortDescending, searchQuery))
                    ?? throw new InvalidOperationException("The configured projection page callback returned null.");
                hasPage = true;
            }
            catch (Exception ex) {
                callbackFailure = ex;
            }
        }
        EnqueueBounded(new ProjectionPageEvidence(
            projectionTypeFqn,
            skip,
            take,
            _options.TestTenantId,
            _options.TestUserId,
            callbackFailure is not null ? "callback-failed" : hasPage ? result!.IsNotModified ? "not-modified" : callbackMode ? "callback" : "configured" : "empty",
            _options.TimeProvider.GetUtcNow()));

        if (callbackFailure is not null) {
            if (callbackFailure is OperationCanceledException canceled) {
                CancellationToken canceledToken = canceled.CancellationToken.IsCancellationRequested
                    ? canceled.CancellationToken
                    : new CancellationToken(canceled: true);
                return Task.FromCanceled<ProjectionPageResult>(canceledToken);
            }

            return Task.FromException<ProjectionPageResult>(callbackFailure);
        }

        return Task.FromResult(hasPage ? result! : new ProjectionPageResult([], 0, null));
    }

    /// <summary>Clears configured pages and captured evidence.</summary>
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
