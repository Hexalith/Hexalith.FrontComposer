using System.Collections.Immutable;

using Counter.Specimens;

using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

namespace Counter.Web;

/// <summary>Gated Test loader for fixed pending and capped query results.</summary>
internal sealed class AccessibilitySpecimenPageLoader(AccessibilitySpecimenState scenarios) : IProjectionPageLoader
{
    /// <inheritdoc />
    public Task<ProjectionPageResult> LoadPageAsync(string projectionTypeFqn, int skip, int take,
        IImmutableDictionary<string, string> filters, string? sortColumn, bool sortDescending,
        string? searchQuery, CancellationToken cancellationToken)
        => projectionTypeFqn == "Counter.Domain.CounterProjection" && scenarios.Page is { } page
            ? string.Equals(searchQuery, "no-match-ae", StringComparison.Ordinal)
                ? Task.FromResult(new ProjectionPageResult([], 0, null))
                : page.WaitAsync(cancellationToken)
            : new NullProjectionPageLoader().LoadPageAsync(projectionTypeFqn, skip, take, filters, sortColumn, sortDescending, searchQuery, cancellationToken);
}
