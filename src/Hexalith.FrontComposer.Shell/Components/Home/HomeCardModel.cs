using Hexalith.FrontComposer.Contracts.Registration;

namespace Hexalith.FrontComposer.Shell.Components.Home;

/// <summary>
/// Per-card view-model: manifest + aggregated count + readiness marker + per-projection rows.
/// Public so the framework-owned <see cref="FcHomeCard"/> can accept it as a parameter
/// across assembly boundaries (Razor components' generated partial classes are public by default).
/// </summary>
/// <param name="Manifest">The source <see cref="DomainManifest"/>.</param>
/// <param name="AggregateCount">Sum of all projection counts inside this manifest.</param>
/// <param name="IsReady">Whether at least one projection count has arrived for this card.</param>
/// <param name="ProjectionRows">Per-projection (FQN, count) rows for the card body.</param>
public sealed record HomeCardModel(
    DomainManifest Manifest,
    int AggregateCount,
    bool IsReady,
    IReadOnlyList<HomeProjectionRow> ProjectionRows);
