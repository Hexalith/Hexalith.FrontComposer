using System.Collections.Immutable;

namespace Hexalith.FrontComposer.SourceTools.Drift;

internal sealed class DriftComparisonResult(ImmutableArray<DriftDiagnosticFact> diagnostics) {
    // Story 9-1 P27: tightened from `public` to `internal` to match the comparison-seam contract
    // ("internal deterministic comparison service/result model"). Previously the `public`
    // modifier on an internal type signaled incipient public-surface intent.
    internal ImmutableArray<DriftDiagnosticFact> Diagnostics { get; } = diagnostics;
}
