using System.Collections.Immutable;

namespace Hexalith.FrontComposer.SourceTools.Drift;

internal sealed class DriftBaselineSet(ImmutableArray<DriftBaselineContract> contracts) {
    internal ImmutableArray<DriftBaselineContract> Contracts { get; } = contracts;
}
