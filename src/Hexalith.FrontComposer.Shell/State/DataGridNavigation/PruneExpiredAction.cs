using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>Prunes snapshots whose <see cref="GridViewSnapshot.CapturedAt"/> is strictly before the threshold.</summary>
/// <remarks>
/// <see cref="Threshold"/> offset MUST be <see cref="TimeSpan.Zero"/> (UTC); the ctor rejects other
/// offsets so pruning cannot silently become non-deterministic. <c>default(DateTimeOffset)</c>
/// (= <see cref="DateTimeOffset.MinValue"/>) prunes nothing; <see cref="DateTimeOffset.MaxValue"/>
/// prunes every snapshot.
/// </remarks>
public sealed record PruneExpiredAction {

    /// <summary>Initializes a new instance of the <see cref="PruneExpiredAction"/> record.</summary>
    /// <param name="threshold">UTC threshold; snapshots captured strictly before this moment are pruned.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="threshold"/> has a non-UTC offset.</exception>
    public PruneExpiredAction(DateTimeOffset threshold) => Threshold = threshold;

    /// <summary>Gets the UTC threshold below which snapshots are pruned.</summary>
    public DateTimeOffset Threshold {
        get;
        init {
            if (value.Offset != TimeSpan.Zero) {
                throw new ArgumentException("Threshold offset must be TimeSpan.Zero (UTC).", nameof(value));
            }

            field = value;
        }
    }
}
