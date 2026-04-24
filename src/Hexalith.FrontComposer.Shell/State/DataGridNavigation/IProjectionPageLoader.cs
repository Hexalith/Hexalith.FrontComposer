using System.Collections.Immutable;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Story 4-4 D3 / D16 — Shell-side NON-GENERIC page loader abstraction consumed by
/// <see cref="LoadPageEffects"/>. Adopters typically implement this by delegating to their
/// typed <c>IQueryService.QueryAsync&lt;T&gt;</c> where <c>T</c> is known at the implementation site.
/// </summary>
/// <remarks>
/// <para>
/// The Fluxor effect cannot invoke <c>IQueryService.QueryAsync&lt;T&gt;</c> directly because
/// <see cref="LoadedPageState.PendingCompletionsByKey"/> is keyed by the non-generic TCS
/// boundary (D16 — Fluxor feature states cannot be open generics). This loader lives on the
/// Shell side of that boundary and returns rows as <c>IReadOnlyList&lt;object&gt;</c>. The
/// generator-emitted provider callback casts back to <c>IReadOnlyList&lt;T&gt;</c>.
/// </para>
/// <para>
/// Shell ships a no-op default implementation (<see cref="NullProjectionPageLoader"/>) so the
/// wiring compiles without an adopter-provided implementation; adopters must register a real
/// loader before the server-side lane activates (D2).
/// </para>
/// </remarks>
public interface IProjectionPageLoader {
    /// <summary>
    /// Loads a single page of projection rows for server-side virtualization.
    /// </summary>
    /// <param name="projectionTypeFqn">Fully-qualified projection type name extracted from the view key (<c>{boundedContext}:{projectionTypeFqn}</c>).</param>
    /// <param name="skip">Non-negative skip offset.</param>
    /// <param name="take">Positive page size.</param>
    /// <param name="filters">Per-column filter values keyed by declared property name; never null (pass <see cref="ImmutableDictionary{TKey, TValue}.Empty"/> when empty).</param>
    /// <param name="sortColumn">Declared property name to sort by, or <see langword="null"/> when unsorted.</param>
    /// <param name="sortDescending">Whether the sort is descending.</param>
    /// <param name="searchQuery">Global search query, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancellation token flowing from the provider request.</param>
    /// <returns>A page payload (items + total count + optional ETag) cast to <see cref="object"/>.</returns>
    Task<ProjectionPageResult> LoadPageAsync(
        string projectionTypeFqn,
        int skip,
        int take,
        IImmutableDictionary<string, string> filters,
        string? sortColumn,
        bool sortDescending,
        string? searchQuery,
        CancellationToken cancellationToken);
}

/// <summary>
/// Story 4-4 D3 — non-generic page result returned by <see cref="IProjectionPageLoader"/>.
/// </summary>
/// <param name="Items">Loaded rows (never null; may be empty).</param>
/// <param name="TotalCount">Server-reported total count across all pages.</param>
/// <param name="ETag">Optional ETag for cache validation on subsequent requests.</param>
public sealed record ProjectionPageResult(
    IReadOnlyList<object> Items,
    int TotalCount,
    string? ETag);

/// <summary>
/// Default implementation that returns an empty page — keeps wiring green until adopters
/// register a typed loader. Under the server-side virtualization lane (Story 4-4 D2), a
/// projection using this loader will render as empty and surface <see cref="Contracts.Rendering.LoadPageFailedAction"/>
/// when the threshold is crossed.
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
        => Task.FromResult(new ProjectionPageResult(Array.Empty<object>(), TotalCount: 0, ETag: null));
}
