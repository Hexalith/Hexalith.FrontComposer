using System.Collections.Immutable;

namespace Hexalith.FrontComposer.SourceTools.Drift;

internal sealed class DriftBaselineLoadResult(
    bool comparisonEnabled,
    DriftBaselineSet baseline,
    ImmutableArray<DriftDiagnosticFact> diagnostics) {
    internal bool ComparisonEnabled { get; } = comparisonEnabled;
    internal DriftBaselineSet Baseline { get; } = baseline;
    internal ImmutableArray<DriftDiagnosticFact> Diagnostics { get; } = diagnostics;
}
