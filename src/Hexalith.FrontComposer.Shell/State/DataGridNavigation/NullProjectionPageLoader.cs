using System.Collections.Immutable;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Default implementation that fails visibly until adopters register a real loader. Under the
/// server-side virtualization lane (Story 4-4 D2), silently returning an empty page would hide a
/// missing integration as "no data", so the default throws and lets <see cref="LoadPageEffects"/>
/// surface the failure path.
/// </summary>
public sealed class NullProjectionPageLoader : IProjectionPageLoader {
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
        => throw new InvalidOperationException(
            "Server-side projection paging requires an IProjectionPageLoader implementation. " +
            "Register a typed loader before enabling the server-side virtualization lane.");
}
