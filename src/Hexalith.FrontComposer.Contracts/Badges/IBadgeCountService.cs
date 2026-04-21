namespace Hexalith.FrontComposer.Contracts.Badges;

/// <summary>
/// Per-projection actionable-item count surface (Story 3-4 ADR-044 — interface frozen by 3-4;
/// implementation ships in Story 3-5). Consumed via nullable injection in
/// <c>FcPaletteResultList</c> so the palette degrades gracefully when Story 3-5 is not yet
/// installed.
/// </summary>
/// <remarks>
/// Adopters that want badges before Story 3-5 ships can implement this interface and register it
/// with <c>services.AddScoped&lt;IBadgeCountService, MyImpl&gt;()</c> — the palette picks it up on
/// the next circuit with no further code change. Implementations MUST register as scoped so the
/// per-circuit <see cref="CountChanged"/> subscriptions do not leak across users or disposed render
/// trees.
/// </remarks>
public interface IBadgeCountService
{
    /// <summary>
    /// Snapshot of actionable-item counts keyed by projection runtime type.
    /// </summary>
    IReadOnlyDictionary<Type, int> Counts { get; }

    /// <summary>
    /// Reactive stream of count changes — subscribe to refresh badges in real time.
    /// </summary>
    IObservable<BadgeCountChangedArgs> CountChanged { get; }

    /// <summary>
    /// Sum of all <see cref="Counts"/> values — used by Story 3-5's home dashboard summary tile.
    /// </summary>
    int TotalActionableItems { get; }
}
