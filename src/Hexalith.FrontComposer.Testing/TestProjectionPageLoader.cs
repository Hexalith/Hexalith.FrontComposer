using System.Collections.Concurrent;
using System.Collections.Immutable;

using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

namespace Hexalith.FrontComposer.Testing;

/// <summary>
/// Deterministic fake projection page loader for generated DataGrid virtualization tests.
/// </summary>
public sealed class TestProjectionPageLoader : IProjectionPageLoader
{
    private readonly ConcurrentDictionary<string, ProjectionPageResult> _pages = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<ProjectionPageEvidence> _evidence = new();
    private readonly FrontComposerTestOptions _options;

    internal TestProjectionPageLoader(FrontComposerTestOptions options) => _options = options;

    /// <summary>Gets captured page-load evidence.</summary>
    public IReadOnlyList<ProjectionPageEvidence> Evidence => [.. _evidence];

    /// <summary>Configures a successful page for one projection type.</summary>
    public void SucceedWith(string projectionTypeFqn, IReadOnlyList<object> items, int? totalCount = null, string? etag = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionTypeFqn);
        ArgumentNullException.ThrowIfNull(items);
        _pages[projectionTypeFqn] = new ProjectionPageResult(items, totalCount ?? items.Count, etag);
    }

    /// <summary>Configures a not-modified response for one projection type.</summary>
    public void NotModified(string projectionTypeFqn, IReadOnlyList<object> cachedItems, int? totalCount = null, string? etag = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionTypeFqn);
        ArgumentNullException.ThrowIfNull(cachedItems);
        _pages[projectionTypeFqn] = new ProjectionPageResult(cachedItems, totalCount ?? cachedItems.Count, etag, IsNotModified: true);
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
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool hasPage = _pages.TryGetValue(projectionTypeFqn, out ProjectionPageResult? result);
        EnqueueBounded(new ProjectionPageEvidence(
            projectionTypeFqn,
            skip,
            take,
            _options.TestTenantId,
            _options.TestUserId,
            hasPage ? (result!.IsNotModified ? "not-modified" : "configured") : "empty",
            _options.TimeProvider.GetUtcNow()));

        return Task.FromResult(hasPage ? result! : new ProjectionPageResult([], 0, null));
    }

    /// <summary>Clears configured pages and captured evidence.</summary>
    public void Reset()
    {
        _pages.Clear();
        while (_evidence.TryDequeue(out _))
        {
        }
    }

    private void EnqueueBounded(ProjectionPageEvidence evidence)
    {
        _evidence.Enqueue(evidence);
        while (_evidence.Count > _options.MaxEvidenceRecords && _evidence.TryDequeue(out _))
        {
        }
    }
}
