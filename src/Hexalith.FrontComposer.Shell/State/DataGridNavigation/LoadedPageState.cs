using System.Collections.Immutable;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Story 4-4 D2 / D3 / D10 / D16 — Fluxor feature state carrying:
/// <list type="bullet">
/// <item><description><see cref="PagesByKey"/> — the per-<c>(viewKey, skip)</c> page cache;</description></item>
/// <item><description><see cref="TotalCountByKey"/> — the last server-reported total per view;</description></item>
/// <item><description><see cref="LastElapsedMsByKey"/> — last measured <c>LoadPageAction</c> elapsed ms per view;</description></item>
/// <item><description><see cref="PendingCompletionsByKey"/> — TCS handles awaited by the generated view's provider callback;</description></item>
/// <item><description><see cref="LaneByKey"/> — latched client/server lane (D2);</description></item>
/// <item><description><see cref="PageInsertionOrder"/> — single <see cref="ImmutableQueue{T}"/> tracking insertion order for FIFO eviction at <c>MaxCachedPages</c>.</description></item>
/// </list>
/// </summary>
/// <remarks>
/// <para>
/// <b>TCS boundary (D16):</b> <see cref="PendingCompletionsByKey"/> uses
/// <see cref="TaskCompletionSource{TResult}"/> of <see cref="object"/> — Fluxor feature states must
/// be concrete non-open-generic types. The generator-emitted provider callback casts the resolved
/// object back to <c>IReadOnlyList&lt;T&gt;</c>. Resolution uses <c>TrySet*</c> exclusively to
/// absorb rapid-scroll races.
/// </para>
/// <para>
/// <b>Eviction (D10 re-revised):</b> on each successful page write the reducer enqueues the
/// <c>(ViewKey, Skip)</c> key onto <see cref="PageInsertionOrder"/>. When
/// <c>PagesByKey.Count</c> exceeds <c>FcShellOptions.MaxCachedPages</c>, the reducer dequeues the
/// front of the queue (O(1)), removes the matching <see cref="PagesByKey"/> entry, and emits an
/// Information-level log so operator dashboards can observe cache pressure.
/// </para>
/// </remarks>
public sealed record LoadedPageState {
    /// <summary>Gets the circuit-scope generation for page completion provenance.</summary>
    internal long ScopeGeneration { get; init; }

    /// <summary>Gets the cached pages keyed by <c>(viewKey, skip)</c>.</summary>
    public ImmutableDictionary<(string ViewKey, int Skip), IReadOnlyList<object>> PagesByKey { get; init; }
        = ImmutableDictionary<(string ViewKey, int Skip), IReadOnlyList<object>>.Empty;

    /// <summary>Gets the last server-reported total row count per view key.</summary>
    public ImmutableDictionary<string, int> TotalCountByKey { get; init; }
        = ImmutableDictionary<string, int>.Empty;

    /// <summary>Gets the last measured <c>LoadPageAction</c> elapsed milliseconds per view key.</summary>
    public ImmutableDictionary<string, long> LastElapsedMsByKey { get; init; }
        = ImmutableDictionary<string, long>.Empty;

    /// <summary>Safe classified query failures keyed by view, cleared by a fresh load.</summary>
    public ImmutableDictionary<string, string> FailureByKey { get; init; }
        = ImmutableDictionary<string, string>.Empty;

    /// <summary>Last completed page result and request provenance for each view.</summary>
    public ImmutableDictionary<string, LoadedPageResult> LastResultByKey { get; init; }
        = ImmutableDictionary<string, LoadedPageResult>.Empty;

    /// <summary>Current filter and sort request identity per view.</summary>
    public ImmutableDictionary<string, string> ActiveRequestIdentityByView { get; init; }
        = ImmutableDictionary<string, string>.Empty;

    /// <summary>Monotonic request generation per view; disambiguates A-B-A criteria sequences.</summary>
    public ImmutableDictionary<string, long> ActiveRequestGenerationByView { get; init; }
        = ImmutableDictionary<string, long>.Empty;

    /// <summary>Maximum retained page-result metadata entries.</summary>
    public int ResultMetadataLimit { get; init; } = 10;

    /// <summary>Independent completed result provenance for each virtualized offset.</summary>
    public ImmutableDictionary<(string ViewKey, int Skip), LoadedPageResult> ResultsByPage { get; init; }
        = ImmutableDictionary<(string ViewKey, int Skip), LoadedPageResult>.Empty;

    /// <summary>Monotonic per-view completed result sequence, retained across 304 and filter changes.</summary>
    public ImmutableDictionary<string, long> ResultSequenceByView { get; init; }
        = ImmutableDictionary<string, long>.Empty;

    /// <summary>Last successful first page for cap and hidden-detail provenance.</summary>
    public ImmutableDictionary<string, LoadedPageResult> LastSuccessfulPrimaryByView { get; init; }
        = ImmutableDictionary<string, LoadedPageResult>.Empty;

    /// <summary>Gets the pending TCS handles keyed by <c>(viewKey, skip)</c>.</summary>
    /// <remarks>
    /// <see cref="TaskCompletionSource{TResult}"/> is a mutable reference held inside an immutable
    /// dictionary — the key is immutable and the TCS itself is the reducer/provider seam.
    /// Resolution uses <c>TrySet*</c>; double-resolution is silently absorbed.
    /// </remarks>
    public ImmutableDictionary<(string ViewKey, int Skip), TaskCompletionSource<object>> PendingCompletionsByKey { get; init; }
        = ImmutableDictionary<(string ViewKey, int Skip), TaskCompletionSource<object>>.Empty;

    /// <summary>Stable registration times for pending page requests, retained across component remounts.</summary>
    public ImmutableDictionary<(string ViewKey, int Skip), DateTimeOffset> PendingStartedAtByKey { get; init; }
        = ImmutableDictionary<(string ViewKey, int Skip), DateTimeOffset>.Empty;

    /// <summary>Gets the virtualization lane decision latched per view key (Story 4-4 D2).</summary>
    public ImmutableDictionary<string, VirtualizationLane> LaneByKey { get; init; }
        = ImmutableDictionary<string, VirtualizationLane>.Empty;

    /// <summary>Gets the insertion-order queue used for <c>MaxCachedPages</c> FIFO eviction (Story 4-4 D10).</summary>
    public ImmutableQueue<(string ViewKey, int Skip)> PageInsertionOrder { get; init; }
        = ImmutableQueue<(string ViewKey, int Skip)>.Empty;
}
