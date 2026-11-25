using Hexalith.FrontComposer.Contracts.Registration;

namespace Hexalith.FrontComposer.Shell.Components.Home;

/// <summary>
/// Per-card view-model: manifest + aggregated count + readiness marker + per-projection rows.
/// Public so the framework-owned <see cref="FcHomeCard"/> can accept it as a parameter
/// across assembly boundaries (Razor components' generated partial classes are public by default).
/// </summary>
/// <param name="Manifest">The source <see cref="DomainManifest"/>.</param>
/// <param name="AggregateCount">Sum of this manifest’s projection counts, capped at <see cref="int.MaxValue"/>.</param>
/// <param name="IsReady">Whether at least one projection count has arrived for this card.</param>
/// <param name="ProjectionRows">Per-projection (FQN, count) rows for the card body.</param>
public sealed record HomeCardModel(
    DomainManifest Manifest,
    int AggregateCount,
    bool IsReady,
    IReadOnlyList<HomeProjectionRow> ProjectionRows) {
    /// <summary>The exact aggregate computed by the Home directory when it exceeds the public limit.</summary>
    internal long? OverflowAggregateCount { get; init; }

    /// <summary>The aggregate used for Home presentation and ordering.</summary>
    internal long DisplayCount => OverflowAggregateCount ?? AggregateCount;
}
