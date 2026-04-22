using Hexalith.FrontComposer.Contracts.Badges;

namespace Hexalith.FrontComposer.Shell.Badges;

/// <summary>
/// Default <see cref="IActionQueueCountReader"/> implementation (Story 3-5 D3 / ADR-045) —
/// returns <c>0</c> for every projection type so Counter.Web and other hosts that have not yet
/// wired Story 5-1's EventStore-backed reader boot cleanly. The home directory renders the
/// "All caught up" / first-visit state in this configuration.
/// </summary>
public sealed class NullActionQueueCountReader : IActionQueueCountReader {
    /// <inheritdoc />
    public ValueTask<int> GetCountAsync(Type projectionType, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(projectionType);
        cancellationToken.ThrowIfCancellationRequested();
        return new ValueTask<int>(0);
    }
}
